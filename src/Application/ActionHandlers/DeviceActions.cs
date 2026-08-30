using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class DeviceActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly ISpeechOutput _speech;

    internal DeviceActions(ActionRouter router, IMainView view, MediaPlayer player, PlayerSettings settings,
        SettingsStore settingsStore, ISpeechOutput speech)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _settingsStore = settingsStore;
        _speech = speech;
        router.Register(ActionId.SoundCards, SelectDevice);
    }

    private void SelectDevice()
    {
        var devices = _player.GetAudioDevices();
        if (devices.Count == 0)
        {
            _speech.Speak("No sound cards found.", "No devices.");
            return;
        }
        var current = _player.CurrentAudioDevice;
        var selected = Math.Max(0, devices.ToList().FindIndex(device => device.Name == current));
        var index = _view.ChooseAudioDevice(devices.Select(device => device.Description).ToArray(), selected);
        if (!index.HasValue)
            return;
        var device = devices[index.Value];
        if (_player.SetAudioDevice(device.Name))
        {
            _settings.Audio.Device = device.Name;
            _settingsStore.SaveExplicit(_settings);
            _speech.Speak("Sound card set.", "Set.");
        }
        else
        {
            _speech.Speak("Could not set sound card.", "Set failed.");
        }
    }
}
