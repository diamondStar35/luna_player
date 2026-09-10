using System.Globalization;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using WxSharp;

namespace LunaPlayer.UI.Equalizer;

/// <summary>What the user settled on, or null when they cancelled.</summary>
internal sealed record EqualizerEditResult(string Name, IReadOnlyList<Band> Slots);

/// <summary>Everything the editor needs to open on one preset.</summary>
/// <param name="Preview">Applies what is on screen to what is playing. Null when the preset being edited
/// is not the one in force, since previewing it would change the sound to something the user did not
/// ask for.</param>
internal sealed record EqualizerEditContext(
    string Title,
    string Name,
    bool CanRename,
    string BasePresetId,
    IReadOnlyList<Band> Slots,
    Library Library,
    Action<IReadOnlyList<Band>>? Preview);

/// <summary>The window for editing one preset, band by band.</summary>
///
/// <remarks>
/// Every band is a group of its own holding three fields. Two of them are plain text boxes rather than
/// spin controls, and that is an accessibility decision rather than a stylistic one: a double-valued spin
/// control is announced badly by screen readers, and an integer one cannot hold a gain of 6.5 at all.
/// A text box says what it is called and takes whatever the user types, which is checked when they save.
/// The frequency is a whole number of hertz, so it keeps its spin control.
///
/// Labels are created immediately before the control they name. Windows works out the accessible name of
/// an unnamed input from the order its siblings were created in, so building all the labels first and all
/// the inputs afterwards would leave every field unnamed however the sizer arranged them.
/// </remarks>
internal sealed class EqualizerEditDialog : IDisposable
{
    private const int ResetId = 17001;

    private sealed record BandRow(
        int Slot, BandType Type, string Label, SpinCtrl Frequency, TextCtrl Gain, TextCtrl Q);

    private readonly Dialog _dialog;
    private readonly TextCtrl _name;
    private readonly Choice _base;
    private readonly List<BandRow> _rows = [];
    private readonly IReadOnlyList<PresetEntry> _presets;
    private readonly Func<string, Band[]> _slotsOf;
    private readonly Func<string, Band[]> _defaultsOf;
    private readonly Action<IReadOnlyList<Band>>? _preview;
    private readonly string _title;
    private bool _loading;

