namespace LunaPlayer.Tests.Infrastructure;
internal sealed class Workspace : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "Luna-tests-" + Guid.NewGuid().ToString("N"));
    internal Workspace() => Directory.CreateDirectory(Root);
    internal string File(string name, string text = "")
    {
        var path = Path.Combine(Root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, text);
        return path;
    }
    public void Dispose() => Directory.Delete(Root, recursive: true);
}
