using LunaPlayer.Application;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI.Recording;
internal sealed partial class MainDialog
{

    // ---- settings ----

    private void OnFormatChanged()
    {
        var compressed = AudioCatalog.HasBitrate(Format());
        _bitrateLabel.Enabled = compressed;
        _bitrate.Enabled = _engine.State is RecordingState.Idle && compressed;
        LoadSupport();
    }

    /// <summary>Asks what the chosen format can be written as, and rebuilds the rate list from it.
    /// </summary>
    /// <remarks>
    /// The lists are not the same for every format: MP3 stops at 48 kHz, FLAC starts at 44.1, and AAC
    /// does 96 kHz in stereo only. Offering the rest and failing later is what this replaces.
    /// </remarks>
    private void LoadSupport()
    {
        var format = Format();
        var wantedRate = Rate();
        var wantedChannels = Channels();
        _ = Task.Run(() =>
        {
            var support = _catalog.Support(format);
            _dispatcher.Post(() => ApplySupport(support, wantedRate, wantedChannels));
        });
    }

    private void ApplySupport(FormatSupport support, int wantedRate, int wantedChannels)
    {
        if (_closed)
            return;
        _support = support;
        _rates = support.Rates;
        _filling = true;
        _rate.Clear();
        foreach (var rate in _rates)
            _rate.Add(Hertz(rate));
        _rate.SelectedIndex = RateIndex(_rates, wantedRate);
        _filling = false;
        FillChannels(wantedChannels);
    }

    private void OnRateChanged()
    {
        if (_filling)
            return;
        FillChannels(Channels());
    }

    /// <summary>Rebuilds the channel list for whichever rate is chosen, then the bitrates for both.
    /// </summary>
    private void FillChannels(int wanted)
    {
        _choices.SelectRate(_rate.SelectedIndex);
        _filling = true;
        _channels.Clear();
        foreach (var count in _channelCounts)
            _channels.Add(count == 1
                // Translators: One of the channel counts a recording can be made in: one channel.
                ? Tr("Mono")
                // Translators: One of the channel counts a recording can be made in: two channels.
                : Tr("Stereo"));
        var chosen = _channelCounts.ToList().IndexOf(wanted);
        _channels.SelectedIndex = chosen < 0 ? _channelCounts.Count - 1 : chosen;
        _filling = false;
        LoadBitrates();
    }

    /// <summary>Asks Windows what bitrates the chosen format will take, off the UI thread.</summary>
    /// <remarks>
    /// The answer depends on the rate and the channel count as well as the format, so it is asked again
    /// whenever any of the three changes rather than listed once.
    /// </remarks>
    private void LoadBitrates()
    {
        if (_filling)
            return;
        var format = Format();
        if (!AudioCatalog.HasBitrate(format))
        {
            _bitrate.Clear();
            _bitrates = [];
            _bitrateLabel.Enabled = false;
            _bitrate.Enabled = false;
            return;
        }
        var rate = Rate();
        var channels = Channels();
        var wanted = _sources.Options.Bitrate;
        _bitrate.Enabled = false;
        _ = Task.Run(() =>
        {
            var rates = _catalog.Bitrates(format, rate, channels);
            _dispatcher.Post(() => FillBitrates(rates, wanted));
        });
    }

    private void FillBitrates(IReadOnlyList<int> rates, int wanted)
    {
        if (_closed)
            return;
        _bitrates = rates;
        _bitrate.Clear();
        foreach (var rate in rates)
            _bitrate.Add(Kilobits(rate));
        if (rates.Count == 0)
            return;
        // The nearest offered rate to the one already chosen, so changing the sample rate does not throw
        // away a bitrate the user picked just because that exact figure is no longer on the list.
        var best = 0;
        for (var index = 1; index < rates.Count; index++)
        {
            if (Math.Abs(rates[index] - wanted) < Math.Abs(rates[best] - wanted))
                best = index;
        }
        _bitrate.SelectedIndex = best;
        // The lookup runs on a worker and may have started before recording began. Do not let its
        // delayed result unlock a setting that is fixed for the recording already in progress.
        _bitrate.Enabled = _engine.State is RecordingState.Idle;
    }

    private void Browse()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the window that asks where recordings should be saved.
            message: Tr("Select the folder for recordings"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.Value = dialog.Path;
    }

    private RecordingOptions Options() => new(
        Format(), Rate(), Channels(),
        _bitrates.Count > 0 && _bitrate.SelectedIndex >= 0 && _bitrate.SelectedIndex < _bitrates.Count
            ? _bitrates[_bitrate.SelectedIndex]
            : _sources.Options.Bitrate,
        _folder.Value.Trim());

    private RecordingFormat Format() => (RecordingFormat)Math.Max(0, _format.SelectedIndex);

    private int Rate() => _rates.Count == 0
        ? _sources.Options.SampleRate
        : _rates[Math.Clamp(_rate.SelectedIndex, 0, _rates.Count - 1)];

    private int Channels() => _channelCounts.Count == 0
        ? _sources.Options.Channels
        : _channelCounts[Math.Clamp(_channels.SelectedIndex, 0, _channelCounts.Count - 1)];

    // ---- wording ----

    private Choice Choice(IEnumerable<string> values, int selected)
    {
        var choice = new Choice(_dialog);
        foreach (var value in values)
            choice.Add(value);
        choice.SelectedIndex = selected;
        return choice;
    }

    private static void Add(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    /// <summary>Where a rate sits in a list, falling back to the nearest one rather than the first: a
    /// format that cannot do 96000 should land the user on 48000, not on 22050.</summary>
    private static int RateIndex(IReadOnlyList<int> rates, int rate)
    {
        if (rates.Count == 0)
            return -1;
        var best = 0;
        for (var index = 1; index < rates.Count; index++)
        {
            if (Math.Abs(rates[index] - rate) < Math.Abs(rates[best] - rate))
                best = index;
        }
        return best;
    }

    // Translators: A sample rate, as shown in a list. {value} is a number such as 44100.
    private static string Hertz(int value) => TrFormat("{value} Hz", value);

    // Translators: A bitrate, as shown in a list. {value} is a number such as 192.
    private static string Kilobits(int value) => TrFormat("{value} kbps", value / 1000);

}
