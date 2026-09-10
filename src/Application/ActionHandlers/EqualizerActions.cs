using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.UI.Equalizer;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>Choosing an equalizer preset, editing one, and managing the user's own.</summary>
///
/// <remarks>
/// Choosing a preset does not come through <see cref="ActionRouter"/>: which presets exist changes while
/// the player is running, so there is no fixed action for one, and the menu names them instead. The two
/// commands that open windows are ordinary actions and can be given a shortcut like anything else.
/// </remarks>
internal sealed class EqualizerActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly ISpeechOutput _speech;
    private readonly Library _library;

    internal EqualizerActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        SettingsStore settingsStore,
        ISpeechOutput speech,
        Library library)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _speech = speech;
        _library = library;
        _view.EqualizerPresetRequested += Choose;
        _library.Changed += OnLibraryChanged;
        router.Register(ActionId.EditEqualizerPreset, EditCurrent);
        router.Register(ActionId.ManageEqualizerPresets, Manage);
    }

    /// <summary>Puts the saved preset back at startup, without saying anything about it.</summary>
    /// <remarks>
    /// Silent because this is not something the user just did. A preset that has since been removed - or
    /// one the user deleted in an earlier session - leaves the equalizer flat and the menu on Off, which
    /// is truthful: the setting names a curve this player no longer has.
    /// </remarks>
    internal void Restore() => Apply(Current(), remember: false);

    /// <summary>The preset in force, or null when the equalizer is off or the saved name no longer names
    /// anything.</summary>
    private Preset? Current()
        => _settings.Equalizer.Enabled ? _library.Effective(_settings.Equalizer.Preset) : null;

    private void OnLibraryChanged() => _view.RebuildEqualizerMenu(_library.All, Current()?.Id);

    private void Choose(string? presetId)
    {
        var preset = presetId is null ? null : _library.Effective(presetId);
        if (presetId is not null && preset is null)
            return;
        Apply(preset, remember: true);
    }

    /// <summary>Sends a preset to the player and puts the menu and the settings in step with it.</summary>
    private void Apply(Preset? preset, bool remember)
    {
        if (!_player.SetEqualizer(preset))
        {
            _speech.Speak(
                // Translators: Spoken when the equalizer preset the user chose could not be applied.
                Tr("Could not change the equalizer."),
                // Translators: The short wording spoken when the equalizer preset could not be applied.
                Tr("Equalizer failed."));
            // Back to whatever is actually in force, so the ticked item never claims more than happened.
            _view.SetEqualizerPreset(_player.CurrentEqualizer?.Id);
            return;
        }
        if (remember)
        {
            _settings.Equalizer.Enabled = preset is not null;
            // The name is kept when the equalizer goes off, so switching it on again returns to this curve.
            if (preset is not null)
                _settings.Equalizer.Preset = preset.Id;
            _settingsStore.SaveExplicit(_settings);
        }
        _view.SetEqualizerPreset(preset?.Id);
        // Nothing is spoken. The choice was made in a menu, which the screen reader has already read out
        // as the user moved onto it and ticked; saying it again only repeats what they just heard.
        // A failure above is different, and is still announced - that is something they cannot see.
    }

    /// <summary>Opens the editor on the preset the equalizer is set to, whether or not it is switched on.
    /// </summary>
    /// <remarks>
    /// A preset the player ships can be edited here as readily as one the user made. What is kept is not
    /// the same in the two cases - the user's own preset is rewritten, while a shipped one gains a record
    /// of what was changed about it - but that is <see cref="Library"/>'s business, not this
    /// method's.
    /// </remarks>
    private void EditCurrent()
    {
        var id = _library.Entry(_settings.Equalizer.Preset) is PresetEntry saved
            ? saved.Id
            : Presets.CustomId;
        if (_library.Entry(id) is not PresetEntry entry)
            return;

        // Heard as it is typed, but only when this preset is the one playing. Previewing a preset that is
        // not in force would change the sound to something the user never asked to hear.
        var live = _settings.Equalizer.Enabled
            && string.Equals(id, _settings.Equalizer.Preset, StringComparison.Ordinal);
        var context = new EqualizerEditContext(
            // Translators: Title of the window for changing an equalizer preset. {name} is the preset's name.
            TrFormat("Edit {name}", entry.Name),
            entry.Name,
            CanRename: !entry.IsSystem,
            id,
            _library.Slots(id),
            _library,
            live ? slots => _player.SetEqualizer(new Preset(id, entry.Name, slots)) : null);

        var result = _view.EditEqualizerPreset(context);
        if (result is null || !_library.Save(id, result.Name, result.Slots))
        {
            if (result is not null)
                Report(_library.LastError);
            // Whether they cancelled or the write failed, what is playing is still whatever the preview
            // last sent. Put the saved preset back so the sound matches what the menu says again.
            if (live)
                Apply(Current(), remember: false);
            return;
        }
        Apply(Current(), remember: false);
    }

    private void Manage()
    {
        if (!_view.ManageEqualizerPresets(_library))
            return;
        // The preset in force may have been edited, or deleted out from under the setting. Current()
        // answers null in the second case, which switches the equalizer off rather than leaving it on a
        // curve that no longer exists.
        if (Current() is null && _settings.Equalizer.Enabled)
        {
            _settings.Equalizer.Enabled = false;
            _settingsStore.SaveExplicit(_settings);
        }
        Apply(Current(), remember: false);
    }

    private void Report(string error)
        => _view.ShowError(
            error.Length > 0
                ? error
                // Translators: Shown when an equalizer preset could not be saved and there is no more detail to give.
                : Tr("The preset could not be saved."),
            // Translators: Title of the window shown when an equalizer preset could not be saved.
            Tr("Equalizer"));
}
