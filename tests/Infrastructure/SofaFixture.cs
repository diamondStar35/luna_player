using System.IO.Compression;

namespace LunaPlayer.Tests.Infrastructure;

internal static class SofaFixture
{
    // Front, left, right, above, below and behind the listener, with distinct left/right
    // impulse amplitudes so rendered-output tests detect direction without an external dataset.
    private const string SyntheticImpulseResponses =
        "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" +
        "AAA=";

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
