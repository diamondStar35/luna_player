using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.UI;
using LunaPlayer.Playback;
using LunaPlayer.Media;
using LunaPlayer.YouTube;
using System.Diagnostics;
using PlaybackPreferences = LunaPlayer.Application.Preferences.Playback;

namespace LunaPlayer.Application.Commands.App;

internal sealed class Preferences
{
    internal Preferences(
        ActionRouter router,
        IMainView view,
        Settings settings,
        SettingsStore store,
        BackupService backup,
        FileAssociations associations,
        Player player,
        ShortcutManager shortcuts,
        ShortcutManager globalShortcuts,
        ISpeechOutput speech,
        Backend youTube,
        LunaPlayer.YouTube.Components.Service youTubeComponents,
        Action spotifyQualityChanged)
    {
        void ShowAudioFailure(AudioFilter filter)
        {
            if (filter == AudioFilter.Normalization)
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that evens out the loudness could not be turned on or off.
                    Tr("Could not apply the audio normalization filter."));
            if (filter == AudioFilter.Mono)
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that mixes both channels into one could not be turned on or off.
                    Tr("Could not apply the mono audio filter."));
            if (filter == AudioFilter.Spatialization)
                view.Dialogs.ShowError(
                    // Translators: Shown when headphone spatialization could not load the selected SOFA file or FFmpeg filter.
                    Tr("Could not apply the SOFA headphone spatialization filter. Choose a valid SOFA HRTF file and use an FFmpeg build with libmysofa support."));
            if (filter == AudioFilter.SilenceRemoval)
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that trims the silent parts out could not be turned on or off.
                    Tr("Could not apply the silence removal filter."));
        }

        bool ApplyRuntime(Changes changes)
        {
            if (!PlaybackPreferences.Apply(settings, changes, player, ShowAudioFailure)) return false;
            // Reconcile the Windows media controls with the newly saved preference.
            player.RefreshState();
            if (changes.Contains("shortcuts.primary", "shortcuts.secondary"))
            {
                shortcuts.Apply(settings.Shortcuts.Primary, settings.Shortcuts.Secondary);
                view.ApplyShortcuts(shortcuts);
            }
            if (changes.Contains("shortcuts.global"))
                GlobalShortcutBinder.Apply(view, globalShortcuts, settings, speech: null);
            view.SetSilenceRemovalChecked(player.IsSilenceRemovalEnabled);
            view.SetHeadphoneSpatializationState(player.CanToggleSofa, player.IsSofaEnabled);
            if (changes.Contains("spotify.audioQuality")) spotifyQualityChanged();
            return true;
        }

        bool OpenSettingsFolder()
        {
            try
            {
                Directory.CreateDirectory(Paths.RootDirectory);
                Process.Start(new ProcessStartInfo(Paths.RootDirectory) { UseShellExecute = true });
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        LunaPlayer.UI.Contracts.UiOperation Register(bool unregister)
        {
            string error;
            var success = unregister ? associations.Unregister(out error) : associations.Register(out error);
            return new(success, error);
        }

        // Called straight from the button and returns at once: the fetch runs behind its own progress
        // window, which is what keeps the Preferences window answering for the length of a download.
        void DownloadYouTubeComponents(YtDlpChannel channel) => youTubeComponents.Install(channel);

        void OpenPreferences(bool spotifyCredentials)
        {
            var language = settings.General.Language;
            var baseline = settings.Copy();
            var operations = new LunaPlayer.UI.Preferences.PrefsOps(
                backup.SettingsPath,
                backup.BookmarksPath,
                Paths.RootDirectory,
                destination => backup.ExportSettings(destination, settings),
                backup.ImportSettings,
                backup.ResetSettings,
                backup.ExportBookmarks,
                backup.ImportBookmarks,
                () => backup.LastError,
                OpenSettingsFolder,
                () => Register(false),
                () => Register(true),
                DownloadYouTubeComponents,
                source =>
                {
                    ApplyRuntime(new Changes(settings, source, replaceAll: true));
                    store.SaveExplicit(settings);
                    // Import and reset take effect immediately, even if this dialog is later canceled.
                    baseline = settings.Copy();
                });
            var result = view.Dialogs.EditPreferences(settings, operations, text => speech.SpeakText(text), spotifyCredentials);
            if (result is null) return;
            if (ApplyRuntime(new Changes(baseline, result)) && !store.SaveExplicit(settings))
                view.Dialogs.ShowError(
                    // Translators: Shown when the settings the user accepted could not be written to disk.
                    Tr("Could not save settings."));
            // The language is chosen once, when the player starts: the menus, the action names and every
            // window built so far already hold their text in the old one. Saying so is the whole of what
            // can be done about it without rebuilding all of them.
            if (!string.Equals(language, settings.General.Language, StringComparison.OrdinalIgnoreCase))
                view.Dialogs.ShowInfo(
                    // Translators: Shown after the user picks a different language, because the player only reads it when it starts.
                    Tr("Please restart the app for language changes to take effect."));
        }
        router.Register(ActionId.OpenPreferences, () => OpenPreferences(false));
        router.Register(ActionId.OpenSpotifyCredentials, () => OpenPreferences(true));
    }
}
