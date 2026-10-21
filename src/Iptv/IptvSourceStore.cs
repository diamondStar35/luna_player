using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Iptv;

/// <summary>The IPTV sources the user has saved to come back to.</summary>
///
/// <remarks>
/// Modelled on <see cref="LunaPlayer.Favorites.FavoriteStore"/>: a whole-file JSON document written through a
/// temporary copy so a failed write leaves the previous file intact, a missing file read as an empty store,
/// and an unreadable or invalid file blocking mutations so it cannot be overwritten by accident.
///
/// Credential fields are protected at rest by <see cref="CredentialProtection"/> (Windows DPAPI) on the way
/// out in <see cref="Save"/> and read back in <see cref="TryLoadCurrent"/>; the rest of the class holds and
/// works with them in the clear and does not care that the file keeps them protected.
/// </remarks>
internal sealed class IptvSourceStore
{
    private readonly string _path;

    internal IptvSourceStore(string path) => _path = path;

    internal string FilePath => _path;

    /// <summary>Why the last read or write failed, or an empty string when it did not.</summary>
    internal string LastError { get; private set; } = string.Empty;

    /// <summary>Every saved source, oldest first.</summary>
    internal IReadOnlyList<IptvSource> ListAll()
    {
        if (!TryLoadCurrent(out var document))
            return [];
        return document.Items
            .OrderBy(source => source.Created)
            .ThenBy(source => source.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal IptvSource? Get(string id)
    {
        var target = (id ?? string.Empty).Trim();
        return target.Length == 0 ? null : ListAll().FirstOrDefault(source => source.Id == target);
    }

    /// <summary>Saves a new source. Returns null, with <see cref="LastError"/> set, when the draft is not
    /// valid or the file could not be written.</summary>
    internal IptvSource? Add(IptvSourceDraft draft)
    {
        if (!Check(draft, out var source))
            return null;
        source.Id = Guid.NewGuid().ToString("N");
        source.Created = Precision.Normalize(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
        if (!TryLoadCurrent(out var document))
            return null;
        document.Items.Add(source);
        return Save(document) ? source : null;
    }

    /// <summary>Changes a saved source in place, keeping its id and when it was created.</summary>
    internal bool Update(string id, IptvSourceDraft draft)
    {
        var target = (id ?? string.Empty).Trim();
        if (target.Length == 0 || !Check(draft, out var replacement))
            return false;
        if (!TryLoadCurrent(out var document))
            return false;
        var existing = document.Items.FirstOrDefault(source => source.Id.Trim() == target);
        if (existing is null)
            return Report(false, NotFound);
        existing.Name = replacement.Name;
        existing.Kind = replacement.Kind;
        existing.Url = replacement.Url;
        existing.Username = replacement.Username;
        existing.Password = replacement.Password;
        existing.MacAddress = replacement.MacAddress;
        existing.Serial = replacement.Serial;
        existing.EpgUrl = replacement.EpgUrl;
        existing.PreferHls = replacement.PreferHls;
        return Save(document);
    }

    internal bool Delete(string id)
    {
        var target = (id ?? string.Empty).Trim();
        if (target.Length == 0)
            return Report(false, NotFound);
        if (!TryLoadCurrent(out var document))
            return false;
        if (document.Items.RemoveAll(source => source.Id.Trim() == target) == 0)
            return Report(false, NotFound);
        return Save(document);
    }

    /// <summary>Whether a draft can be saved, and why not when it cannot. The rules depend on the kind: an
    /// M3U URL and an Xtream server must be web addresses, an M3U file must exist, a Stalker source needs a
    /// portal URL and a MAC address. Nothing here touches the network.</summary>
    /// <param name="error">Empty when the draft may be saved; otherwise a message to show the user.</param>
    internal static bool Validate(IptvSourceDraft draft, out string error)
    {
        if (draft.Name.Trim().Length == 0)
        {
            // Translators: Shown when the user saves an IPTV source without typing a name for it.
            error = Tr("Name is required.");
            return false;
        }
        var address = draft.Url.Trim();
        switch (draft.Kind)
        {
            case IptvSourceKind.M3uUrl:
            case IptvSourceKind.Xtream:
            case IptvSourceKind.Stalker:
                if (!Media.LinkValidator.IsHttpUrl(address))
                {
                    // Translators: Shown when an IPTV source's address is not a web address. "http" and
                    // "https" are protocol names and are not translated.
                    error = Tr("The address must start with http or https.");
                    return false;
                }
                break;
            case IptvSourceKind.M3uFile:
                if (address.Length == 0)
                {
                    // Translators: Shown when the user saves a file-based IPTV source without choosing a file.
                    error = Tr("A playlist file is required.");
                    return false;
                }
                break;
        }
        if (draft.Kind == IptvSourceKind.Xtream
            && (draft.Username.Trim().Length == 0 || draft.Password.Length == 0))
        {
            // Translators: Shown when an Xtream IPTV source is saved without a username and password.
            error = Tr("Xtream sources need a username and password.");
            return false;
        }
        if (draft.Kind == IptvSourceKind.Stalker && draft.MacAddress.Trim().Length == 0)
        {
            // Translators: Shown when a Stalker portal IPTV source is saved without a MAC address.
            error = Tr("Stalker sources need a MAC address.");
            return false;
        }
        error = string.Empty;
        return true;
    }

    /// <summary>What a source of this kind is called, for a list or a message.</summary>
    internal static string Describe(IptvSourceKind kind) => kind switch
    {
        // Translators: The kind of an IPTV source: an extended M3U playlist at a web address.
        IptvSourceKind.M3uUrl => Tr("M3U web address"),
        // Translators: The kind of an IPTV source: an extended M3U playlist file on this computer.
        IptvSourceKind.M3uFile => Tr("M3U file"),
        // Translators: The kind of an IPTV source: a provider portal using the Xtream Codes API.
        IptvSourceKind.Xtream => Tr("Xtream Codes"),
        // Translators: The kind of an IPTV source: a set-top-box portal using the Stalker protocol.
        _ => Tr("Stalker portal"),
    };

    private bool Check(IptvSourceDraft draft, out IptvSource source)
    {
        source = draft.ToSource();
        return Validate(draft, out var error) ? Report(true, error) : Report(false, error);
    }

    private static string NotFound =>
        // Translators: Shown when the user edits or removes an IPTV source that is no longer saved.
        Tr("That IPTV source is no longer saved.");

    private bool Report(bool success, string error)
    {
        LastError = success ? string.Empty : error;
        return success;
    }

    private bool TryLoadCurrent(out IptvSourceDocument document)
    {
        if (!File.Exists(_path))
        {
            document = new IptvSourceDocument();
            return Report(true, string.Empty);
        }
        try
        {
            using var stream = File.OpenRead(_path);
            document = JsonSerializer.Deserialize(stream, IptvJsonContext.Default.IptvSourceDocument)
                ?? throw new JsonException("The IPTV sources file is empty.");
            ValidateDocument(document);
            foreach (var source in document.Items)
            {
                source.Created = Precision.Normalize(source.Created);
                source.Username = CredentialProtection.Unprotect(source.Username);
                source.Password = CredentialProtection.Unprotect(source.Password);
            }
            return Report(true, string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            document = new IptvSourceDocument();
            return Report(false, exception.Message);
        }
    }

    private static void ValidateDocument(IptvSourceDocument document)
    {
        if (document.Version < 1 || document.Items is null)
            throw new JsonException("The IPTV sources file does not contain a valid items collection.");
        foreach (var source in document.Items)
        {
            if (source is null
                || string.IsNullOrWhiteSpace(source.Id)
                || string.IsNullOrWhiteSpace(source.Name)
                || !Enum.IsDefined(source.Kind)
                || !double.IsFinite(source.Created)
                || source.Created < 0)
            {
                throw new JsonException("The IPTV sources file contains an invalid entry.");
            }
        }
    }

    private bool Save(IptvSourceDocument document)
    {
        var written = AtomicFile.WriteJson(
            _path, Protected(document), IptvJsonContext.Default.IptvSourceDocument, out var error);
        return Report(written, error);
    }

    /// <summary>A copy of the document with credential fields protected for writing, leaving the in-memory
    /// sources - which callers keep using in the clear - untouched. Credentials are the only fields that
    /// change; everything else is copied across as-is.</summary>
    private static IptvSourceDocument Protected(IptvSourceDocument document)
    {
        var copy = new IptvSourceDocument { Version = document.Version };
        foreach (var source in document.Items)
        {
            copy.Items.Add(new IptvSource
            {
                Id = source.Id,
                Name = source.Name,
                Kind = source.Kind,
                Url = source.Url,
                Username = CredentialProtection.Protect(source.Username),
                Password = CredentialProtection.Protect(source.Password),
                MacAddress = source.MacAddress,
                Serial = source.Serial,
                EpgUrl = source.EpgUrl,
                PreferHls = source.PreferHls,
                Created = source.Created,
            });
        }
        return copy;
    }
}
