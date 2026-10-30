using System.IO.Compression;

namespace LunaPlayer.Tests.Infrastructure;

internal static class SofaFixture
{
    private const string SyntheticImpulseResponses =
        "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";

    internal static string Write(Workspace workspace, string name = "synthetic.sofa")
    {
        var path = Path.Combine(workspace.Root, name);
        using var compressed = new MemoryStream(Convert.FromBase64String(SyntheticImpulseResponses));
        using var decoded = new GZipStream(compressed, CompressionMode.Decompress);
        using var output = File.Create(path);
        decoded.CopyTo(output);
        return path;
    }
}
