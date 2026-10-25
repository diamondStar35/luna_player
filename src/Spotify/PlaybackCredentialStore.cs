using System.Security.Cryptography;
using System.Text.Json;
using LunaPlayer.Configuration;

namespace LunaPlayer.Spotify;

/// <summary>Keeps reusable Spotify playback credentials protected for the Windows user.</summary>
internal static class PlaybackCredentialStore
{
    private static string FilePath => Path.Combine(Paths.RootDirectory, "spotify-playback-credentials.dat");

    internal static AccessPointLogin.Credentials? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var encrypted = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            try
            {
                using var stream = new MemoryStream(plain, writable: false);
                using var reader = new BinaryReader(stream);
                if (reader.ReadInt32() != 1) return null;
                var username = reader.ReadString();
                var type = reader.ReadInt32();
                var length = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(username) || type < 0
                    || length is < 1 or > 65_536 || stream.Length - stream.Position != length) return null;
                var data = reader.ReadBytes(length);
                return new AccessPointLogin.Credentials(username, type, data);
            }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or CryptographicException or FormatException) { return null; }
    }

    /// <summary>Reads the old librespot cache during migration without copying its unprotected JSON.</summary>
    internal static AccessPointLogin.Credentials? LoadLegacy()
    {
        try
        {
            var path = Path.Combine(Paths.RootDirectory, "librespot-rs", "credentials.json");
            if (!File.Exists(path)) return null;
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            var root = document.RootElement;
            var username = root.GetProperty("username").GetString();
            var type = root.GetProperty("auth_type").GetInt32();
            var data = Convert.FromBase64String(root.GetProperty("auth_data").GetString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(username) || type < 0 || data.Length is < 1 or > 65_536) return null;
            return new AccessPointLogin.Credentials(username, type, data);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or JsonException or FormatException or KeyNotFoundException) { return null; }
    }

    internal static void Save(AccessPointLogin.Credentials credentials)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(1);
            writer.Write(credentials.Username ?? string.Empty);
            writer.Write(credentials.Type);
            writer.Write(credentials.Data.Length);
            writer.Write(credentials.Data);
        }
        var plain = stream.ToArray();
        try
        {
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            Paths.EnsureDirectoryFor(FilePath);
            var temporary = Paths.TemporaryFor(FilePath);
            File.WriteAllBytes(temporary, encrypted);
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    internal static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        DeleteLegacy();
    }

    internal static void DeleteLegacy()
    {
        var path = Path.Combine(Paths.RootDirectory, "librespot-rs", "credentials.json");
        if (File.Exists(path)) File.Delete(path);
    }
}
