using LunaPlayer.Application;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI.Recording;
internal sealed partial class MainDialog
{

    // ---- sources ----

    private void Rebuild(string? select = null)
    {
        var selected = select ?? SelectedId();
        // Replaced in one go rather than cleared and refilled. A list being built item by item is read out
        // as it grows, which is noise.
        _list.Set([.. _sources.All.Select(RecordingSources.Describe)]);
        if (_sources.Count == 0)
        {
            SyncSelection();
            SyncButtons();
            return;
        }
        var index = selected is null
            ? 0
            : _sources.All.ToList().FindIndex(source => source.Id == selected);
        _list.SelectedIndex = index < 0 ? 0 : index;
        SyncSelection();
        SyncButtons();
    }

    private string? SelectedId()
        => _list.SelectedIndex >= 0 && _list.SelectedIndex < _sources.Count
            ? _sources.All[_list.SelectedIndex].Id
            : null;

    private RecordingSource? Selected()
        => SelectedId() is string id ? _sources.Find(id) : null;

    /// <summary>Brings the volume slider on to the row the user is on.</summary>
    /// <remarks>
    /// Guarded, because the custom slider raises its change event even when the value is set in code -
    /// which is what lets a screen reader hear it, and would otherwise mean moving through the list wrote
    /// each row's volume on to the row before it.
    /// </remarks>
    private void SyncSelection()
    {
        _syncing = true;
        try
        {
            _volume.Value = Selected()?.Volume ?? 100;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ApplyVolume()
    {
        if (_syncing)
            return;
        if (Selected() is not RecordingSource source)
            return;
        source.Volume = _volume.Value;
        var index = _list.SelectedIndex;
        _list.SetString(index, RecordingSources.Describe(source));
    }

    private Menu BuildAddMenu()
    {
        var menu = new Menu();
        // Translators: Menu item on the recording window that adds a microphone or line-in as a source.
        menu.Append(_inputId, Tr("Input device..."));
        // Translators: Menu item on the recording window that adds everything a speaker plays as a source.
        menu.Append(_outputId, Tr("System output..."));
        // Translators: Menu item on the recording window that adds one program's sound as a source.
        menu.Append(_processId, Tr("Program..."));
        // Left there but greyed on a Windows too old for it, so the option is visible and its absence
        // explainable rather than simply missing.
        menu.Enable(_processId, AudioCatalog.SupportsProcessCapture);
        return menu;
    }

    private void ShowAddMenu()
    {
        var at = _add.Position;
        _dialog.PopupMenu(_addMenu, new Point(at.X, at.Y + _add.Size.Height));
    }

    private void AddSource(RecordingSourceKind kind)
    {
        if (kind is RecordingSourceKind.Process && !AudioCatalog.SupportsProcessCapture)
        {
            Wx.MessageBox(
                // Translators: Shown when capturing one program's sound is asked for on a version of
                // Windows that cannot do it. Capturing a device still works.
                Tr("This version of Windows cannot capture a single program. Windows 10 version 2004 or later is needed for that; recording from a device still works."),
                Title, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        var source = new RecordingSource { Kind = kind, Name = DefaultName(kind) };
        using var dialog = new SourceDialog(_dialog, _dispatcher, _catalog, AddCaption(kind), source);
        if (dialog.Show() is not RecordingSource added)
            return;
        _sources.Add(added);
        Rebuild(added.Id);
    }

    private void EditSelected()
    {
        if (Selected() is not RecordingSource source)
            return;
        // A copy, so backing out of the window leaves the source as it was.
        using var dialog = new SourceDialog(
            _dialog, _dispatcher, _catalog, EditCaption(source.Kind), source.Copy());
        if (dialog.Show() is not RecordingSource edited)
            return;
        _sources.Update(edited);
        Rebuild(edited.Id);
    }

    private void RemoveSelected()
    {
        if (Selected() is not RecordingSource source)
            return;
        if (Wx.MessageBox(
            // Translators: Asks the user to confirm removing a recording source. {name} is what they called it.
            TrFormat("Remove the source '{name}'?", source.Name),
            // Translators: Title of the window that asks the user to confirm removing a recording source.
            Tr("Confirm remove"),
            MessageBoxStyle.YesNo | MessageBoxStyle.IconQuestion, _dialog) != MessageBoxStyle.Yes)
            return;
        _sources.Remove(source.Id);
        Rebuild();
    }

    private static string DefaultName(RecordingSourceKind kind) => kind switch
    {
        // Translators: The name a newly added microphone source is given until the user changes it.
        RecordingSourceKind.InputDevice => Tr("Microphone"),
        // Translators: The name a newly added speaker-output source is given until the user changes it.
        RecordingSourceKind.OutputLoopback => Tr("System output"),
        // Translators: The name a newly added program source is given until the user changes it.
        _ => Tr("Program"),
    };

    private static string AddCaption(RecordingSourceKind kind) => kind switch
    {
        // Translators: Title of the window for adding a microphone or line-in as a recording source.
        RecordingSourceKind.InputDevice => Tr("Add input device"),
        // Translators: Title of the window for adding a speaker's output as a recording source.
        RecordingSourceKind.OutputLoopback => Tr("Add system output"),
        // Translators: Title of the window for adding one program's sound as a recording source.
        _ => Tr("Add program"),
    };

    private static string EditCaption(RecordingSourceKind kind) => kind switch
    {
        // Translators: Title of the window for changing a microphone or line-in recording source.
        RecordingSourceKind.InputDevice => Tr("Edit input device"),
        // Translators: Title of the window for changing a speaker-output recording source.
        RecordingSourceKind.OutputLoopback => Tr("Edit system output"),
        // Translators: Title of the window for changing a program recording source.
        _ => Tr("Edit program"),
    };

}
