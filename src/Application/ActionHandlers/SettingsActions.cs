using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.UI;
using LunaPlayer.Playback;
using LunaPlayer.Media;
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
        ApplicationPaths paths,
        ISpeechOutput speech)
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
                Directory.CreateDirectory(paths.RootDirectory);
                Process.Start(new ProcessStartInfo(paths.RootDirectory) { UseShellExecute = true });
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

        var operations = new PrefsOps(
            backup.SettingsPath,
            backup.BookmarksPath,
            paths.RootDirectory,
            destination => backup.ExportSettings(destination, settings),
            backup.ImportSettings,
            backup.ResetSettings,
            backup.ExportBookmarks,
            backup.ImportBookmarks,
            OpenSettingsFolder,
            () => Register(false),
            () => Register(true),
            source =>
            {
                ApplyRuntime(source);
                store.SaveExplicit(settings);
            });

        router.Register(ActionId.OpenPreferences, () =>
        {
            var result = view.EditPreferences(settings, operations, text => speech.SpeakText(text));
            if (result is null) return;
            ApplyRuntime(result);
            if (!store.SaveExplicit(settings))
                view.ShowError(
                    // Translators: Shown when the settings the user accepted could not be written to disk.
                    Tr("Could not save settings."), Tr("Preferences"));
        });
    }
}
