using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI.Conversion;
internal sealed partial class MainDialog
{

    // Where converted files go until the user picks somewhere else: a "converted" folder under the player's
    // own folder in the user's Documents. Not created here - nothing is written until a conversion runs.
    private static string DefaultDestination()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Luna Player", "converted");

    private void OnFormatChanged()
    {
        if (!_choosingFormat)
            _formatUserSet = true;
        SyncFormatOptions();
    }
    // Moves the format choice to the one that fits the first file added: its own extension when that is an
    // audio format we can write, MP3 otherwise - which is the case for a video file, whose audio we keep. Does
    // nothing once the user has picked a format, so their choice is never overwritten by adding more files.
    private void ApplyDefaultFormat(string firstAddedPath)
    {
        if (_formatUserSet)
            return;
        var extension = Path.GetExtension(firstAddedPath);
        var index = Array.FindIndex(Formats, f => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            index = Array.FindIndex(Formats, f => f.Extension == ".mp3");
        // Guarded so the change made here is not taken for the user's own and left stuck for good.
        _choosingFormat = true;
        _format.SelectedIndex = index;
        _choosingFormat = false;
        // A programmatic selection does not always raise the changed event, so the options are synced by hand.
        SyncFormatOptions();
    }

    // Shows only the options the chosen format can honour, and hides the rest so a screen reader moving through
    // the window never meets a control that means nothing: no quality for a lossless format, no sample rate for
    // Opus - which is fixed at 48 kHz and offers no way to change it - and the Opus-only bitrate mode and
    // profile for Opus alone.
    private void SyncFormatOptions()
    {
        var extension = Formats[_format.SelectedIndex].Extension;
        var lossless = Lossless.Contains(extension);
        var opus = extension.Equals(".opus", StringComparison.OrdinalIgnoreCase);
        _sampleRateLabel.Show(!opus);
        _sampleRate.Show(!opus);
        _qualityLabel.Show(!lossless);
        _quality.Show(!lossless);
        _opusModeLabel.Show(opus);
        _opusMode.Show(opus);
        _opusProfileLabel.Show(opus);
        _opusProfile.Show(opus);
        _dialog.Layout();
    }
    // Shows the destination folder box and its Browse button only when the files are not being written beside
    // the ones they came from. With the box hidden, a screen reader skips straight past a control that would
    // have no bearing on where anything ends up.
    private void SyncDestination()
    {
        var custom = !_preserveLocations.Checked;
        _destinationLabel.Show(custom);
        _destination.Show(custom);
        _browse.Show(custom);
        _dialog.Layout();
    }

    // Remove acts on the selection, Convert on the list as a whole, so each is offered only when it has
    // something to work on.
    private void SyncButtons()
    {
        _remove.Enabled = _list.SelectedCount > 0;
        _convert.Enabled = _files.Count > 0;
    }

    // Opens a folder picker starting at whatever the box already holds, and writes the chosen folder back into
    // it. Only the box is touched; nothing is created until a conversion runs.
    private void BrowseDestination()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Browse" button opens for the destination folder.
            message: Tr("Choose where converted files are written"),
            defaultPath: _destination.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _destination.Value = dialog.Path;
    }

    // The encoder settings for the chosen format: which encoder writes it, and the sample rate, channel count
    // and bitrate to write it at - each null when the file's own value is kept. Lossless formats carry no
    // bitrate, and Opus carries no sample rate, matching the controls SyncFormatOptions leaves on screen.
    private ConversionSettings BuildSettings(string extension)
    {
        var lossless = Lossless.Contains(extension);
        var opus = extension.Equals(".opus", StringComparison.OrdinalIgnoreCase);
        int? sampleRate = opus || _sampleRate.SelectedIndex == 0 ? null : SampleRates[_sampleRate.SelectedIndex - 1];
        int? channels = _channels.SelectedIndex switch { 1 => 1, 2 => 2, _ => null };
        int? bitrate = lossless || _quality.SelectedIndex == 0 ? null : Bitrates[_quality.SelectedIndex - 1];
        var opusSettings = opus
            ? new OpusSettings(
                VariableBitrate: _opusMode.SelectedIndex == 1,
                // Music tunes for general audio, speech for voice, low delay for the shortest latency; these
                // are the three yt-dlp-independent applications libopus takes.
                Application: _opusProfile.SelectedIndex switch { 1 => "voip", 2 => "lowdelay", _ => "audio" })
            : (OpusSettings?)null;
        return new ConversionSettings(RequestBuilder.CodecFor(extension), sampleRate, channels, bitrate, opusSettings);
    }

}
