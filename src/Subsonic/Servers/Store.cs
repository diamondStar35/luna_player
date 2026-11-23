using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Servers;

/// <summary>Atomic profile storage. A failed read or decryption prevents any replacement of existing data.</summary>
internal sealed class Store(string path)
{
    private static readonly byte[] Entropy = "LunaPlayer.Subsonic.v1"u8.ToArray();
    private readonly string _path = Path.GetFullPath(path);
    private readonly SemaphoreSlim _gate = new(1, 1);
    internal Guid? LastServer { get; private set; }
    internal async Task<Profile[]> LoadAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try { return await ReadAsync(token).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }
    internal async Task<Profile> SaveAsync(Profile profile, CancellationToken token = default)
    {
        Validate(profile);
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var profiles = (await ReadAsync(token).ConfigureAwait(false)).ToList();
            var saved = profile.Id == Guid.Empty ? profile with { Id = Guid.NewGuid() } : profile;
            var index = profiles.FindIndex(x => x.Id == saved.Id);
            if (index < 0) profiles.Add(saved); else profiles[index] = saved;
            await WriteAsync(profiles, LastServer, token).ConfigureAwait(false);
            return saved;
        }
        finally { _gate.Release(); }
    }
    internal async Task<bool> RemoveAsync(Guid id, CancellationToken token = default)
    {
        if (id == Guid.Empty) throw new ArgumentException(nameof(id));
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var profiles = (await ReadAsync(token).ConfigureAwait(false)).ToList();
            if (profiles.RemoveAll(x => x.Id == id) == 0) return false;
            await WriteAsync(profiles, LastServer, token).ConfigureAwait(false);
            return true;
        }
        finally { _gate.Release(); }
    }
    internal async Task SelectAsync(Guid server, CancellationToken token = default)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var profiles = (await ReadAsync(token).ConfigureAwait(false)).ToList();
            if (!profiles.Any(profile => profile.Id == server)) throw new KeyNotFoundException();
            if (LastServer != server) await WriteAsync(profiles, server, token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
    private async Task<Profile[]> ReadAsync(CancellationToken token)
    {
        if (!File.Exists(_path)) { LastServer = null; return []; }
        try
        {
            var bytes = await File.ReadAllBytesAsync(_path, token).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize(bytes, StoreJson.Default.StoredDocument);
            if (document is null || document.Version != 1 || document.Servers is null) throw new InvalidDataException("Invalid server store.");
            var ids = new HashSet<Guid>();
            var result = new List<Profile>();
            foreach (var entry in document.Servers)
            {
                if (entry?.Profile is not { } profile || profile.Id == Guid.Empty || !ids.Add(profile.Id)
                    || profile.Credentials is null || profile.Streaming is null || profile.Headers is null || profile.LibraryIds is null || profile.HiddenSections is null || profile.Downloads is null)
                    throw new InvalidDataException("Invalid server identity.");
                var plain = ProtectedData.Unprotect(Convert.FromBase64String(entry.Secret), Entropy, DataProtectionScope.CurrentUser);
                try
                {
                    var secret = JsonSerializer.Deserialize(plain, StoreJson.Default.Secrets) ?? throw new InvalidDataException("Invalid protected data.");
                    if (secret.Headers is null || secret.Password is null || secret.ApiKey is null || secret.AudioMuseToken is null)
                        throw new InvalidDataException("Invalid protected data.");
                    profile = profile with
                    {
                        Credentials = profile.Credentials with { Password = secret.Password, ApiKey = secret.ApiKey },
                        Headers = secret.Headers,
                        AudioMuse = profile.AudioMuse is { } muse ? muse with { Token = secret.AudioMuseToken } : null
                    };
                    Validate(profile);
                    result.Add(profile);
                }
                finally { CryptographicOperations.ZeroMemory(plain); }
            }
            LastServer = result.Any(profile => profile.Id == document.LastServer) ? document.LastServer : null;
            return result.ToArray();
        }
        catch (Exception error) when (error is JsonException or FormatException or ArgumentException)
        { throw new InvalidDataException("Invalid server store.", error); }
    }
    private async Task WriteAsync(List<Profile> profiles, Guid? lastServer, CancellationToken token)
    {
        var document = new StoredDocument { LastServer = profiles.Any(profile => profile.Id == lastServer) ? lastServer : null };
        foreach (var profile in profiles)
        {
            var secrets = new Secrets { Password = profile.Credentials.Password, ApiKey = profile.Credentials.ApiKey,
                Headers = new Dictionary<string, string>(profile.Headers), AudioMuseToken = profile.AudioMuse?.Token ?? "" };
            var plain = JsonSerializer.SerializeToUtf8Bytes(secrets, StoreJson.Default.Secrets);
            try
            {
                var cipher = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                document.Servers.Add(new StoredProfile
                {
                    Profile = profile with { Credentials = profile.Credentials with { Password = "", ApiKey = "" },
                        Headers = new Dictionary<string, string>(), AudioMuse = profile.AudioMuse is { } muse ? muse with { Token = "" } : null },
                    Secret = Convert.ToBase64String(cipher)
                });
            }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, JsonSerializer.SerializeToUtf8Bytes(document, StoreJson.Default.StoredDocument), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, _path, true);
            LastServer = document.LastServer;
        }
        catch (Exception error)
        {
            try { File.Delete(temporary); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            { throw new AggregateException(error, cleanup); }
            throw;
        }
    }
    private static void Validate(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Name);
        ArgumentNullException.ThrowIfNull(profile.Credentials);
        ArgumentNullException.ThrowIfNull(profile.Headers);
        ArgumentNullException.ThrowIfNull(profile.LibraryIds);
        ArgumentNullException.ThrowIfNull(profile.HiddenSections);
        ArgumentNullException.ThrowIfNull(profile.Streaming);
        ArgumentNullException.ThrowIfNull(profile.Downloads);
        ArgumentNullException.ThrowIfNull(profile.Downloads.Folder);
        if (!Enum.IsDefined(profile.Credentials.Kind)) throw new ArgumentException(nameof(profile));
        if (!Enum.IsDefined(profile.Streaming.Mode) || profile.Streaming.MaximumBitRate < 0
            || profile.Streaming.TimeOffset is { } offset && (!double.IsFinite(offset) || offset < 0))
            throw new ArgumentException(nameof(profile));
        if (profile.Streaming.Mode == StreamingMode.Transcode)
            ArgumentException.ThrowIfNullOrWhiteSpace(profile.Streaming.Format);
        profile.ConnectionOptions().Validate();
        if (profile.Credentials.Kind == AuthenticationKind.ApiKey)
            ArgumentException.ThrowIfNullOrWhiteSpace(profile.Credentials.ApiKey);
        else ArgumentException.ThrowIfNullOrWhiteSpace(profile.Credentials.Username);
        if (profile.AudioMuse is { } muse)
        {
            if (muse.ResultCount is < 1 or > 500) throw new ArgumentException(nameof(profile));
            if (muse.Server is { } server && (!server.IsAbsoluteUri || server.Scheme is not ("http" or "https")
                || server.UserInfo.Length > 0 || server.Query.Length > 0 || server.Fragment.Length > 0))
                throw new ArgumentException(nameof(profile));
        }
    }
}

internal sealed class StoredDocument
{
    public int Version { get; set; } = 1;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LastServer { get; set; }
    public List<StoredProfile> Servers { get; set; } = [];
}
internal sealed class StoredProfile
{
    public required Profile Profile { get; set; }
    public string Secret { get; set; } = "";
}
internal sealed class Secrets
{
    public string Password { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = [];
    public string AudioMuseToken { get; set; } = "";
}
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredDocument))]
[JsonSerializable(typeof(Secrets))]
internal partial class StoreJson : JsonSerializerContext;
