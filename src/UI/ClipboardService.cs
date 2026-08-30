using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed class WxClipboardService : IClipboardService
{
    public bool SetText(string text)
    {
        if (!Clipboard.Open())
            return false;
        try
        {
            return Clipboard.SetText(text) && Clipboard.Flush();
        }
        finally
        {
            Clipboard.Close();
        }
    }

    public bool SetFiles(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0 || !Clipboard.Open())
            return false;
        try
        {
            return Clipboard.SetFiles([.. paths]) && Clipboard.Flush();
        }
        finally
        {
            Clipboard.Close();
        }
    }

    public IReadOnlyList<string> GetPaths()
    {
        if (!Clipboard.Open())
            return [];
        try
        {
            var files = Clipboard.GetFiles();
            if (files.Length > 0)
                return files;
            return ParseText(Clipboard.GetText());
        }
        finally
        {
            Clipboard.Close();
        }
    }

    private static IReadOnlyList<string> ParseText(string text)
    {
        var result = new List<string>();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var value = line.Trim('"');
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
                value = uri.LocalPath;
            if (File.Exists(value) || Directory.Exists(value))
                result.Add(value);
        }
        return result;
    }
}
