using System.Text;

namespace LunaPlayer.Tests.Infrastructure;

internal static class TaggedWave
{
    internal static string Write(Workspace workspace, string name, string? title = null, string? artist = null)
    {
        var path = NativeHost.Wave(workspace, name);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
        using var writer = new BinaryWriter(stream);
        stream.Position = stream.Length;
        writer.Write("LIST"u8);
        var sizePosition = stream.Position;
        writer.Write(0);
        writer.Write("INFO"u8);
        foreach (var (field, value) in new[] { ("INAM", title), ("IART", artist) })
        {
            if (value is null) continue;
            var bytes = Encoding.UTF8.GetBytes(value + '\0');
            writer.Write(Encoding.ASCII.GetBytes(field));
            writer.Write(bytes.Length);
            writer.Write(bytes);
            if (bytes.Length % 2 != 0) writer.Write((byte)0);
        }
        var end = stream.Position;
        stream.Position = sizePosition;
        writer.Write((int)(end - sizePosition - sizeof(int)));
        stream.Position = 4;
        writer.Write((int)(end - 8));
        return path;
    }
}
