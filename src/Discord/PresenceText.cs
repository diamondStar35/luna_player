using LunaPlayer.Configuration;

namespace LunaPlayer.Discord;

internal sealed record PresenceText(
    string Idle, string Generic, string Untitled,
    string Music, string Video, string YouTube, string Radio, string Spotify, string Audiobook, string Watch,
    string WatchYouTube, string ListenYouTube, string OpenSpotify, string Download,
    string ArtistTemplate, string PausedTemplate)
{
    // Capture translations on the UI thread; formatting and tests do not need wxWidgets.
    internal static PresenceText Localized() => new(
        // Translators: Discord activity when no media is loaded.
        Tr("Idle"),
        // Translators: Discord title line when media details are hidden.
        Tr("Media playback"),
        // Translators: Discord fallback title when the media has no safe title.
        Tr("Untitled media"),
        // Translators: Activity name for local audio. Discord adds "Listening to" before this phrase.
        Tr("music on Luna"),
        // Translators: Activity name for local video. Discord adds "Watching" before this phrase.
        Tr("video on Luna"),
        // Translators: Activity name for YouTube. Discord adds "Listening to" or "Watching" before this phrase.
        Tr("YouTube on Luna"),
        // Translators: Activity name for radio. Discord adds "Listening to" before this phrase.
        Tr("radio on Luna"),
        // Translators: Activity name for Spotify. Discord adds "Listening to" before this phrase.
        Tr("Spotify on Luna"),
        // Translators: Audiobook activity name. Discord adds "Listening to" before this phrase.
        Tr("an audiobook on Luna"),
        // Translators: Discord button opening a public radio stream in the browser.
        Tr("Watch online"),
        // Translators: Discord button opening the current video on YouTube.
        Tr("Watch on YouTube"),
        // Translators: Discord button opening the current audio on YouTube.
        Tr("Listen on YouTube"),
        // Translators: Discord button opening the current track on Spotify.
        Tr("Open in Spotify"),
        // Translators: Discord button opening Luna Player's releases page.
        Tr("Download Luna Player"),
        // Translators: Artist credit in a Watching card's body, separate from its artwork label. {artist} is the performer or channel.
        Tr("By {artist}"),
        // Translators: Discord app heading while paused. {app} is the application name, Luna Player.
        Tr("[Paused]: {app}"));

    internal string By(string artist) => Text.FormatTranslatedValue(ArtistTemplate, artist);
    internal string PausedApplication(string app) => Text.FormatTranslatedValue(PausedTemplate, app);
}
