using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Media;

namespace LunaPlayer.Favorites;

/// <summary>The links the user has saved to come back to.</summary>
///
/// <remarks>
/// A port of the Python player's <c>favorites</c> package, keeping its file shape so a favourites file
/// written by that player is read as it stands.
///
/// Nothing opens these yet. Three of the four kinds are YouTube addresses, and the player has no YouTube
/// support to hand them to; the fourth is a plain network stream, which it could open today but which has
/// nowhere to be chosen from until there is a window listing what is saved. What is here is the store and
/// the rules about what may go in it, so that the part which is missing is the part that belongs with
/// YouTube rather than the part underneath it.
/// </remarks>
internal sealed class FavoriteStore
{
    private readonly string _path;

    internal FavoriteStore(string path) => _path = path;

    internal string FilePath => _path;

    /// <summary>Why the last read or write failed, or an empty string when it did not.</summary>
    internal string LastError { get; private set; } = string.Empty;

    /// <summary>Everything saved, oldest first.</summary>
    /// <remarks>
    /// An entry that does not survive <see cref="IsUsable"/> is passed over rather than repaired or thrown
    /// away: a file that has been edited by hand, or written by a newer version, still gives up the entries
    /// that make sense, and nothing is deleted behind the user's back.
    /// </remarks>
    internal IReadOnlyList<Favorite> ListAll()
    {
        var document = Load();
        return document.Items
            .Where(IsUsable)
            .OrderBy(favorite => favorite.Created)
            .ThenBy(favorite => favorite.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal Favorite? Get(string id)
    {
        var target = (id ?? string.Empty).Trim();
        return target.Length == 0
            ? null
            : ListAll().FirstOrDefault(favorite => favorite.Id == target);
    }

    /// <summary>Saves a new link. Returns null, with <see cref="LastError"/> set, if the link is not one this
    /// kind may hold or if the file could not be written.</summary>
    internal Favorite? Add(string name, FavoriteKind kind, string link)
    {
        if (!Check(name, kind, link, out var favorite))
            return null;
        favorite.Id = Guid.NewGuid().ToString("N");
        favorite.Created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var document = Load();
        document.Items.Add(favorite);
        return Save(document) ? favorite : null;
    }

    /// <summary>Changes what a saved link is called, what kind it is, and where it points.</summary>
    internal bool Update(string id, string name, FavoriteKind kind, string link)
    {
        var target = (id ?? string.Empty).Trim();
        if (target.Length == 0 || !Check(name, kind, link, out var replacement))
            return false;
        var document = Load();
        var existing = document.Items.FirstOrDefault(favorite => favorite.Id.Trim() == target);
        if (existing is null)
            return Report(false, NotFound);
        existing.Name = replacement.Name;
        existing.Kind = replacement.Kind;
        existing.Link = replacement.Link;
        return Save(document);
    }

    internal bool Delete(string id)
    {
        var target = (id ?? string.Empty).Trim();
        if (target.Length == 0)
            return Report(false, NotFound);
        var document = Load();
        if (document.Items.RemoveAll(favorite => favorite.Id.Trim() == target) == 0)
            return Report(false, NotFound);
        return Save(document);
    }

    /// <summary>Whether a link may be saved under a given kind, and why not when it may not.</summary>
    ///
    /// <remarks>
    /// The kind is the user's choice rather than something read off the link, because one address can be
    /// several things: a YouTube address carrying both a video id and a list id can be opened as either, and
    /// which one the user wanted is not recoverable from the address. So the link is not classified here,
    /// only checked against what the chosen kind requires.
    ///
    /// Nothing here goes near the network. Whether a video still exists is a question for the point at which
    /// it is opened; this only rules out what cannot work, so a mistyped address is refused while the user is
    /// still looking at it.
    /// </remarks>
    /// <param name="error">Empty when the link may be saved; otherwise a message to show the user.</param>
    internal static bool Validate(string? name, FavoriteKind kind, string? link, out string error)
    {
        if ((name ?? string.Empty).Trim().Length == 0)
        {
            // Translators: Shown when the user saves a favourite without typing a name for it.
            error = Tr("Name is required.");
            return false;
        }
        var address = (link ?? string.Empty).Trim();
        if (address.Length == 0)
        {
            // Translators: Shown when the user saves a favourite without giving a link.
            error = Tr("Link is required.");
            return false;
        }

        var info = LinkValidator.Parse(address);
        if (kind == FavoriteKind.Stream)
        {
            if (!info.IsHttp)
            {
                // Translators: Shown when a favourite saved as a network stream is not a web address.
                error = Tr("Generic stream link must start with http or https.");
                return false;
            }
            error = string.Empty;
            return true;
        }

        if (!info.IsHttp)
        {
            // Translators: Shown when a favourite is saved with something that is not a web address.
            error = Tr("The link must start with http or https.");
            return false;
        }
        if (!info.IsYouTube)
        {
            // Translators: Shown when a favourite saved as a video, playlist or combined link is not a
            // YouTube address.
            error = Tr("The link must be a valid YouTube link for this type.");
            return false;
        }
        switch (kind)
        {
            case FavoriteKind.Video when !info.HasVideo:
                // Translators: Shown when a favourite saved as a video does not name one.
                error = Tr("Video favorites require a YouTube video link.");
                return false;
            case FavoriteKind.Playlist when !info.HasPlaylist:
                // Translators: Shown when a favourite saved as a playlist does not name one.
                error = Tr("Playlist favorites require a YouTube playlist link.");
                return false;
            case FavoriteKind.Combined when !(info.HasVideo && info.HasPlaylist):
                // Translators: Shown when a favourite saved as a combined link does not carry both a video
                // and a playlist.
                error = Tr("Combined link favorites require a link containing both video and playlist.");
                return false;
        }
        error = string.Empty;
        return true;
    }

    /// <summary>What a favourite of this kind is called, for a list or a message.</summary>
    internal static string Describe(FavoriteKind kind) => kind switch
    {
        // Translators: The kind of a saved favourite: a single YouTube video.
        FavoriteKind.Video => Tr("Video"),
        // Translators: The kind of a saved favourite: a YouTube playlist.
        FavoriteKind.Playlist => Tr("Playlist"),
        // Translators: The kind of a saved favourite: a YouTube link naming both a video and a playlist.
        FavoriteKind.Combined => Tr("Combined link"),
        // Translators: The kind of a saved favourite: a web address played as a stream.
        _ => Tr("Generic stream"),
    };

    /// <summary>Whether an entry read from the file is complete enough to show.</summary>
    private static bool IsUsable(Favorite favorite)
        => favorite.Id.Trim().Length > 0
            && favorite.Name.Trim().Length > 0
            && favorite.Link.Trim().Length > 0;

    /// <summary>Validates the fields and returns them trimmed, ready to store.</summary>
    private bool Check(string name, FavoriteKind kind, string link, out Favorite favorite)
    {
        favorite = new Favorite { Name = (name ?? string.Empty).Trim(), Kind = kind, Link = (link ?? string.Empty).Trim() };
        return Validate(name, kind, link, out var error) ? Report(true, error) : Report(false, error);
    }

    /// <summary>The message shown when an id names nothing. Not a validation failure the user caused: it
    /// means the file changed under the window listing it.</summary>
    private static string NotFound =>
        // Translators: Shown when the user edits or removes a favourite that is no longer saved.
        Tr("That favorite is no longer saved.");

    private bool Report(bool success, string error)
    {
        LastError = success ? string.Empty : error;
        return success;
    }

    private FavoriteDocument Load()
    {
        if (!File.Exists(_path))
            return new FavoriteDocument();
        try
        {
            using var stream = File.OpenRead(_path);
            var document = JsonSerializer.Deserialize(stream, FavoriteJsonContext.Default.FavoriteDocument);
            if (document is null)
                return new FavoriteDocument();
            document.Items ??= [];
            return document;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A file that cannot be read is reported as empty rather than as an error, so that saving a new
            // favourite still works. Save writes the whole document, which repairs it.
            return new FavoriteDocument();
        }
    }

    /// <summary>Writes the file whole, through a temporary copy, so that a failure part way leaves the
    /// previous contents rather than a truncated file.</summary>
    private bool Save(FavoriteDocument document)
    {
        try
        {
            Paths.EnsureDirectoryFor(_path);
            var temporaryPath = Paths.TemporaryFor(_path);
            using (var stream = File.Create(temporaryPath))
                JsonSerializer.Serialize(stream, document, FavoriteJsonContext.Default.FavoriteDocument);
            File.Move(temporaryPath, _path, overwrite: true);
            return Report(true, string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Report(false, exception.Message);
        }
    }
}
