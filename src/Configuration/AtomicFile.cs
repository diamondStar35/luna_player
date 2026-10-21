using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LunaPlayer.Configuration;

/// <summary>Writes complete files through a sibling temporary file so a failed save cannot truncate the
/// previous copy.</summary>
internal static class AtomicFile
{
    internal static bool WriteJson<T>(
        string path,
        T value,
        JsonTypeInfo<T> typeInfo,
        out string error)
    {
        return Write(path, stream => JsonSerializer.Serialize(stream, value, typeInfo), out error);
    }

    internal static bool WriteBytes(string path, ReadOnlySpan<byte> content, out string error)
    {
        var snapshot = content.ToArray();
        return Write(path, stream => stream.Write(snapshot), out error);
    }

    private static bool Write(string path, Action<Stream> write, out string error)
    {
        var temporary = Paths.TemporaryFor(path);
        try
        {
            Paths.EnsureDirectoryFor(path);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                write(stream);
            File.Move(temporary, path, overwrite: true);
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = exception.Message;
            return false;
        }
    }
}
