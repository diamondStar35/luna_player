using LunaPlayer.Playlist;
using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class PlaylistsDialogs(DialogContext context) : IDisposable
{
    private readonly DialogContext _context = context;
    private LunaPlayer.UI.Playlists.ManagerDialog? _manager;
    public Document? EditPlaylist(Document document, Action<string> speak, bool metadataOnly = false)
    {
        using var dialog = new LunaPlayer.UI.Playlists.EditorDialog(_context.Parent, document, _context.Dialogs, speak, metadataOnly);
        return dialog.Show();
    }
    public void ManagePlaylists(IReadOnlyList<Document> documents, Action<LunaPlayer.UI.Playlists.PlaylistRequest> request)
    {
        if (_manager is null)
            _manager = new LunaPlayer.UI.Playlists.ManagerDialog(_context.Frame, documents, request);
        else
            _manager.Refresh(documents);
        _manager.Show();
    }
    public void RefreshPlaylists(IReadOnlyList<Document> documents, Guid? selectedId = null)
        => _manager?.Refresh(documents, selectedId);

    public void Dispose() { _manager?.Dispose(); _manager = null; }
    public string[] ChoosePlaylistFiles()
    {
        // Translators: Title of the multiple-file picker adding playlist entries.
        using var dialog = new FileDialog(_context.Parent, message: Tr("Add files"), wildcard: Media.MediaLibrary.DialogWildcard,
            style: FileDialogStyle.Open | FileDialogStyle.Multiple | FileDialogStyle.FileMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.GetPaths() : [];
    }
    public string? ChoosePlaylistDestination()
    {
        // Translators: Title of the dialog choosing a playlist export filename and format.
        var title = Tr("Save Playlist As");
        // Translators: Save dialog file types. JSON, M3U and M3U8 and all wildcard patterns must remain unchanged.
        var wildcard = Tr("M3U8 playlist (*.m3u8)|*.m3u8|M3U playlist (*.m3u)|*.m3u|JSON playlist (*.json)|*.json");
        using var dialog = new FileDialog(_context.Parent, message: title, wildcard: wildcard,
            style: FileDialogStyle.DefaultSave);
        if (dialog.ShowModal() != StandardId.Ok) return null;
        var extensions = new[] { ".m3u8", ".m3u", ".json" };
        var extension = extensions[Math.Clamp(dialog.FilterIndex, 0, 2)];
        var path = dialog.Path;
        if (!Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.ChangeExtension(path, extension);
            // Translators: Confirmation before replacing an existing playlist after adjusting its filename extension.
            if (File.Exists(path) && !_context.Dialogs.Confirm(Tr("Replace the existing playlist file?"))) return null;
        }
        return path;
    }
}
