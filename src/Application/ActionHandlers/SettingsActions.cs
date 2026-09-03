using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.UI;
using LunaPlayer.Playback;
using LunaPlayer.Media;
using LunaPlayer.YouTube;
using System.Diagnostics;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class SettingsActions
{
    internal SettingsActions(
        ActionRouter router,
        IMainView view,
        PlayerSettings settings,
        SettingsStore store,
        BackupService backup,
        FileAssociations associations,
        MediaPlayer player,
        ShortcutManager shortcuts,
        ShortcutManager globalShortcuts,
        ISpeechOutput speech,
        Backend youTube,
        Components youTubeComponents)
    {
        void ApplyRuntime(PlayerSettings source)
        {
            settings.Apply(source);
            player.TrackPositions(settings.Audio.SaveFilePositions);
            player.SetEndBehavior(settings.Audio.EndBehavior);
            player.ConfigureSilence(settings.Silence);
            if (!player.SetNormalization(settings.Audio.NormalizeAudio))
                view.ShowError(
                    // Translators: Shown when the setting that evens out the loudness could not be turned on or off.
                    Tr("Could not apply the audio normalization filter."), Tr("Preferences"));
            if (!player.SetMono(settings.Audio.MonoAudio))
                view.ShowError(
                    // Translators: Shown when the setting that mixes both channels into one could not be turned on or off.
                    Tr("Could not apply the mono audio filter."), Tr("Preferences"));
            if (!player.SetSilenceRemoval(settings.Silence.Enabled))
                view.ShowError(
                    // Translators: Shown when the setting that trims the silent parts out could not be turned on or off.
                    Tr("Could not apply the silence removal filter."), Tr("Preferences"));
            settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
            settings.Audio.MonoAudio = player.IsMonoEnabled;
            settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
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

        // The tick box goes back only when the user actually said no. While the programs are on their way
        // it stays as they left it, and the setting falls back to the player's own resolver until they
        // arrive. Ticking the box is itself a request for them, so the offer is made even to somebody who
        // once said "stop asking" - that answer was about being interrupted, not about this.
        bool EnsureYouTubeComponents(YtDlpChannel channel)
            => youTubeComponents.Ensure(channel, ignoreSkip: true) is not Components.ComponentsState.Declined;

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
            EnsureYouTubeComponents,
            source =>
            {
                ApplyRuntime(source);
                store.SaveExplicit(settings);
            });

        router.Register(ActionId.OpenPreferences, () =>
        {
            var language = settings.General.Language;
            var result = view.EditPreferences(settings, operations, text => speech.SpeakText(text));
            if (result is null) return;
            ApplyRuntime(result);
            if (!store.SaveExplicit(settings))
                view.ShowError(
                    // Translators: Shown when the settings the user accepted could not be written to disk.
                    Tr("Could not save settings."), Tr("Preferences"));
            // The language is chosen once, when the player starts: the menus, the action names and every
            // window built so far already hold their text in the old one. Saying so is the whole of what
            // can be done about it without rebuilding all of them.
            if (!string.Equals(language, settings.General.Language, StringComparison.OrdinalIgnoreCase))
                view.ShowInfo(
                    // Translators: Shown after the user picks a different language, because the player only reads it when it starts.
                    Tr("Please restart the app for language changes to take effect."), Tr("Preferences"));
        });
    }
}
