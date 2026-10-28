using LunaPlayer.Playlist;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Playlist;

[TestClass, TestCategory("Unit")]
public sealed class SavedDocumentTests
{
    [TestMethod]
    public void RoundTripKeepsDescriptionFoldersAndDuplicates()
    {
        using var workspace = new Workspace();
        var song = workspace.File("song.mp3");
        var document = new Document { Name = "ﬁ«∆„…", Description = "Description", Entries = [SavedEntry.FromPath(song), SavedEntry.FromPath(workspace.Root), SavedEntry.FromPath(song)] };
        var path = Path.Combine(workspace.Root, "list.json");
        DocumentStore.Write(document, path);
        var restored = DocumentStore.Read(path);
        Assert.AreEqual(document.Id, restored.Id);
        Assert.AreEqual(document.Description, restored.Description);
        Assert.AreEqual(3, restored.Entries.Count);
        Assert.AreEqual(EntryKind.Folder, restored.Entries[1].Kind);
        restored.Copy().Entries.Clear();
        Assert.AreEqual(3, restored.Entries.Count);
    }
    [TestMethod]
    public void FolderResolutionIsTopLevelAndRefreshesOnEachOpen()
    {
        using var workspace = new Workspace();
        workspace.File("one.mp3"); workspace.File("nested/two.mp3"); workspace.File("ignored.txt");
        var folder = SavedEntry.FromPath(workspace.Root);
        Assert.AreEqual(1, DocumentResolver.Resolve([folder]).Files.Count);
        workspace.File("three.flac");
        Assert.AreEqual(2, DocumentResolver.Resolve([folder]).Files.Count);
    }
    [TestMethod]
    public void StoreRejectsDuplicateNamesWithoutOverwritingTheOriginal()
    {
        using var workspace = new Workspace();
        var store = new DocumentStore(workspace.Root);
        var original = new Document { Name = "Songs" };
        store.Save(original);
        Assert.ThrowsExactly<InvalidDataException>(() => store.Save(new Document { Name = "songs" }));
        Assert.AreEqual(original.Id, store.List().Single().Id);
    }
    [TestMethod]
    public void MalformedDocumentIsRejected()
    {
        using var workspace = new Workspace();
        var path = workspace.File("invalid.json", "{}");
        Assert.ThrowsExactly<InvalidDataException>(() => DocumentStore.Read(path));
    }
    [TestMethod]
    public void MissingEntriesAreReportedAndRetainedInTheDocument()
    {
        using var workspace = new Workspace();
        var entries = new[] { SavedEntry.FromPath(Path.Combine(workspace.Root, "missing.mp3")) };
        var result = DocumentResolver.Resolve(entries);
        Assert.AreEqual(0, result.Files.Count);
        Assert.AreEqual(1, result.Unavailable.Count);
        Assert.AreEqual(1, entries.Length);
    }
}
