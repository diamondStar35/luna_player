using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.UI.Subsonic;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class CommandTests
{
    private static CommandContext Context(Row[] rows, User? user = null)
        => new(rows, new(Category.Songs), user, null, "owner", false, false, false);

    [TestMethod]
    public void EmptyMenusRetainServerCommandsWithoutSelectionActions()
    {
        var context = Context([]);
        Assert.IsTrue(CommandPolicy.Allows(Command.CreatePlaylist, context));
        Assert.IsTrue(CommandPolicy.Allows(Command.Info, context));
        foreach (var command in new[] { Command.Rate, Command.Play, Command.Download, Command.Favorite, Command.Remove, Command.SendPlaylist })
            Assert.IsFalse(CommandPolicy.Allows(command, context), command.ToString());
    }

    [TestMethod]
    public void MissingPermissionsAreUnknownAndExplicitRefusalsAreHonored()
    {
        Row[] rows = [new(RowKind.Song, "s", "Song")];
        Assert.IsTrue(CommandPolicy.Allows(Command.CreatePlaylist, Context(rows, new())));
        Assert.IsTrue(CommandPolicy.Allows(Command.Download, Context(rows, new())));
        Assert.IsFalse(CommandPolicy.Allows(Command.Download, Context(rows, new() { DownloadRole = false })));
        Assert.IsFalse(CommandPolicy.Allows(Command.CreatePlaylist, Context(rows, new() { PlaylistRole = false })));
        Assert.IsTrue(CommandPolicy.Allows(Command.CreatePlaylist, Context(rows, new() { PlaylistRole = false, AdminRole = true })));
        Assert.IsFalse(CommandPolicy.Allows(Command.Play, Context(rows, new() { StreamRole = false })));
    }

    [TestMethod]
    public void RatingsAndPlaylistRemovalApplyOnlyToEligibleSongOccurrences()
    {
        Assert.IsTrue(CommandPolicy.Allows(Command.Rate, Context([new(RowKind.Song, "s", "Song"), new(RowKind.Bookmark, "b", "Bookmark")])));
        Assert.IsFalse(CommandPolicy.Allows(Command.Rate, Context([new(RowKind.Artist, "a", "Artist")])));
        Assert.IsFalse(CommandPolicy.Allows(Command.Play, Context([new(RowKind.Podcast, "p", "Episode", Available: false)])));
        var context = Context([new(RowKind.Song, "s", "Song", PlaylistIndex: 3)]) with { Location = new(Category.Playlists, RowKind.Playlist, "p") };
        Assert.IsTrue(CommandPolicy.Allows(Command.RemovePlaylistItems, context));
        Assert.IsFalse(CommandPolicy.Allows(Command.RemovePlaylistItems, context with { Location = new(Category.Songs) }));
    }

    [TestMethod]
    public void EditingAnotherUsersPlaylistRequiresOwnershipAndSonicNeedsItsExtension()
    {
        var context = Context([new(RowKind.Playlist, "p", "Other", Owner: "someone")]);
        Assert.IsFalse(CommandPolicy.Allows(Command.EditPlaylist, context));
        Assert.IsTrue(CommandPolicy.Allows(Command.EditPlaylist, context with { User = new() { AdminRole = true } }));
        context = Context([new(RowKind.Song, "s", "Song")]);
        Assert.IsFalse(CommandPolicy.Allows(Command.Sonic, context));
        Assert.IsTrue(CommandPolicy.Allows(Command.Sonic, context with { Capabilities = new(new Response(), [new() { Name = "sonicSimilarity", Versions = [1] }], true) }));
    }
}