    /// <param name="canRename">False for a preset the player ships. Its name is translated and belongs to
    /// the player, so it is shown but not editable.</param>
    /// <param name="preview">Applies what is on screen to what is playing, so the user can judge a change
    /// by ear instead of having to save first. Null when nothing is playing this would affect.</param>
    internal EqualizerEditDialog(
        Window parent,
        string title,
        string name,
        bool canRename,
        string basePresetId,
        IReadOnlyList<Band> slots,
        IReadOnlyList<PresetEntry> presets,
        Func<string, Band[]> slotsOf,
        Func<string, Band[]> defaultsOf,
        Action<IReadOnlyList<Band>>? preview)
    {
        _title = title;
        _presets = presets;
        _slotsOf = slotsOf;
        _defaultsOf = defaultsOf;
        _preview = preview;
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label of the list for choosing which preset a preset being edited starts out from.
        var baseLabel = new StaticText(_dialog, label: Tr("Base preset"));
        _base = new Choice(_dialog);
        foreach (var preset in presets)
            _base.Add(preset.Name);
        _base.SelectedIndex = Math.Max(0, IndexOf(basePresetId));
        _base.SelectionChanged += (_, _) => LoadFromBase();

        // Translators: Label of the box holding the name of the equalizer preset being edited.
        var nameLabel = new StaticText(_dialog, label: Tr("Name"));
        _name = new TextCtrl(_dialog, value: name);
        _name.Enabled = canRename;

        var bands = new ScrolledWindow(_dialog);
        bands.SetScrollRate(0, 8);
        // Before the sizer goes on, not after. Setting a sizer lays the window out, and a scrolled window
        // that lays out without knowing how big it is meant to be works out its scrollbars from the size
        // it happens to have, changes its client area by showing them, and lays out again. With seventeen
        // groups inside it that is not a cost worth paying twice.
        bands.MinSize = new Size(420, 320);
        var bandsSizer = new BoxSizer(Orientation.Vertical);
        for (var slot = 0; slot < slots.Count; slot++)
            bandsSizer.Add(BuildBand(bands, slot, slots[slot]),
                flags: SizerFlags.All | SizerFlags.Expand, border: 6);
        bands.SetSizer(bandsSizer);

        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Button in the equalizer editor that puts every band back the way the chosen base preset ships.
        var reset = new Button(_dialog, ResetId, Tr("Reset to defaults"));
        reset.Click += (_, _) => LoadSlots(_defaultsOf(SelectedBaseId()));
        buttons.Add(reset, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        // Translators: Button in the equalizer editor that keeps the changes.
        var save = new Button(_dialog, StandardId.Ok, Tr("Save"));
        save.SetDefault();
        save.Click += OnSave;
        buttons.Add(save, flags: SizerFlags.BorderRight, border: 6);
        // Translators: The button that closes a window and leaves everything as it was.
        buttons.Add(new Button(_dialog, StandardId.Cancel, Tr("Cancel")));

        const SizerFlags Side = SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom;
        var root = new BoxSizer(Orientation.Vertical);
        root.Add(baseLabel, flags: SizerFlags.All, border: 8);
        root.Add(_base, flags: Side | SizerFlags.Expand, border: 8);
        root.Add(nameLabel, flags: Side, border: 8);
        root.Add(_name, flags: Side | SizerFlags.Expand, border: 8);
        root.Add(bands, proportion: 1, flags: Side | SizerFlags.Expand, border: 8);
        root.Add(buttons, flags: Side | SizerFlags.Expand, border: 8);
        // Sized outright rather than fitted. Fit measures the whole tree to find a size that is then
        // thrown away, since the band list scrolls and the dialog is never meant to be as tall as its
        // contents.
        _dialog.MinSize = new Size(480, 520);
        _dialog.Size = new Size(480, 620);
        _dialog.SetSizer(root);
        _dialog.Center(onParent: true);
        // A preset being made starts on the base list, because choosing what to start from comes before
        // naming it. One being edited starts on its name, which is the field most likely to be wanted.
        (canRename && name.Length > 0 ? (Window)_name : _base).Focus();
    }

    internal EqualizerEditResult? Show()
        => _dialog.ShowModal() == StandardId.Ok
            ? new EqualizerEditResult(_name.Value.Trim(), ReadSlots())
            : null;

    public void Dispose() => _dialog.Dispose();

    private StaticBoxSizer BuildBand(Window parent, int slot, Band band)
    {
        var label = BandLabel(slot, band);
        var box = new StaticBox(parent, label);
        var group = new StaticBoxSizer(box);

        // Translators: Label of the box holding the centre frequency of one equalizer band, in hertz.
        var frequencyLabel = new StaticText(box, label: Tr("Frequency (Hz)"));
        var frequency = new SpinCtrl(box,
            (int)Math.Round(band.Frequency),
            (int)Bands.MinimumFrequency,
            (int)Bands.MaximumFrequency);
        // Translators: Label of the box holding how far one equalizer band lifts or cuts, in decibels.
        var gainLabel = new StaticText(box, label: Tr("Gain (dB)"));
        var gain = new TextCtrl(box, value: Format(band.Gain));
        // Translators: Label of the box holding how wide one equalizer band is. Q is the usual name for it and is normally left as it is.
        var qLabel = new StaticText(box, label: Tr("Q (width)"));
        var q = new TextCtrl(box, value: Format(band.Q));

        frequency.ValueChanged += (_, _) => OnEdited();
        frequency.TextChanged += (_, _) => OnEdited();
        gain.TextChanged += (_, _) => OnEdited();
        q.TextChanged += (_, _) => OnEdited();

        const SizerFlags Side = SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom;
        group.Add(frequencyLabel, flags: SizerFlags.All, border: 6);
        group.Add(frequency, flags: Side | SizerFlags.Expand, border: 6);
        group.Add(gainLabel, flags: Side, border: 6);
        group.Add(gain, flags: Side | SizerFlags.Expand, border: 6);
        group.Add(qLabel, flags: Side, border: 6);
        group.Add(q, flags: Side | SizerFlags.Expand, border: 6);
        _rows.Add(new BandRow(slot, band.Type, label, frequency, gain, q));
        return group;
    }

    /// <summary>What a band's group is called. The shelves are named for what they are; the peaks are
    /// named for where the player puts them, which is how a user recognises the one they want.</summary>
    private static string BandLabel(int slot, Band band) => band.Type switch
    {
        // Translators: Title of the group of settings for the equalizer band that lifts or cuts everything below a frequency.
        BandType.LowShelf => Tr("Low shelf"),
        // Translators: Title of the group of settings for the equalizer band that lifts or cuts everything above a frequency.
        BandType.HighShelf => Tr("High shelf"),
        // Translators: Title of the group of settings for one equalizer band. {frequency} is the band's frequency in hertz, such as 250.
        _ => TrFormat("{frequency} Hz",
            Bands.Frequencies[slot - Bands.FirstPeakingSlot]
                .ToString("0", CultureInfo.InvariantCulture)),
    };

    private void LoadFromBase() => LoadSlots(_slotsOf(SelectedBaseId()));

    private void LoadSlots(IReadOnlyList<Band> slots)
    {
        _loading = true;
        try
        {
            foreach (var row in _rows)
            {
                if (row.Slot >= slots.Count)
                    continue;
                var band = slots[row.Slot];
                row.Frequency.Value = (int)Math.Round(band.Frequency);
                row.Gain.Value = Format(band.Gain);
                row.Q.Value = Format(band.Q);
            }
        }
        finally
        {
            _loading = false;
        }
        OnEdited();
    }

    /// <summary>Sends what is on screen to whatever is playing, so long as all of it makes sense. A value
    /// half typed is not an error to complain about, it is simply not ready to be heard yet.</summary>
    private void OnEdited()
    {
        if (_loading || _preview is null)
            return;
        if (TryReadSlots(out var slots, out _, out _))
            _preview(slots);
    }

    private void OnSave(object? sender, CommandEventArgs args)
    {
        if (_name.Enabled && _name.Value.Trim().Length == 0)
        {
            Complain(
                // Translators: Shown when the user saves an equalizer preset without giving it a name.
                Tr("Enter a name for the preset."), _name);
            return;
        }
        if (!TryReadSlots(out _, out var message, out var offender))
        {
            Complain(message, offender);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    private void Complain(string message, Window? focus)
    {
        Wx.MessageBox(message, _title, MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
        focus?.Focus();
    }

    private IReadOnlyList<Band> ReadSlots()
        => TryReadSlots(out var slots, out _, out _) ? slots : [];

    /// <summary>Reads every band, saying which field is wrong and where it is when one of them is.
    /// </summary>
    private bool TryReadSlots(
        out IReadOnlyList<Band> slots, out string message, out Window? offender)
    {
        var read = new List<Band>(_rows.Count);
        foreach (var row in _rows)
        {
            if (!TryNumber(row.Gain.Value, Bands.MinimumGain,
                    Bands.MaximumGain, out var gain))
            {
                slots = [];
                offender = row.Gain;
                // Translators: Shown when a gain typed into the equalizer editor is not a number or is out of range. {band} names the band, such as "250 Hz"; {minimum} and {maximum} are the smallest and largest allowed.
                message = TrFormat("The gain for {band} must be a number between {minimum} and {maximum} decibels.",
                    row.Label, Format(Bands.MinimumGain), Format(Bands.MaximumGain));
                return false;
            }
            if (!TryNumber(row.Q.Value, Bands.MinimumQ,
                    Bands.MaximumQ, out var q))
            {
                slots = [];
                offender = row.Q;
                // Translators: Shown when a Q typed into the equalizer editor is not a number or is out of range. {band} names the band, such as "250 Hz"; {minimum} and {maximum} are the smallest and largest allowed. Q is the usual name for a band's width.
                message = TrFormat("The Q for {band} must be a number between {minimum} and {maximum}.",
                    row.Label, Format(Bands.MinimumQ), Format(Bands.MaximumQ));
                return false;
            }
            var frequency = Math.Clamp(row.Frequency.Value,
                Bands.MinimumFrequency, Bands.MaximumFrequency);
            read.Add(new Band(frequency, q, gain, row.Type));
        }
        slots = read;
        message = string.Empty;
        offender = null;
        return true;
    }

    private string SelectedBaseId()
    {
        var index = _base.SelectedIndex;
        return index >= 0 && index < _presets.Count ? _presets[index].Id : Presets.CustomId;
    }

    private int IndexOf(string id)
    {
        for (var index = 0; index < _presets.Count; index++)
        {
            if (string.Equals(_presets[index].Id, id, StringComparison.Ordinal))
                return index;
        }
        return -1;
    }

    private static bool TryNumber(string text, double minimum, double maximum, out double value)
        => double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value) && value >= minimum && value <= maximum;

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
