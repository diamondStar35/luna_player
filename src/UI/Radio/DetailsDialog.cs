using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed class DetailsDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal DetailsDialog(Window parent, Station station)
    {
        // Translators: Title of the read-only details window for a radio station.
        _dialog = new Dialog(parent, title: Tr("Station details"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var lines = new[]
        {
            // Translators: Radio station details. {name} is the station's name.
            TrFormat("Name: {name}", station.Name),
            // Translators: Radio station details. {country} is the country supplied by Radio Browser.
            TrFormat("Country: {country}", station.Country),
            // Translators: Radio station details. {region} is the state or province supplied by Radio Browser.
            TrFormat("Region: {region}", station.State),
            // Translators: Radio station details. {languages} lists languages spoken by the station.
            TrFormat("Languages: {languages}", station.Language),
            // Translators: Radio station details. {tags} contains the station's descriptive tags.
            TrFormat("Tags: {tags}", station.Tags),
            // Translators: Radio station details. {codec} is an audio codec such as MP3 or AAC.
            TrFormat("Codec: {codec}", station.Codec),
            // Translators: Radio station details. {bitrate} is the bitrate in kilobits per second.
            TrFormat("Bitrate: {bitrate} kbps", station.Bitrate),
            // Translators: Radio station details. {url} is the stream address.
            TrFormat("Stream URL: {url}", Catalog.StreamUrl(station) ?? station.Url),
            // Translators: Radio station details. {url} is the station's website.
            TrFormat("Website: {url}", station.Homepage),
            // Translators: Radio station details. {count} is the station's public vote count.
            TrFormat("Votes: {count}", station.Votes),
            // Translators: Radio station details. {count} is the directory's playback click count.
            TrFormat("Playback clicks: {count}", station.ClickCount),
            // Translators: Radio station details. {value} is the latest check result.
            TrFormat("Last check: {value}", station.LastCheckOk
                // Translators: Radio Browser's latest check found the station available.
                ? Tr("Working")
                // Translators: Radio Browser's latest check did not find the station available.
                : Tr("Unavailable")),
            // Translators: Radio station details. {time} is the timestamp supplied by Radio Browser for the latest check.
            TrFormat("Last checked: {time}", station.LastCheckTime),
            // Translators: Radio station details. {time} is the timestamp supplied by Radio Browser for the latest metadata change.
            TrFormat("Last updated: {time}", station.LastChangeTime),
            // Translators: Radio station details. {id} is the station's unique Radio Browser identifier.
            TrFormat("Station UUID: {id}", station.StationUuid),
        };
        var text = new TextCtrl(_dialog, value: string.Join(Environment.NewLine, lines),
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        text.InsertionPoint = 0;
        // Translators: Button closing the radio station details window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(text, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        layout.Add(close, flags: SizerFlags.All | SizerFlags.AlignRight, border: 8);
        _dialog.SetSizer(layout);
        _dialog.MinSize = new Size(570, 420);
        _dialog.Fit();
        _dialog.Center();
        text.Focus();
    }

    internal void Show() => _dialog.ShowModal();
    public void Dispose() => _dialog.Dispose();
}
