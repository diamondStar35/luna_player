using LunaPlayer.Media.Discs;
using WxSharp;

namespace LunaPlayer.UI.Discs;

internal sealed record DiscSelection(string Location, DiscFormat Format, int? Title);

internal sealed class Picker : FileDialogCustomizeHook, IDisposable
{
    private readonly DirDialog _dialog;
    private readonly Discovery _discovery = new(Wx.CallAfter);
    private readonly DiscFormat _initial;
    private FileDialogChoice? _type, _title;
    private FileDialogStaticText? _status;
    private DiscInfo? _disc;
    private string _location = string.Empty;
    private DiscFormat _format;
    private bool _disposed;
    internal DiscSelection? Accepted { get; private set; }

    internal Picker(DirDialog dialog, DiscFormat initial)
    {
        _dialog = dialog;
        _initial = initial;
        _discovery.Completed += Complete;
    }
    public override void AddCustomControls(FileDialogCustomize customizer)
    {
        // Translators: Label of the DVD format choice in the native Open DVD folder picker.
        var typeLabel = Tr("Disc type");
        _type = customizer.AddChoice([
            // Translators: DVD format choice for movie/video discs, including their audio soundtracks.
            Tr("DVD-Video"),
            // Translators: DVD format choice for genuine AUDIO_TS audio discs, not a DVD-Video soundtrack.
            Tr("DVD-Audio")], typeLabel);
        _type.SelectedIndex = (int)_initial;
        // Translators: Label of the DVD program/title choice in the native Open DVD folder picker.
        _title = customizer.AddChoice([Automatic()], Tr("Title"));
        _title.SelectedIndex = 0;
        _title.Disable();
        // Translators: Initial status in the Open DVD folder picker, before a disc location is selected.
        _status = customizer.AddStaticText(Tr("Choose a DVD drive or folder."));
        _type.SelectionChanged += (_, _) => UpdateCustomControls();
    }
    private static string Automatic()
        // Translators: DVD title choice that lets the player select the longest playable title.
        => Tr("Automatic");

    public override void UpdateCustomControls()
    {
        if (_disposed || _type is null) return;
        var location = _dialog.CurrentlySelectedPath;
        var format = (DiscFormat)_type.SelectedIndex;
        if (string.Equals(location, _location, StringComparison.OrdinalIgnoreCase) && format == _format) return;
        _location = location;
        _format = format;
        _disc = null;
        _title!.Set([Automatic()]);
        _title.SelectedIndex = 0;
        _title.Disable();
        if (string.IsNullOrWhiteSpace(location))
        {
            _discovery.Cancel();
            // Translators: Status when the Open DVD picker is at a virtual location such as This PC.
            _status!.SetLabelText(Tr("Choose a DVD drive or folder."));
            return;
        }
        // Translators: Status while DVD titles are inspected in the background in the Open DVD picker.
        _status!.SetLabelText(Tr("Reading DVD titles..."));
        _discovery.Request(location, format);
    }
    private void Complete(DiscoveryResult result)
    {
        if (_disposed) return;
        _disc = result.Disc;
        if (_disc is null)
        {
            _status!.SetLabelText(Describe(result.Error!, _format));
            return;
        }
        _title!.Set(new[] { Automatic() }.Concat(_disc.Titles.Select(title =>
            // Translators: A numbered DVD program in the native title choice. {number} is its one-based title number.
            TrFormat("Title {number}", title.Index + 1))));
        _title.SelectedIndex = 0;
        _title.Enable(_disc.Titles.Count > 1);
        // Translators: Status after DVD inspection. {count} is the number of playable programs, excluding the disc menu.
        _status!.SetLabelText(TrFormat("Playable titles: {count}", _disc.Titles.Count));
    }
    public override void TransferDataFromCustomControls()
    {
        var location = _dialog.CurrentlySelectedPath;
        var same = string.Equals(location, _location, StringComparison.OrdinalIgnoreCase)
            && (DiscFormat)_type!.SelectedIndex == _format;
        int? title = same && _disc is not null && _title!.SelectedIndex > 0
            ? _disc.Titles[_title.SelectedIndex - 1].Index : null;
        Accepted = new(location, (DiscFormat)_type!.SelectedIndex, title);
    }
    internal static string Describe(DiscException error, DiscFormat format) => error.Failure switch
    {
        // Translators: Open DVD error when DVD-Audio was selected but the disc has no playable audio titles.
        Failure.NoAudioTitles => Tr("No audio titles were found on the selected DVD-Audio disc."),
        // Translators: Open DVD error when the DVD-Video structure contains no playable programs.
        Failure.NoVideoTitles => Tr("No video titles were found on the selected DVD-Video disc."),
        Failure.InvalidDisc => format == DiscFormat.Audio
            // Translators: Open DVD error when the chosen location has no valid DVD-Audio structure.
            ? Tr("The selected location does not contain a valid DVD-Audio disc.")
            // Translators: Open DVD error when the chosen location has no valid DVD-Video structure.
            : Tr("The selected location does not contain a valid DVD-Video disc."),
        // Translators: Open DVD error when the disc cannot be read or its playback backend fails.
        _ => Tr("Could not read the selected DVD disc. Check that it is accessible and supported by the playback libraries."),
    };
    public void Dispose()
    {
        _disposed = true;
        _discovery.Dispose();
    }
}
