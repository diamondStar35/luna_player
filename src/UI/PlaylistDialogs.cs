using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI;

internal sealed partial class DialogService
{
    public Document? EditPlaylist(Document document, Action<string> speak, bool metadataOnly = false)
    {
        using var dialog = new PlaylistEditorDialog(DialogParent, document, this, speak, metadataOnly);
        return dialog.Show();
    }
    public PlaylistRequest? ManagePlaylists(IReadOnlyList<Document> documents)
    {
        using var dialog = new PlaylistManagerDialog(DialogParent, documents);
        return dialog.Show();
    }
    public string[] ChoosePlaylistFiles()
    {
        // Translators: Title of the multiple-file picker adding playlist entries.
        using var dialog = new FileDialog(DialogParent, message: Tr("Add files"), wildcard: Media.MediaLibrary.DialogWildcard,
            style: FileDialogStyle.Open | FileDialogStyle.Multiple | FileDialogStyle.FileMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.GetPaths() : [];
    }
    public string? ChoosePlaylistDestination()
    {
        // Translators: Title of the dialog choosing a playlist export filename and format.
        var title = Tr("Save Playlist As");
        // Translators: Save dialog file types. JSON, M3U and M3U8 and all wildcard patterns must remain unchanged.
        var wildcard = Tr("M3U8 playlist (*.m3u8)|*.m3u8|M3U playlist (*.m3u)|*.m3u|JSON playlist (*.json)|*.json");
        using var dialog = new FileDialog(DialogParent, message: title, wildcard: wildcard,
            style: FileDialogStyle.DefaultSave);
        if (dialog.ShowModal() != StandardId.Ok) return null;
        var extensions = new[] { ".m3u8", ".m3u", ".json" };
        var extension = extensions[Math.Clamp(dialog.FilterIndex, 0, 2)];
        var path = dialog.Path;
        if (!Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.ChangeExtension(path, extension);
            // Translators: Confirmation before replacing an existing playlist after adjusting its filename extension.
            if (File.Exists(path) && !Confirm(Tr("Replace the existing playlist file?"), title)) return null;
        }
        return path;
    }
}
