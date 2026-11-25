using LunaPlayer.Subsonic.Browsing;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Servers;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed class BrowsingPage
{
    private readonly CheckListBox _libraries, _sections;
    private readonly CheckBox _restrict, _refresh;
    private readonly Category[] _categories = Enum.GetValues<Category>();
    private string[] _libraryIds;
    private readonly Button _load;
    private readonly Form _form;
    internal Func<CancellationToken, Task<MusicFolder[]>>? LoadAsync { get; set; }

    internal BrowsingPage(Window parent, BoxSizer body, Profile profile, Form form)
    {
        _form = form; _libraryIds = profile.LibraryIds;
        _restrict = new CheckBox(parent, label: Words.RestrictLibraries) { Checked = _libraryIds.Length > 0 };
        body.Add(_restrict, flags: SizerFlags.All, border: 5);
        _libraries = Form.Field(parent, body, Words.SelectLibraries, () => new CheckListBox(parent));
        _libraries.MinSize = new Size(300, 110);
        foreach (var id in _libraryIds) { _libraries.Add(id); _libraries.SetChecked(_libraries.Count - 1); }
        _libraries.Enabled = _restrict.Checked;
        _restrict.Toggled += (_, _) => _libraries.Enabled = _restrict.Checked;
        _load = new Button(parent, label: Words.LoadLibraries); body.Add(_load, flags: SizerFlags.All, border: 5);
        _load.Click += (_, _) => Load();
        _sections = Form.Field(parent, body, Words.VisibleSections, () => new CheckListBox(parent));
        _sections.MinSize = new Size(300, 220);
        foreach (var category in _categories)
        { _sections.Add(Sections.Name(category)); _sections.SetChecked(_sections.Count - 1, !profile.HiddenSections.Contains(category.ToString(), StringComparer.Ordinal)); }
        _refresh = new CheckBox(parent, label: Words.RefreshMetadata) { Checked = profile.RefreshMetadataOnStartup };
        body.Add(_refresh, flags: SizerFlags.All, border: 5);
        form.Help(_restrict, Words.HelpRestrictLibraries); form.Help(_libraries, Words.HelpSelectLibraries);
        form.Help(_load, Words.HelpLoadLibraries); form.Help(_sections, Words.HelpVisibleSections);
        form.Help(_refresh, Words.HelpStartupMetadata);
    }

    internal Profile Apply(Profile profile)
    {
        var chosen = _libraries.GetCheckedItems().Select(index => _libraryIds[index]).ToArray();
        if (_restrict.Checked && chosen.Length == 0) throw new ArgumentException(Words.NeedLibraries);
        if (_sections.GetCheckedItems().Length == 0) throw new ArgumentException(Words.NeedSections);
        return profile with { LibraryIds = _restrict.Checked ? chosen : [], RefreshMetadataOnStartup = _refresh.Checked,
            HiddenSections = _categories.Where((_, index) => !_sections.IsChecked(index)).Select(category => category.ToString()).ToArray() };
    }

    internal string Signature => string.Join(",", _libraries.GetCheckedItems().Select(index => _libraryIds[index]).Order(StringComparer.Ordinal)) + "|" + _restrict.Checked
        + "|" + string.Join(",", _sections.GetCheckedItems()) + "|" + _refresh.Checked;

    internal void Libraries(MusicFolder[] libraries)
    {
        var chosen = _libraries.GetCheckedItems().Select(index => _libraryIds[index]).ToHashSet(StringComparer.Ordinal);
        _libraryIds = libraries.Select(library => library.Id).ToArray(); _libraries.Clear();
        foreach (var library in libraries) { _libraries.Add(library.Name); _libraries.SetChecked(_libraries.Count - 1, chosen.Contains(library.Id)); }
    }

    private async void Load()
    {
        if (LoadAsync is null || _form.Token.IsCancellationRequested || !_load.Enabled) return;
        _load.Enabled = false;
        try
        {
            var libraries = await LoadAsync(_form.Token).ConfigureAwait(false);
            _form.Post(() => { Libraries(libraries); _form.Success(Words.LibrariesLoaded(libraries.Length)); });
        }
        catch (OperationCanceledException) when (_form.Token.IsCancellationRequested) { }
        catch (Exception error) { _form.Post(() => _form.Error(error)); }
        finally { _form.Post(() => _load.Enabled = true); }
    }
}
