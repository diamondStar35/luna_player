using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.Models;

internal sealed record ScanStatus
{
    public bool Scanning { get; init; }
    public long Count { get; init; }
    public string? LastScan { get; init; }
    public long? FolderCount { get; init; }
    public long? ArtistCount { get; init; }
    public long? AlbumCount { get; init; }
    public long? SongCount { get; init; }
    public long? Errors { get; init; }
    public bool? FullScan { get; init; }
}

internal sealed record Extension
{
    public string Name { get; init; } = "";
    public int[] Versions { get; init; } = [];
}

internal sealed record User
{
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public bool ScrobblingEnabled { get; init; }
    public bool AdminRole { get; init; }
    public bool SettingsRole { get; init; }
    public bool DownloadRole { get; init; }
    public bool UploadRole { get; init; }
    public bool PlaylistRole { get; init; }
    public bool CoverArtRole { get; init; }
    public bool CommentRole { get; init; }
    public bool PodcastRole { get; init; }
    public bool StreamRole { get; init; }
    public bool JukeboxRole { get; init; }
    public bool ShareRole { get; init; }
    public bool VideoConversionRole { get; init; }
    public int? MaxBitRate { get; init; }
    [JsonPropertyName("folder")]
    [JsonConverter(typeof(Transport.IdentifiersConverter))]
    public string[] Folders { get; init; } = [];
    public string? AvatarLastChanged { get; init; }
}

internal sealed record License
{
    public bool Valid { get; init; }
    public string Email { get; init; } = "";
    public string? LicenseExpires { get; init; }
    public string? TrialExpires { get; init; }
}

internal sealed record Error
{
    public int Code { get; init; }
    public string Message { get; init; } = "";
    public string HelpUrl { get; init; } = "";
}
