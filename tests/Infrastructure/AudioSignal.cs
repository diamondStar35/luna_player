namespace LunaPlayer.Tests.Infrastructure;

internal static class AudioSignal
{
    internal static short[] Tone(int frames, int rate = 48000)
        => Enumerable.Range(0, frames)
            .Select(index => (short)(2000 * Math.Sin(2 * Math.PI * 523.25 * index / rate))).ToArray();

    internal static string Wave(Workspace workspace, string name, ReadOnlySpan<short> samples, int rate = 48000)
    {
        var path = Path.Combine(workspace.Root, name);
        using var writer = new BinaryWriter(File.Create(path));
        var bytes = samples.Length * sizeof(short);
        writer.Write("RIFF"u8); writer.Write(bytes + 36); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate);
        writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(bytes);
        foreach (var sample in samples) writer.Write(sample);
        return path;
    }

    internal static float[] ReadFloats(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.AreEqual(0, bytes.Length % sizeof(float));
        var samples = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
        return samples;
    }
}
