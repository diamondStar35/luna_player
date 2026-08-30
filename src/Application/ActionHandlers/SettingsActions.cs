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
        ApplicationPaths paths,
        ISpeechOutput speech)
    {
        void ApplyRuntime(PlayerSettings source)
        {
            settings.Apply(source);
            player.TrackPositions(settings.Audio.SaveFilePositions);
            player.ConfigureSilence(settings.Silence);
            if (!player.SetNormalization(settings.Audio.NormalizeAudio))
                view.ShowError("Could not apply the audio normalization filter.", "Preferences");
            if (!player.SetMono(settings.Audio.MonoAudio))
                view.ShowError("Could not apply the mono audio filter.", "Preferences");
            if (!player.SetSilenceRemoval(settings.Silence.Enabled))
                view.ShowError("Could not apply the silence removal filter.", "Preferences");
            settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
            settings.Audio.MonoAudio = player.IsMonoEnabled;
            settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
            shortcuts.Apply(settings.Shortcuts.Primary, settings.Shortcuts.Secondary);
            view.ApplyShortcuts(shortcuts);
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
            if (!store.SaveExplicit(settings)) view.ShowError("Could not save settings.", "Preferences");
        });
    }
}
