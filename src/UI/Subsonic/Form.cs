using LunaPlayer.UI.Services;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

/// <summary>An owned editor disables its owner only; saving can continue without a nested modal loop.</summary>
internal sealed class Form : IDisposable
{
    internal Dialog Window { get; }
    internal Panel Panel { get; }
    internal BoxSizer Body { get; }
    private readonly OwnedDialog _owner;
    private readonly Button _save;
    private readonly Button? _cancel;
    private readonly Lifetime _lifetime = new();
    private readonly CancellationToken _token;
    private bool _closed, _disposed;
    private readonly bool _closeOnly;
    private readonly Dictionary<Window, string> _help = [];
    internal CancellationToken Token => _token;
    internal Form(Window? owner, string title, string? accept = null, bool closeOnly = false)
    {
        _token = _lifetime.Token;
        _closeOnly = closeOnly;
        Window = new Dialog(owner, title: title, size: new Size(620, 540), style: DialogStyle.Default | DialogStyle.ResizeBorder
            | (owner is null ? DialogStyle.NoParent : 0));
        Window.MinSize = new Size(520, 360);
        Panel = new Panel(Window); Body = new BoxSizer(Orientation.Vertical); Panel.SetSizer(Body);
        var root = new BoxSizer(Orientation.Vertical); root.Add(Panel, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 10);
        var buttons = new BoxSizer(Orientation.Horizontal);
        _save = new Button(Window, closeOnly ? StandardId.Cancel : StandardId.Ok, closeOnly ? Words.Close : accept ?? Words.Save); _save.SetDefault();
        buttons.AddStretchSpacer(); buttons.Add(_save, flags: SizerFlags.BorderRight, border: 8);
        if (!closeOnly)
        {
            _cancel = new Button(Window, StandardId.Cancel, Words.Cancel); buttons.Add(_cancel);
        }
        root.Add(buttons, flags: SizerFlags.Expand | SizerFlags.All, border: 10); Window.SetSizer(root);
        Window.SetEscapeId(StandardId.Cancel);
        _owner = new OwnedDialog(Window, owner) { CanCancel = () => HasChanges?.Invoke() != true || Confirm(Words.Unsaved) };
        _owner.Closed += accepted => { if (_disposed) return; _closed = true; _lifetime.Dispose(); Closed?.Invoke(accepted); };
        if (!closeOnly) _save.Click += (_, _) => Save();
        Window.Bind(WxEvents.CharHook, (_, args) =>
        {
            if (args.Code == Key.F1)
            {
                var help = GetContextHelp(WxSharp.Window.FindFocus());
                if (SpeakHelp is { } speak) speak(help); else Info(help);
            }
            else if (args.Control && args.Code is (Key)'S' or (Key)'s') Save();
            else args.Skip();
        });
    }
    private async void Save()
    {
        if (_closed || _closeOnly || !_save.Enabled) return;
        _save.Enabled = false;
        try
        {
            var accepted = SaveAsync is null || await SaveAsync(Token).ConfigureAwait(false);
            Post(() => { if (accepted) _owner.Complete(true); });
        }
        catch (OperationCanceledException) when (_token.IsCancellationRequested) { }
        catch (Exception error) { Post(() => Error(error)); }
        finally { Post(() => _save.Enabled = true); }
    }
    internal Func<bool>? HasChanges { get; set; }
    internal string HelpText { get; set; } = Words.HelpText;
    internal Action<string>? SpeakHelp { get; set; }
    internal Func<string>? PageHelp { get; set; }
    internal void Help(Window control, string text) => _help[control] = text;
    internal void HelpFooter(string save, string cancel)
    { Help(_save, save); if (_cancel is not null) Help(_cancel, cancel); }
    internal string GetContextHelp(Window? focused)
    {
        for (var control = focused; control is not null; control = control.Parent)
        {
            if (_help.TryGetValue(control, out var help)) return help;
            if (ReferenceEquals(control, Window)) break;
        }
        return PageHelp?.Invoke() ?? HelpText;
    }
    internal event Action<bool>? Closed;
    internal event Action? Disposing;
    internal Func<CancellationToken, Task<bool>>? SaveAsync { get; set; }
    internal static T Field<T>(Window parent, BoxSizer body, string label, Func<T> create, int proportion = 0) where T : Window
        => Field(parent, body, label, create, out _, proportion);

    internal static T Field<T>(Window parent, BoxSizer body, string label, Func<T> create, out StaticText caption, int proportion = 0) where T : Window
    {
        // Windows associates a static label with the next native sibling, so creation order must
        // match visual order. Setting Name alone does not establish that relationship for MSAA.
        caption = new StaticText(parent, label: label);
        body.Add(caption, flags: SizerFlags.BorderTop | SizerFlags.BorderBottom, border: 5);
        var control = create();
        control.Name = label;
        body.Add(control, proportion: proportion, flags: SizerFlags.Expand | SizerFlags.BorderBottom, border: 5);
        return control;
    }
    internal TextCtrl Text(string label, string value = "", bool multiline = false, bool secret = false)
    {
        var field = Field(Panel, Body, label, () => new TextCtrl(Panel, value: value,
            style: multiline ? TextCtrlStyle.MultiLine : secret ? TextCtrlStyle.Password : TextCtrlStyle.None), proportion: multiline ? 1 : 0);
        if (multiline) field.MinSize = new Size(300, 100);
        return field;
    }
    internal CheckBox Check(string label, bool value = false)
    { var field = new CheckBox(Panel, label: label) { Checked = value }; Body.Add(field, flags: SizerFlags.All, border: 5); return field; }
    internal Choice Choice(string label, IEnumerable<string> values, int selected = 0)
    { var field = Field(Panel, Body, label, () => new Choice(Panel)); foreach (var value in values) field.Add(value); field.SelectedIndex = selected; return field; }
    internal SpinCtrl Number(string label, int value, int minimum = 0, int maximum = 9999)
        => Field(Panel, Body, label, () => new SpinCtrl(Panel, value, minimum, maximum));
    internal Button Button(string label, Action action)
    { var button = new Button(Panel, label: label); button.Click += (_, _) => action(); Body.Add(button, flags: SizerFlags.All, border: 5); return button; }
    internal string? ChooseFolder(string current)
    { using var picker = new FolderDialog(Window, Words.Folder, current); return picker.Show(); }
    internal void Show() { Window.Layout(); _owner.Show(); }
    internal void Complete(bool accepted) => _owner.Complete(accepted);
    internal void Success(string message) => Messages.Success(message, Window, ownerOnly: true);
    internal void Info(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, Window, ownerOnly: true);
    internal void Error(Exception error) => Messages.Show(Words.Error(error), MessageBoxStyle.Ok | MessageBoxStyle.IconError, Window, ownerOnly: true);
    internal bool Confirm(string message) => Messages.Show(message, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, Window, ownerOnly: true) == MessageBoxStyle.Yes;
    internal void Post(Action action)
    {
        _lifetime.Post(Wx.CallAfter, () => { if (!_closed) action(); });
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _closed = true;
        _lifetime.Dispose();
        Disposing?.Invoke();
        _owner.Dispose();
    }
}
