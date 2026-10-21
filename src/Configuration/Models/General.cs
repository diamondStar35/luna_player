using System.Text.Json.Serialization;

namespace LunaPlayer.Configuration.Models;

internal enum SpeechVerbosity { Beginner, Advanced }
internal enum OpenFilesMode { FileOnly, MainFolder, MainAndSubfolders }
/// <summary>Which interface appearance the player asks Windows for. <see cref="SystemDefault"/> follows the
/// Windows setting; the other two force light or dark regardless. A high-contrast scheme always wins over
/// this choice. Applied once at startup, so a change takes effect after the player restarts.</summary>
internal enum AppTheme { SystemDefault, Light, Dark }

internal sealed class GeneralSettings
{
    public string Language { get; set; } = Localization.SystemLanguage;
    public bool RememberLastPosition { get; set; }
    public bool SpeakFileOnNavigation { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool SaveOnClose { get; set; } = true;
    public bool OpenFilesInExistingWindow { get; set; } = true;

    /// <summary>Whether the player stays out of the system's media controls entirely: the Windows overlay
    /// for the volume keys and lock screen, and mpv's own media-key and transport-control integration.</summary>
    /// <remarks>
    /// The equivalent of mpv's <c>--no-media-controls</c>, offered because that integration can misbehave with
    /// some headphones - a play/pause button that pauses twice, or reaches the wrong application. Off by
    /// default, so the controls are there for everyone who is not troubled by them.
    /// </remarks>
    public bool DisableMediaControls { get; set; }

    /// <summary>Whether the window title follows the playing track - the title bar showing what is open, and
    /// paused when it is - rather than staying the plain program name.</summary>
    /// <remarks>
    /// Off by default: a screen reader announces the foreground window whenever its title changes, so a title
    /// that tracks playback speaks over the player on every track change and pause. Some users want it anyway,
    /// so it is offered as a choice rather than left out.
    /// </remarks>
    public bool SpeakWindowTitle { get; set; }

    /// <summary>Whether a subtitle line being read aloud is cut off when the next line appears, so the speech
    /// keeps pace with the picture. Off reads each line to the end before the next, so none is lost.</summary>
    /// <remarks>Off by default: a cut-off line is a line the listener never heard in full, and subtitle cues
    /// often sit close enough together to clip one another. Turning it on trades completeness for staying in
    /// step with the picture, which suits dense dialogue.</remarks>
    public bool SubtitleInterrupt { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<SpeechVerbosity>))]
    public SpeechVerbosity Verbosity { get; set; } = SpeechVerbosity.Beginner;
    [JsonConverter(typeof(JsonStringEnumConverter<OpenFilesMode>))]
    public OpenFilesMode OpenFilesMode { get; set; } = OpenFilesMode.FileOnly;
    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; set; } = AppTheme.SystemDefault;
    public string LastDirectory { get; set; } = string.Empty;
    internal GeneralSettings Copy() => (GeneralSettings)MemberwiseClone();
    internal void Apply(GeneralSettings source)
    {
        Language = source.Language;
        RememberLastPosition = source.RememberLastPosition;
        SpeakFileOnNavigation = source.SpeakFileOnNavigation;
        CheckUpdatesOnStartup = source.CheckUpdatesOnStartup;
        SaveOnClose = source.SaveOnClose;
        OpenFilesInExistingWindow = source.OpenFilesInExistingWindow;
        DisableMediaControls = source.DisableMediaControls;
        SpeakWindowTitle = source.SpeakWindowTitle;
        SubtitleInterrupt = source.SubtitleInterrupt;
        Verbosity = source.Verbosity;
        OpenFilesMode = source.OpenFilesMode;
        Theme = source.Theme;
        LastDirectory = source.LastDirectory;
    }
}
