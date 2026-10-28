using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal static class DocumentWriter
{
    internal static void Export(Document document, string path, Resolution? resolved = null)
    {
        var text = new StringBuilder("#EXTM3U\n");
        foreach (var entry in (resolved ?? DocumentResolver.Resolve(document.Entries)).Files)
        {
            var title = (entry.Title ?? MediaLibrary.DisplayName(entry.Location)).Replace('\r', ' ').Replace('\n', ' ');
            if (entry.Location.Contains('\n') || entry.Location.Contains('\r'))
                throw new InvalidDataException("A playlist location contains a line break.");
            text.Append("#EXTINF:-1,").Append(title).Append('\n').Append(entry.Location).Append('\n');
        }
        if (!AtomicFile.WriteBytes(path, Encoding.UTF8.GetBytes(text.ToString()), out var error)) throw new IOException(error);
    }
}
