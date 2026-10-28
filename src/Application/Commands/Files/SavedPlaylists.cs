using LunaPlayer.Actions;
using LunaPlayer.Playback;
using LunaPlayer.Playlist;
using LunaPlayer.UI;
using LunaPlayer.Accessibility;

namespace LunaPlayer.Application.Commands.Files;

internal sealed class SavedPlaylists
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly ISpeechOutput _speech;
    private readonly DocumentStore _store = new();
    internal SavedPlaylists(ActionRouter router, IMainView view, Player player, ISpeechOutput speech)
    {
        _view = view; _player = player; _speech = speech;
        router.Register(ActionId.MyPlaylists, () => Guard(Manage));
        router.Register(ActionId.CreatePlaylist, () => Guard(() => Edit(new())));
        router.Register(ActionId.SaveCurrentPlaylist, () => Guard(() => SaveCurrent(false)));
        router.Register(ActionId.ExportPlaylist, () => Guard(() => SaveCurrent(true)));
        router.Register(ActionId.AddPlaylistFiles, () => Guard(() => Append(view.Dialogs.ChoosePlaylistFiles().Select(SavedEntry.FromPath).ToArray())));
        router.Register(ActionId.AddPlaylistFolder, () => Guard(() =>
        {
            if (view.Dialogs.ChooseFolder(string.Empty) is string path) Append([SavedEntry.FromPath(path)]);
        }));
    }
    private void Guard(Action work)
    {
        try { work(); }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        {
            // Translators: Playlist operation failure. {error} contains the reason from the file or storage operation.
            _view.Dialogs.ShowError(TrFormat("Could not complete the playlist operation: {error}", exception.Message), Caption());
        }
    }
    private static string Caption()
    {
        // Translators: Caption of playlist information and error messages.
        return Tr("Playlists");
    }
    private void Report(Resolution resolution)
    {
        if (resolution.Unavailable.Count == 0) return;
        // Translators: Warning listing missing files and inaccessible or empty playlist folders. {paths} is a newline-separated list.
        _view.Dialogs.ShowWarning(TrFormat("Unavailable or empty playlist entries:\n{paths}", string.Join('\n', resolution.Unavailable)), Caption());
    }
    private void Append(IReadOnlyList<SavedEntry> entries)
    {
        if (entries.Count == 0) return;
        // Translators: Reason shown when a playlist operation cannot complete; describes invalid content or an unsupported operation.
        if (_player.InSession) throw new InvalidDataException(Tr("Leave the online session before adding playlist entries."));
        var resolved = DocumentResolver.Resolve(entries);
        Report(resolved);
        _player.AppendSaved(entries, resolved.Files);
        // Translators: Spoken after appending entries to the current playback playlist.
        _speech.SpeakText(Tr("Playlist entries added."));
    }
    private void Edit(Document document, bool metadataOnly = false)
    {
        while (_view.Dialogs.EditPlaylist(document, text => _speech.SpeakText(text), metadataOnly) is Document edited)
        {
            try
            {
                _store.Save(edited);
                // Translators: Spoken confirmation after saving a named playlist.
                _speech.SpeakText(Tr("Playlist saved."));
                return;
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                // Translators: Validation error for a playlist name. {error} is the reason.
                _view.Dialogs.ShowWarning(TrFormat("Could not save the playlist: {error}", exception.Message), Caption());
                document = edited;
            }
        }
    }
    private void SaveCurrent(bool export)
    {
        // Translators: Reason shown when a playlist operation cannot complete; describes invalid content or an unsupported operation.
        if (_player.InSession) throw new InvalidDataException(Tr("Leave the online session before saving the playlist."));
        var document = new Document { Entries = _player.SnapshotSaved().ToList() };
        if (export) Export(document);
        else Edit(document, metadataOnly: true);
    }
    private void Export(Document document)
    {
        if (_view.Dialogs.ChoosePlaylistDestination() is not string path) return;
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            var copy = document.Copy();
            copy.Id = Guid.NewGuid();
            if (copy.Name.Length == 0) copy.Name = Path.GetFileNameWithoutExtension(path);
            if (_view.Dialogs.EditPlaylist(copy, text => _speech.SpeakText(text), metadataOnly: true) is Document named)
                DocumentStore.Write(named, path);
        }
        else
        {
            var resolved = DocumentResolver.Resolve(document.Entries);
            Report(resolved);
            DocumentWriter.Export(document, path, resolved);
        }
    }
    private void Manage()
    {
        while (true)
        {
            var documents = _store.List();
            var request = _view.Dialogs.ManagePlaylists(documents);
            if (request is null) return;
            if (request.Action == "new") { Edit(new()); continue; }
            var document = documents.First(item => item.Id == request.Id);
            switch (request.Action)
            {
                case "edit": Edit(document); break;
                case "export": Export(document); break;
                case "remove":
                    // Translators: Confirmation removing a saved playlist, leaving all media files untouched.
                    if (_view.Dialogs.Confirm(Tr("Remove this saved playlist? Media files will be kept."), Caption())) _store.Remove(document.Id);
                    break;
                case "open":
                    var resolved = DocumentResolver.Resolve(document.Entries);
                    Report(resolved);
                    if (resolved.Files.Count > 0) { _player.OpenSaved(document.Entries, resolved.Files); return; }
                    // Translators: Warning when opening a saved playlist yields no playable tracks.
                    _view.Dialogs.ShowWarning(Tr("The playlist contains no playable entries."), Caption());
                    break;
            }
        }
    }
}
