using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Application identity, purpose, version, and project website.</summary>
internal sealed class AboutDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal AboutDialog(Window parent)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window containing Luna Player's version and website.
            title: Tr("About Luna Player"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        var aboutText = string.Join(
            "\n\n",
            // Translators: Description in the Luna Player About window.
            Tr("Luna Player is a fast, accessible media player for Windows. It is designed for effortless " +
               "keyboard control and a clear screen-reader experience, and plays local media, network streams, " +
               "and YouTube content without getting in your way."),
            // Translators: Additional description in the Luna Player About window.
            Tr("Luna Player is free and open-source software."),
            // Translators: Third-party copyright and licensing information in the About window. mpv and
            // python-mpv are project names and should not be translated. NOTICE.txt and licenses are names
            // found in Luna's installation folder.
            Tr("Luna includes mpv, copyright © the mpv developers and contributors to MPlayer and mplayer2, " +
               "and a C# translation of python-mpv, copyright © 2017-2024 Sebastian Götte. These components are licensed under " +
               "the GNU Lesser General Public License, version 2.1 or later. FFmpeg is licensed under the GNU " +
               "Lesser General Public License, version 3 or later. See NOTICE.txt and the licenses folder for " +
               "complete third-party notices and terms."),
            // Translators: Final two lines in the About window. {publisher} is the developer's name and
            // {version} is the installed application version.
            TrFormat("Copyright © 2026 {publisher}\nVersion: {version}", AppInfo.Publisher, AppInfo.Version));
        var description = new TextCtrl(
            _dialog,
            value: aboutText,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        description.InsertionPoint = 0;
        description.ShowPosition(0);
        // Translators: Link in the About window that opens the Luna Player project website.
        var website = new HyperlinkCtrl(_dialog, Tr("Visit the Luna Player website"), AppInfo.RepositoryUrl);

        // Bound to Cancel so both this button and Escape dismiss the dialog.
        var ok = new Button(_dialog, StandardId.Cancel, Tr("OK"));
        ok.SetDefault();
        _dialog.SetEscapeId(StandardId.Cancel);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(description, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 12);
        sizer.Add(website, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 12);
        sizer.Add(ok, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.AlignRight, border: 12);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(620, 380);
        _dialog.Center(onParent: true);
        description.Focus();
    }

    internal void Show() => _dialog.ShowModal();

    public void Dispose() => _dialog.Dispose();
}
