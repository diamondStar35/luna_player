using System.Diagnostics.CodeAnalysis;
using LunaPlayer.Accessibility;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>The conditions an action checks before it can do anything, and what the player says when one of
/// them is not met.</summary>
///
/// <remarks>
/// Nearly every action needs a file to be loaded, and several need one that is on this computer or a position
/// within it that mpv can report. Each handler used to make that check and word the refusal itself, which is
/// how two of them came to phrase the same refusal differently. Asking here keeps one wording for one
/// situation, in one place a translator has to read.
///
/// A Require method that returns false has already spoken, so a caller does nothing but return.
/// </remarks>
internal sealed class MediaGuard
{
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;

    internal MediaGuard(MediaPlayer player, ISpeechOutput speech)
    {
        _player = player;
        _speech = speech;
    }

    /// <summary>Whether anything is loaded, handing back what is playing.</summary>
    internal bool RequireFile([NotNullWhen(true)] out string? path)
    {
        path = _player.CurrentPath;
        if (!string.IsNullOrEmpty(path))
            return true;
        path = null;
        ReportNoFile();
        return false;
    }

    /// <summary>Whether the playlist holds anything at all, for an action that works on the whole list rather
    /// than on the entry that is playing.</summary>
    internal bool RequireAnyFile()
    {
        if (_player.Count > 0)
            return true;
        ReportNoFile();
        return false;
    }

    /// <summary>Whether what is playing is a file on this computer rather than a network stream.</summary>
    /// <param name="unavailable">What to say when it is a stream: the action names itself, because a sentence
    /// assembled from a translated verb and a translated remainder does not read as one in every language.
    /// </param>
    internal bool RequireLocalFile(string unavailable, [NotNullWhen(true)] out string? path)
    {
        if (!RequireFile(out path))
            return false;
        if (File.Exists(path))
            return true;
        path = null;
        _speech.Speak(unavailable,
            // Translators: The short wording spoken when a command that needs a file on this computer is used while a stream is playing.
            Tr("Not available for streams."));
        return false;
    }

    /// <summary>Whether mpv can say how far through the file playing has reached. Zero is a real answer: it
    /// means the very beginning.</summary>
    internal bool RequireElapsed(out double elapsed)
    {
        if (_player.Elapsed is double value)
        {
            elapsed = value;
            return true;
        }
        elapsed = 0;
        ReportTimeUnavailable();
        return false;
    }

    /// <summary>Whether mpv can say how long the file is. A length of zero is not an answer: nothing can be
    /// seeked to or worked out as a proportion of it.</summary>
    internal bool RequireDuration(out double duration)
    {
        if (_player.Duration is double value && value > 0)
        {
            duration = value;
            return true;
        }
        duration = 0;
        ReportTimeUnavailable();
        return false;
    }

    /// <summary>Says that there is nothing loaded. For an action that finds this out from the result of what
    /// it tried rather than by asking first.</summary>
    internal void ReportNoFile() => _speech.Speak(
        // Translators: Spoken when a command needs a file to be playing but none is loaded.
        Tr("No file loaded."),
        // Translators: The short wording spoken when a command needs a file to be playing but none is loaded.
        Tr("No file."));

    /// <summary>Says that a time the user asked for is not known.</summary>
    internal void ReportTimeUnavailable() => _speech.Speak(
        // Translators: Spoken when the player cannot tell how long the file is, so it has no time to report or move to.
        Tr("Time not available"),
        // Translators: The short wording spoken when a time the user asked for is not known.
        Tr("Unavailable"));
}
