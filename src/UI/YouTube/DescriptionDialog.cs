using System.Text.RegularExpressions;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>Shows the text an uploader wrote under a video, and offers to open its links, copy it, or save
/// it.</summary>
///
/// <remarks>
/// The box shows a <em>processed</em> copy of the
/// text - each link put on its own line and the blank lines that follow it closed up - so a link can be
/// reached and opened on a line of its own; Enter on a link's line opens it. Copy and the two exports keep
/// the <em>raw</em> text the uploader wrote, save for the HTML export, which wraps the links it shows in
/// anchors. When the uploader wrote nothing, there is nothing to copy or save and those buttons are off.
/// </remarks>
internal sealed partial class DescriptionDialog : IDisposable
{
    // Gruber's "liberal, accurate" URL pattern: it finds the links in a description so each
    // can be put on its own line, opened from its line, and wrapped in an anchor when the text is exported.
    // A verbatim string rather than a raw one so the old xgettext that builds the translation template can
    // still lex this file; the two hold the same pattern, the verbatim form only doubling its one quote.
    [GeneratedRegex(@"\b((?:https?://|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))", RegexOptions.IgnoreCase)]
    private static partial Regex Url();

    private readonly Dialog _dialog;
    private readonly TextCtrl _text;
    private readonly string _raw;
    private readonly string _title;

    internal DescriptionDialog(Window parent, string title, string description)
    {
        _title = title;
        _raw = description;
        _dialog = new Dialog(parent,
            // Translators: Title of the window showing the text an uploader wrote under a YouTube video.
            title: Tr("Video description"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label before the box that shows the text written under a video.
        var label = new StaticText(_dialog, label: Tr("Description"));
        _text = new TextCtrl(_dialog, value: Process(_raw),
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.ProcessEnter | TextCtrlStyle.DontWrap);
        _text.InsertionPoint = 0;
        _text.ShowPosition(0);

        // Translators: Button that copies the whole description to the clipboard.
        var copy = new Button(_dialog, label: Tr("Copy"));
        // Translators: Button that saves the description as a plain-text (.txt) file.
        var txt = new Button(_dialog, label: Tr("Export to text (.txt)..."));
        // Translators: Button that saves the description as a web page (.html) file.
        var html = new Button(_dialog, label: Tr("Export to web page (.html)..."));
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        // Copy and export have nothing to act on when the uploader wrote no description.
        var hasText = _raw.Trim().Length > 0;
        copy.Enabled = hasText;
        txt.Enabled = hasText;
        html.Enabled = hasText;

        copy.Click += (_, _) => OnCopy();
        txt.Click += (_, _) => OnExportText();
        html.Click += (_, _) => OnExportHtml();
        // The box keeps Enter to itself (ProcessEnter) and raises this rather than moving focus on.
        _text.EnterPressed += (_, _) => OpenUrlOnCaretLine();

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(copy, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(txt, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(html, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(label, flags: SizerFlags.All, border: 8);
        sizer.Add(_text, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(500, 360);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _text.Focus();
    }

    internal void Show() => _dialog.ShowModal();
    public void Dispose() => _dialog.Dispose();

    /// <summary>Puts each link on its own line and closes up the blank lines around it: a link
    /// on a line by itself can be reached and opened, where one buried in a paragraph cannot.</summary>
    private static string Process(string content)
    {
        if (content.Length == 0)
            return content;
        var spaced = Url().Replace(content, "\n$1");
        return Regex.Replace(spaced, "\n{2,}", "\n");
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            _dialog.EndModal(StandardId.Cancel);
        else
            args.Skip();
    }

    /// <summary>Opens the first link on the line the caret is on, if any.</summary>
    private void OpenUrlOnCaretLine()
    {
        if (!_text.PositionToXY(_text.InsertionPoint, out _, out var line))
            return;
        var match = Url().Match(_text.GetLineText(line));
        if (match.Success)
            Wx.LaunchDefaultBrowser(match.Value);
    }

    private void OnCopy()
    {
        Clipboard.SetText(_raw);
        _text.Focus();
    }

    private void OnExportText()
    {
        // Translators: File-type filter for exporting a video description as plain text.
        if (ChooseSavePath(".txt", Tr("Text files (*.txt)|*.txt")) is string path)
            Save(path, _raw);
        _text.Focus();
    }

    private void OnExportHtml()
    {
        // Translators: File-type filter for exporting a video description as a web page.
        if (ChooseSavePath(".html", Tr("Web pages (*.html)|*.html")) is string path)
            Save(path, BuildHtml());
        _text.Focus();
    }

    /// <summary>The HTML export: the shown text, its link lines turned into anchors, under
    /// a heading of the video's title.</summary>
    private string BuildHtml()
    {
        var lines = _text.Value.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var match = Url().Match(lines[i]);
            if (match.Success)
                lines[i] = $"<a href=\"{match.Value}\">{match.Value}</a>";
        }
        var body = string.Join("<br>\n", lines);
        return $"<html>\n<head>\n<meta charset='utf-8'>\n<title>{_title}</title>\n</head>\n<body>\n"
            + $"<h1>{_title}</h1>\n<p>{body}</p>\n</body>\n</html>";
    }

    /// <summary>Asks where to save, defaulting the extension when the user left it off.</summary>
    private string? ChooseSavePath(string extension, string wildcard)
    {
        using var dialog = new FileDialog(_dialog,
            // Translators: Title of the window that asks where to save a video's description.
            message: Tr("Save description"),
            wildcard: wildcard,
            style: FileDialogStyle.DefaultSave);
        if (dialog.ShowModal() != StandardId.Ok)
            return null;
        var path = dialog.Path;
        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
    }

    private static void Save(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            // A best-effort export: a write that fails is left silent, rather than raising
            // a modal error over a window the user opened only to read.
        }
    }
}
