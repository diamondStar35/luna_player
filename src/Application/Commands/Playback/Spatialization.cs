using System.Globalization;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.Commands.Playback;

/// <summary>Session-only headphone controls. Saved audio settings supply steps, never runtime values.</summary>
internal sealed class Spatialization
{
    private readonly Player _player;
    private readonly ISpeechOutput _speech;
    private readonly Action<bool, bool> _setMenuState;

    internal Spatialization(ActionRouter router, Player player, AudioSettings settings,
        ISpeechOutput speech, Action<bool, bool> setMenuState)
    {
        _player = player;
        _speech = speech;
        _setMenuState = setMenuState;
        router.Register(ActionId.ToggleHeadphoneSpatialization, Toggle);
        router.Register(ActionId.RotateSofaLeft, () => ChangeRotation(-settings.SofaRotationStep));
        router.Register(ActionId.RotateSofaRight, () => ChangeRotation(settings.SofaRotationStep));
        router.Register(ActionId.SofaElevationUp, () => ChangeElevation(settings.SofaElevationStep));
        router.Register(ActionId.SofaElevationDown, () => ChangeElevation(-settings.SofaElevationStep));
    }

    private void SyncMenu()
        => _setMenuState(_player.CanToggleSofa, _player.IsSofaEnabled);

    private void Toggle()
    {
        // Accelerators can fire even when the corresponding native menu item is disabled.
        if (!_player.CanToggleSofa)
        {
            SyncMenu();
            // Translators: Spoken if the spatialization shortcut is used before a SOFA path is configured.
            _speech.SpeakText(Tr("Choose a SOFA file in Preferences before enabling headphone spatialization."));
            return;
        }
        var enabled = !_player.IsSofaEnabled;
        var applied = _player.SetSofaEnabled(enabled);
        // A checkable native item may have toggled itself even when applying the filter failed.
        SyncMenu();
        if (!applied)
        {
            AnnounceFailure();
            return;
        }
        _speech.SpeakText(enabled
            // Translators: Spoken after headphone spatialization is enabled for the current session.
            ? Tr("Headphone spatialization on")
            // Translators: Spoken after headphone spatialization is disabled for the current session.
            : Tr("Headphone spatialization off"));
    }

    private bool RequireEnabled()
    {
        if (_player.IsSofaEnabled)
            return true;
        // Translators: Spoken when a rotation or elevation shortcut is used while spatialization is off.
        _speech.SpeakText(Tr("Enable headphone spatialization before changing rotation or elevation."));
        return false;
    }

    private void ChangeRotation(double delta)
    {
        if (!RequireEnabled())
            return;
        if (!_player.ChangeSofaRotation(delta))
        {
            AnnounceFailure();
            return;
        }
        var value = _player.SofaRotation.ToString("0.###", CultureInfo.InvariantCulture);
        _speech.Speak(
            // Translators: Beginner announcement after changing horizontal rotation. {rotation} is the angle in degrees.
            TrFormat("Rotation {rotation} degrees", value),
            // Translators: Advanced announcement of a spatialization angle. {angle} is the value in degrees.
            TrFormat("{angle} degrees", value));
    }

    private void ChangeElevation(double delta)
    {
        if (!RequireEnabled())
            return;
        if (!_player.ChangeSofaElevation(delta))
        {
            AnnounceFailure();
            return;
        }
        var value = _player.SofaElevation.ToString("0.###", CultureInfo.InvariantCulture);
        _speech.Speak(
            // Translators: Beginner announcement after changing vertical elevation. {elevation} is the angle in degrees.
            TrFormat("Elevation {elevation} degrees", value),
            // Translators: Advanced announcement of a spatialization angle. {angle} is the value in degrees.
            TrFormat("{angle} degrees", value));
    }

    private void AnnounceFailure()
        => _speech.SpeakText(
            // Translators: Spoken if the SOFA file or audio filter cannot be loaded; the previous state is retained.
            Tr("Could not change headphone spatialization. Check the SOFA file in Preferences."));
}
