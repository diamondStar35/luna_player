using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal void PlayMediaMix(IReadOnlyList<(string Address, MediaTags Tags)> songs)
    {
        if (songs.Count == 0) return;
        var requested = songs.DistinctBy(song => song.Address, StringComparer.Ordinal).ToArray();
        _mediaForward = true; LeaveSession(); LeaveDisc(); SavePosition();
        var previous = CurrentItemId; var position = SourceElapsed ?? 0;
        var loaded = _loaded;
        foreach (var song in requested) { _catalogTags[song.Address] = song.Tags; _playlist.SetTitle(song.Address, song.Tags.Title); }
        var batch = _playlist.QueueMix(requested.Select(song => song.Address).ToArray(), position);
        var target = _playlist.Items.First(item => item.SourceId == batch[0].SourceId);
        _playlist.GoToItem(target.Id);
        if (loaded && target.Id == previous)
        {
            if (IsPaused) Play();
            PrepareNext(); CurrentChanged?.Invoke(); StateChanged?.Invoke();
        }
        else LoadCurrent(0, paused: false, fadeTransition: true);
    }
}
