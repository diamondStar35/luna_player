using LunaPlayer.Favorites;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The window for saving a link or changing one already saved.</summary>
/// <remarks>
/// What was typed is checked when OK is pressed and the window is kept open when the check fails, so a
/// mistyped address costs a correction rather than the whole entry. The Python player throws the window
/// away and reopens an empty one, which loses everything the user wrote.
///
/// The rules themselves are <see cref="FavoriteStore.Validate"/>'s, so the window and the store cannot
/// come to disagree about what may be saved.
/// </remarks>
internal sealed class FavoriteEditDialog : IDisposable
{
    /// <summary>The kinds in the order they are listed, which is the order they are declared in.</summary>
    private static readonly FavoriteKind[] Kinds =
        [FavoriteKind.Video, FavoriteKind.Playlist, FavoriteKind.Combined, FavoriteKind.Stream];

    private readonly Dialog _dialog;
    private readonly TextCtrl _name;
    private readonly Choice _kind;
    private readonly TextCtrl _link;

    internal FavoriteEditDialog(Window parent, string caption, FavoriteDraft value)
    {
        _dialog = new Dialog(parent, title: caption, style: DialogStyle.Default | DialogStyle.ResizeBorder);
        // Translators: Label of the box holding what a saved link is called.
        var nameLabel = new StaticText(_dialog, label: Tr("Name"));
        _name = new TextCtrl(_dialog, value: value.Name);
        // Translators: Label of the list saying what kind of thing a saved link points at.
        var kindLabel = new StaticText(_dialog, label: Tr("Type"));
        _kind = new Choice(_dialog);
        foreach (var kind in Kinds)
            _kind.Add(FavoriteStore.Describe(kind));
        _kind.SelectedIndex = Math.Max(0, Array.IndexOf(Kinds, value.Kind));
        // Translators: Label of the box holding the address a saved link points at.
        var linkLabel = new StaticText(_dialog, label: Tr("Link"));
        _link = new TextCtrl(_dialog, value: value.Link);

        // A two-column form: label beside its box rather than above it. Three short labels stacked above
        // three full-width boxes wastes the height the window has and reads as six things rather than three.
        var form = new FlexGridSizer(0, 2, 8, 8);
        form.AddGrowableColumn(1, 1);
        AddField(form, nameLabel, _name);
        AddField(form, kindLabel, _kind);
        AddField(form, linkLabel, _link);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(form, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.Ok | ButtonSizerFlags.Cancel);
        if (buttons is not null)
            sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(560, 220);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.ButtonClicked, OnAccept, StandardId.Ok);
        _name.Focus();
    }

    internal FavoriteDraft? Show()
        => _dialog.ShowModal() == StandardId.Ok ? Current() : null;

    public void Dispose() => _dialog.Dispose();

    private FavoriteDraft Current()
        => new(_name.Value.Trim(), Kinds[Math.Max(0, _kind.SelectedIndex)], _link.Value.Trim());

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        var draft = Current();
        if (!FavoriteStore.Validate(draft.Name, draft.Kind, draft.Link, out var error))
        {
            Wx.MessageBox(error,
                // Translators: Title of the message shown when a saved link cannot be saved as it was typed.
                Tr("Favorite videos"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>One row of the form: the label centred against its box, the box taking the width.</summary>
    private static void AddField(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }
}
