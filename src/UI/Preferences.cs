using WxSharp;

namespace LunaPlayer.UI;

internal interface IPreferences
{
    Window Window { get; }
    string GetContextHelp(Window? focused);
    string? Validate();
    void Apply();
    void Refresh();
}

internal abstract class Preferences : IPreferences
{
    private readonly string _defaultHelp;
    private readonly Dictionary<Window, Func<string>> _help = [];

    protected Preferences(Window window, string defaultHelp)
    {
        Window = window;
        _defaultHelp = defaultHelp;
    }

    public Window Window { get; }
    public virtual string GetContextHelp(Window? focused)
    {
        for (var control = focused; control is not null; control = control.Parent)
        {
            if (_help.TryGetValue(control, out var help)) return help();
            if (ReferenceEquals(control, Window)) break;
        }
        return _defaultHelp;
    }

    public virtual string? Validate() => null;
    public abstract void Apply();
    public virtual void Refresh() { }
    protected void Help(Window control, string text) => _help[control] = () => text;
    protected void Help(Window control, Func<string> text) => _help[control] = text;
}
