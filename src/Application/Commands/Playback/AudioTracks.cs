using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

internal sealed class AudioTracks
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;

    internal AudioTracks(ActionRouter router, IMainView view, MediaPlayer player, ISpeechOutput speech)
    {
        _view = view;
        _player = player;
        _speech = speech;
        router.Register(ActionId.OpenAudioTracks, ChooseTrack);
        router.Register(ActionId.NextAudioTrack, () => Step(1));
        router.Register(ActionId.PreviousAudioTrack, () => Step(-1));
    }

    private void ChooseTrack()
    {
        var tracks = _player.GetAudioTracks();
        if (tracks.Count == 0)
        {
            _speech.Speak(
                // Translators: Spoken when the user asks to choose an audio track but the file has none.
                Tr("No audio tracks found."),
                // Translators: The short wording spoken when the file has no audio tracks to choose from.
                Tr("No tracks."));
            return;
        }
        var labels = tracks.Select(track => Describe(track, beginner: true)).ToArray();
        var selected = Math.Max(0, tracks.ToList().FindIndex(track => track.Selected));
        var index = _view.ChooseAudioTrack(labels, selected);
        if (index.HasValue)
            Apply(tracks, index.Value);
    }

    // The Shift+PageUp/PageDown accelerators fire even while the menu items are disabled, so a file with one
    // track or none is left alone here. The ends do not wrap: a press past the last or first track stays put.
    private void Step(int direction)
    {
        var tracks = _player.GetAudioTracks();
        if (tracks.Count < 2)
            return;
        var current = tracks.ToList().FindIndex(track => track.Selected);
        if (current < 0)
            current = 0;
        var next = current + direction;
        if (next < 0 || next >= tracks.Count)
            return;
        Apply(tracks, next);
    }

    private void Apply(IReadOnlyList<AudioTrack> tracks, int index)
    {
        var track = tracks[index];
        if (track.Selected || _player.SetAudioTrack(track.Id))
            Announce(track, index, tracks.Count);
        else
            _speech.Speak(
                // Translators: Spoken when the player could not switch to the audio track the user picked.
                Tr("Could not change audio track."),
                // Translators: The short wording spoken when the audio track could not be changed.
                Tr("Track failed."));
    }

    private void Announce(AudioTrack track, int index, int count)
    {
        // Translators: The place of an audio track in the file's list, as in "2 of 2". {current} is the
        // track's number and {total} is how many there are. Spoken after the track only in beginner verbosity.
        var position = TrFormat("{current} of {total}", index + 1, count);
        _speech.Speak($"{Describe(track, beginner: true)}, {position}", Describe(track, beginner: false));
    }

    // The title, then the channels and language, separated by commas. Beginner and advanced read alike
    // except for the stand-in used when the track has no title of its own.
    private static string Describe(AudioTrack track, bool beginner)
    {
        var parts = new List<string>(3)
        {
            track.Title ?? (beginner
                // Translators: Stand-in name for an audio track that has no title of its own, in beginner verbosity.
                ? Tr("Audio Track")
                // Translators: The short stand-in name for an audio track with no title, in advanced verbosity.
                : Tr("Track")),
        };
        if (FormatChannels(track.Channels) is { } channels)
            parts.Add(channels);
        if (Localization.LanguageName(track.Language) is { Length: > 0 } language)
            parts.Add(language);
        return string.Join(", ", parts);
    }

    private static string? FormatChannels(int channels) => channels switch
    {
        <= 0 => null,
        // Translators: Name of a single-channel (mono) audio track's channel layout.
        1 => Tr("mono"),
        // Translators: Name of a two-channel (stereo) audio track's channel layout.
        2 => Tr("stereo"),
        // Translators: Name of a six-channel surround audio track's layout. Usually left as "5.1".
        6 => Tr("5.1"),
        // Translators: Name of an eight-channel surround audio track's layout. Usually left as "7.1".
        8 => Tr("7.1"),
        // Translators: Channel layout of an audio track with an unusual channel count. {count} is that number.
        _ => TrFormat("{count} channels", channels),
    };
}
