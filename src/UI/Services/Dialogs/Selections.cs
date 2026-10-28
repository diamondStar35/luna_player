using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class SelectionsDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;

    public double? ChooseSelectionTime(string title, string prompt, double duration, double elapsed)
    {
        using var dialog = new LunaPlayer.UI.Selections.TimeDialog(_context.Parent, title, prompt, duration, elapsed);
        return dialog.Show();
    }

    public string? ChooseSelectionDestination(string source, bool video)
    {
        var directory = File.Exists(source) ? Path.GetDirectoryName(source) ?? string.Empty : string.Empty;
        // Translators: Default base name for a selected part when the source has no usable file name.
        var sourceName = File.Exists(source) ? Path.GetFileNameWithoutExtension(source) : Tr("selection");
        // Translators: Default file name for a saved selection. {name} is the source file's base name.
        var name = TrFormat("{name} - selection", sourceName);
        var formats = SelectionFormats(video);
        var extensions = new HashSet<string>(formats.Select(format => format.Extension), StringComparer.OrdinalIgnoreCase);
        var sourceExtension = Path.GetExtension(source);
        var selectedFormat = Array.FindIndex(formats,
            format => format.Extension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase));
        if (selectedFormat < 0)
            selectedFormat = 0;
        name += formats[selectedFormat].Extension;
        var wildcard = string.Join('|', formats.Select(format => $"{format.Label}|*{format.Extension}"));
        using var dialog = new FileDialog(_context.Parent,
            // Translators: Title of the file picker used to save the currently selected part of a file.
            message: Tr("Save selection"), directory: directory, fileName: name,
            wildcard: wildcard, style: FileDialogStyle.DefaultSave);
        dialog.FilterIndex = selectedFormat;
        if (dialog.ShowModal() != StandardId.Ok)
            return null;
        var path = dialog.Path;
        var chosenFormat = Math.Clamp(dialog.FilterIndex, 0, formats.Length - 1);
        var chosenExtension = Path.GetExtension(path);
        // Windows leaves the pre-filled extension in the name when the file-type choice changes. Replace
        // that untouched default so the type combo really does choose the format; a different supported
        // extension typed into the name is an explicit choice and wins.
        if (chosenFormat != selectedFormat
            && chosenExtension.Equals(formats[selectedFormat].Extension, StringComparison.OrdinalIgnoreCase))
            return Path.ChangeExtension(path, formats[chosenFormat].Extension);
        return extensions.Contains(chosenExtension)
            ? path
            : path + formats[chosenFormat].Extension;
    }

    public LunaPlayer.UI.Selections.SelectionSaveOptions? ChooseSelectionBatch(string source, bool video)
    {
        var folder = File.Exists(source)
            ? Path.GetDirectoryName(source) ?? string.Empty
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        // Translators: Default common name for numbered files created by Save Selection Points.
        var name = Tr("selection");
        using var dialog = new LunaPlayer.UI.Selections.SaveDialog(
            _context.Parent, SelectionFormats(video), folder, name, Path.GetExtension(source));
        return dialog.Show();
    }

    private static LunaPlayer.UI.Selections.SelectionFileFormat[] SelectionFormats(bool video) => video
        ?
        [
            // Translators: MP4 choice in the format list when saving selected parts of a video.
            new(Tr("MP4 video (*.mp4)"), ".mp4"),
            // Translators: Matroska choice in the format list when saving selected parts of a video.
            new(Tr("Matroska video (*.mkv)"), ".mkv"),
            // Translators: WebM choice in the format list when saving selected parts of a video.
            new(Tr("WebM video (*.webm)"), ".webm"),
            // Translators: QuickTime choice in the format list when saving selected parts of a video.
            new(Tr("QuickTime video (*.mov)"), ".mov"),
            // Translators: AVI choice in the format list when saving selected parts of a video.
            new(Tr("AVI video (*.avi)"), ".avi"),
        ]
        :
        [
            // Translators: MP3 choice in the format list when saving selected parts of an audio file.
            new(Tr("MP3 audio (*.mp3)"), ".mp3"),
            // Translators: M4A choice in the format list when saving selected parts of an audio file.
            new(Tr("M4A audio (*.m4a)"), ".m4a"),
            // Translators: FLAC choice in the format list when saving selected parts of an audio file.
            new(Tr("FLAC audio (*.flac)"), ".flac"),
            // Translators: WAV choice in the format list when saving selected parts of an audio file.
            new(Tr("WAV audio (*.wav)"), ".wav"),
            // Translators: Ogg Vorbis choice in the format list when saving selected parts of an audio file.
            new(Tr("Ogg Vorbis audio (*.ogg)"), ".ogg"),
            // Translators: Opus choice in the format list when saving selected parts of an audio file.
            new(Tr("Opus audio (*.opus)"), ".opus"),
        ];
}
