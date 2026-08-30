namespace LunaPlayer.Application;

internal interface IClipboardService
{
    bool SetText(string text);
    bool SetFiles(IReadOnlyList<string> paths);
    IReadOnlyList<string> GetPaths();
}
