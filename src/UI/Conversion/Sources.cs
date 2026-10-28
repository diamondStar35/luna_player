using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI.Conversion;
internal sealed partial class MainDialog
{

    // The text shown for one row of the source list, asked for only as the row is drawn. The full path is
    // held; only the file name is shown, which is what a screen reader should read out for a list of files.
    private string RowText(int index)
        => index >= 0 && index < _files.Count ? MediaLibrary.DisplayName(_files[index]) : string.Empty;
    // Opens a file picker that takes several files at once and adds every one chosen. The picker is filtered
    // to the media types the player knows, so nothing that cannot be converted reaches the list.
    private void AddFiles()
    {
        using var dialog = new FileDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Add file" button opens.
            message: Tr("Add files to convert"),
            wildcard: MediaLibrary.DialogWildcard,
            style: FileDialogStyle.Open | FileDialogStyle.Multiple | FileDialogStyle.FileMustExist);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        AddPaths(dialog.GetPaths());
    }

    // Opens a folder picker and adds every supported file found under it, subfolders included. A folder can
    // hold far more than a file picker ever would, which is the whole reason the source list is virtual.
    private void AddFolder()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the picker the converter's "Add folder" button opens.
            message: Tr("Add a folder of files to convert"),
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() != StandardId.Ok)
            return;
        AddPaths(MediaLibrary.CollectFiles(dialog.Path, recursive: true));
    }
    // Adds paths not already listed, keeping the order they arrived in. The first file added to an empty list
    // decides the default output format, unless the user has since chosen one for themselves.
    private void AddPaths(IReadOnlyList<string> paths)
    {
        var wasEmpty = _files.Count == 0;
        var seen = new HashSet<string>(_files, StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
            if (seen.Add(path))
                _files.Add(path);
        if (wasEmpty && _files.Count > 0)
            ApplyDefaultFormat(_files[0]);
        RefreshList();
    }

    // Takes the selected files back out. Removing from the highest index down leaves the lower indices, which
    // have not yet been touched, still pointing at the same files.
    private void RemoveSelected()
    {
        var indices = _list.GetSelectedIndices();
        Array.Sort(indices);
        for (var i = indices.Length - 1; i >= 0; i--)
            _files.RemoveAt((int)indices[i]);
        RefreshList();
    }

    // Tells the virtual list how many rows it now has - which is enough to redraw it, since it asks for each
    // visible row's text itself - and brings the buttons back into step with the new count.
    private void RefreshList()
    {
        _list.SetItemCount(_files.Count);
        SyncButtons();
    }

}
