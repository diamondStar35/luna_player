using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.UI;
using LunaPlayer.Playback;
using LunaPlayer.Media;
using LunaPlayer.YouTube;
using System.Diagnostics;

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
        void ApplyRuntime(Settings source)
        {
            settings.Apply(source);
            player.TrackPositions(settings.Audio.SaveFilePositions);
            player.SetAudiobookMode(settings.Audio.AudiobookMode);
            player.SetEndBehavior(settings.Audio.EndBehavior);
            player.ConfigurePlaybackEffects(settings.Audio);
            player.ConfigureSilence(settings.Silence);
            if (!player.SetNormalization(settings.Audio.NormalizeAudio))
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that evens out the loudness could not be turned on or off.
                    Tr("Could not apply the audio normalization filter."),
                    // Translators: Title of an error caused by applying a preference.
                    Tr("Preferences"));
            if (!player.SetMono(settings.Audio.MonoAudio))
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that mixes both channels into one could not be turned on or off.
                    Tr("Could not apply the mono audio filter."),
                    // Translators: Title of an error caused by applying a preference.
                    Tr("Preferences"));
            settings.Audio.Pitch = player.SetPitch(settings.Audio.Pitch);
            settings.Audio.Pan = player.SetPan(settings.Audio.Pan);
            if (!player.SetSilenceRemoval(settings.Silence.Enabled))
                view.Dialogs.ShowError(
                    // Translators: Shown when the setting that trims the silent parts out could not be turned on or off.
                    Tr("Could not apply the silence removal filter."),
                    // Translators: Title of an error caused by applying a preference.
                    Tr("Preferences"));
            settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
            settings.Audio.MonoAudio = player.IsMonoEnabled;
            settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
            // Reconcile the Windows media controls with the newly saved preference.
            player.RefreshState();
            shortcuts.Apply(settings.Shortcuts.Primary, settings.Shortcuts.Secondary);
            view.ApplyShortcuts(shortcuts);
            GlobalShortcutBinder.Apply(view, globalShortcuts, settings, speech: null);
            view.SetSilenceRemovalChecked(player.IsSilenceRemovalEnabled);
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

        UiOperation Register(bool unregister)
        {
            string error;
            var success = unregister ? associations.Unregister(out error) : associations.Register(out error);
            return new(success, error);
        }

        // Called straight from the button and returns at once: the fetch runs behind its own progress
        // window, which is what keeps the Preferences window answering for the length of a download.
        void DownloadYouTubeComponents(YtDlpChannel channel) => youTubeComponents.Install(channel);

        var operations = new PrefsOps(
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
                ApplyRuntime(source);
                store.SaveExplicit(settings);
            });

        void OpenPreferences(bool spotifyCredentials)
        {
            var language = settings.General.Language;
            var spotifyQuality = settings.Spotify.AudioQuality;
            var result = view.Dialogs.EditPreferences(settings, operations, text => speech.SpeakText(text), spotifyCredentials);
            if (result is null) return;
            ApplyRuntime(result);
            if (spotifyQuality != settings.Spotify.AudioQuality)
                spotifyQualityChanged();
            if (!store.SaveExplicit(settings))
                view.Dialogs.ShowError(
                    // Translators: Shown when the settings the user accepted could not be written to disk.
                    Tr("Could not save settings."),
                    // Translators: Title of an error caused by saving preferences.
                    Tr("Preferences"));
            // The language is chosen once, when the player starts: the menus, the action names and every
            // window built so far already hold their text in the old one. Saying so is the whole of what
            // can be done about it without rebuilding all of them.
            if (!string.Equals(language, settings.General.Language, StringComparison.OrdinalIgnoreCase))
                view.Dialogs.ShowInfo(
                    // Translators: Shown after the user picks a different language, because the player only reads it when it starts.
                    Tr("Please restart the app for language changes to take effect."),
                    // Translators: Title of the message shown after changing the language preference.
                    Tr("Preferences"));
        }
        router.Register(ActionId.OpenPreferences, () => OpenPreferences(false));
        router.Register(ActionId.OpenSpotifyCredentials, () => OpenPreferences(true));
    }
}
