using System.Collections.ObjectModel;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>Puts the configured system-wide shortcuts into force and tells the user when the system would not
/// let the player watch the keyboard. Shared by startup and by Preferences so both apply the same set.
/// </summary>
internal static class GlobalShortcutBinder
{
    private static readonly string Failure =
        // Translators: Spoken or shown when the player could not start watching the keyboard, so the shortcuts that
        // work while another program is in front will do nothing.
        Tr("Global shortcuts could not be set up and will not work.");

    /// <param name="speech">Where to report a failure at startup. A message box there would block the window
    /// the user is waiting for, so it is spoken instead; pass null from Preferences, where the user has just
    /// changed a shortcut and is expecting to see the answer.</param>
    internal static void Apply(
        IMainView view, ShortcutManager shortcuts, PlayerSettings settings, ISpeechOutput? speech)
    {
        // Global actions have no secondary slot, so only the primary set is ever populated.
        shortcuts.Apply(settings.Shortcuts.Global, ReadOnlyDictionary<ActionId, Shortcut>.Empty);
        if (view.ApplyGlobalShortcuts(shortcuts)) return;
        if (speech is not null)
            speech.Speak(Failure, Failure);
        else
            view.ShowWarning(Failure, Tr("Global Shortcuts"));
    }
}
