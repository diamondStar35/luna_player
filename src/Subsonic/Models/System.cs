using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record ScanStatus
{
    public bool Scanning { get; set; }
    public long Count { get; set; }
    public string? LastScan { get; set; }
    public long? FolderCount { get; set; }
    public long? ArtistCount { get; set; }
    public long? AlbumCount { get; set; }
    public long? SongCount { get; set; }
    public long? Errors { get; set; }
    public bool? FullScan { get; set; }
}

internal sealed record Extension
{
    public string Name { get; set; } = "";
    public int[] Versions { get; set; } = [];
}

internal sealed record User
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public bool ScrobblingEnabled { get; set; }
    // A missing role is unknown, not a refusal. Compatible servers omit roles they do not implement.
    public bool? AdminRole { get; set; }
    public bool? SettingsRole { get; set; }
    public bool? DownloadRole { get; set; }
    public bool? UploadRole { get; set; }
    public bool? PlaylistRole { get; set; }
    public bool? CoverArtRole { get; set; }
    public bool? CommentRole { get; set; }
    public bool? PodcastRole { get; set; }
    public bool? StreamRole { get; set; }
    public bool? JukeboxRole { get; set; }
    public bool? ShareRole { get; set; }
    public bool? VideoConversionRole { get; set; }
    public int? MaxBitRate { get; set; }
    [JsonPropertyName("folder")]
    [JsonConverter(typeof(Transport.IdentifiersConverter))]
    public string[] Folders { get; set; } = [];
    public string? AvatarLastChanged { get; set; }
}

internal sealed record License
{
    public bool Valid { get; set; }
    public string Email { get; set; } = "";
    public string? LicenseExpires { get; set; }
    public string? TrialExpires { get; set; }
}

internal sealed record Error
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public string HelpUrl { get; set; } = "";
}
