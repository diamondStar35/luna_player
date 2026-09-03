using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>Asks which half of a link naming a video and a playlist at once the user meant.</summary>
/// <remarks>
/// Three buttons rather than a list and an OK: there are exactly two answers and a way out, and a button
/// each says what will happen in the words the user would use. The caller opens this only when the setting
/// says to ask, so reaching it means the question is real.
/// </remarks>
internal sealed class LinkKindDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal LinkKindDialog(Window parent)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window asking whether to play the video or open the playlist.
            title: Tr("Open YouTube Link"),
            style: DialogStyle.Default);

        // Translators: Message shown when a YouTube address names a video and a playlist at the same time,
        // asking which of the two the user wants.
        var message = new StaticText(_dialog, label: Tr("The app detected that this YouTube link contains a playlist and a video ID. Choose how you want to proceed."));

        // Translators: Button that plays the one video a link names, rather than the playlist it also names.
        var video = new Button(_dialog, StandardId.Yes, Tr("Play the video"));
        video.SetDefault();
        // Translators: Button that lists every video in the playlist a link names, rather than the one video.
        var playlist = new Button(_dialog, StandardId.No, Tr("Open the playlist"));
        // Translators: The button that closes a window and leaves everything as it was.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        // Bound by hand: wxWidgets ends a modal dialog by itself only for OK and Cancel, so a Yes or a No
        // button that nothing listens to does nothing at all when pressed.
        video.Click += (_, _) => _dialog.EndModal(StandardId.Yes);
        playlist.Click += (_, _) => _dialog.EndModal(StandardId.No);
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(video, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(playlist, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.AlignRight, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.Center(onParent: true);
        video.Focus();
    }

    internal YouTubeLinkKind? Show()
    {
        // The stock ids are resolved by wx at run time rather than being constants, so this cannot be a
        // switch over them.
        var answer = _dialog.ShowModal();
        if (answer == StandardId.Yes)
            return YouTubeLinkKind.Video;
        return answer == StandardId.No ? YouTubeLinkKind.Playlist : null;
    }

    public void Dispose() => _dialog.Dispose();
}
