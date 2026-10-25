using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed class FiltersDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Choice _scope;
    private readonly TextCtrl _country;
    private readonly TextCtrl _region;
    private readonly TextCtrl _language;
    private readonly TextCtrl _tags;
    private readonly TextCtrl _codec;
    private readonly SpinCtrl _minimum;
    private readonly SpinCtrl _maximum;
    private readonly CheckBox _working;
    private readonly CheckBox _https;
    private Filters _global;
    private Filters _local;
    private bool _editingGlobal;
    private FilterChoice? _choice;

    internal FiltersDialog(Window parent, Browsing browsing, Category category)
    {
        _global = browsing.GlobalFilters;
        _local = browsing.LocalFilters(category);
        _editingGlobal = !browsing.HasLocalFilters(category);
        // Translators: Title of the dialog for filtering radio station lists.
        _dialog = new Dialog(parent, title: Tr("Radio filters"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var layout = new BoxSizer(Orientation.Vertical);
        // Translators: Label for choosing whether radio filters apply to this category or every category.
        layout.Add(new StaticText(_dialog, label: Tr("Apply to")), flags: SizerFlags.All, border: 6);
        _scope = new Choice(_dialog);
        // Translators: Scope option in Radio filters; these restrictions are combined with the global filters.
        _scope.Add(Tr("This category"));
        // Translators: Scope option in Radio filters that affects every category, including favorites and history.
        _scope.Add(Tr("All categories"));
        _scope.SelectedIndex = _editingGlobal ? 1 : 0;
        layout.Add(_scope, flags: SizerFlags.All | SizerFlags.Expand, border: 6);
        // Translators: Explanation in Radio filters. Category restrictions narrow the global restrictions.
        var explanation = new StaticText(_dialog, label: Tr("Category filters are combined with the global filters. Leave a field empty to add no restriction."));
        explanation.Wrap(480);
        layout.Add(explanation, flags: SizerFlags.All, border: 6);
        // Translators: Label for a two-letter country code, such as EG, in Radio filters.
        _country = Field(layout, Tr("Country code"));
        // Translators: Label for a state or province name in Radio filters.
        _region = Field(layout, Tr("Region"));
        // Translators: Label for the language spoken by radio stations in Radio filters.
        _language = Field(layout, Tr("Language"));
        // Translators: Label for required radio station tags in Radio filters. Every entered tag must match.
        _tags = Field(layout, Tr("Tags, separated by commas"));
        // Translators: Label for an audio codec, such as MP3 or AAC, in Radio filters.
        _codec = Field(layout, Tr("Codec"));
        // Translators: Label for the lowest acceptable radio stream bitrate. Zero adds no lower limit.
        layout.Add(new StaticText(_dialog, label: Tr("Minimum bitrate (kbps; 0 for no limit)")), flags: SizerFlags.All, border: 6);
        _minimum = new SpinCtrl(_dialog, minimum: 0, maximum: 1_000_000);
        layout.Add(_minimum, flags: SizerFlags.All | SizerFlags.Expand, border: 6);
        // Translators: Label for the highest acceptable radio stream bitrate. Zero adds no upper limit.
        layout.Add(new StaticText(_dialog, label: Tr("Maximum bitrate (kbps; 0 for no limit)")), flags: SizerFlags.All, border: 6);
        _maximum = new SpinCtrl(_dialog, minimum: 0, maximum: 1_000_000);
        layout.Add(_maximum, flags: SizerFlags.All | SizerFlags.Expand, border: 6);
        // Translators: Checkbox in Radio filters excluding stations last reported as unavailable by Radio Browser.
        _working = new CheckBox(_dialog, label: Tr("Only working stations"));
        layout.Add(_working, flags: SizerFlags.All, border: 6);
        // Translators: Checkbox in Radio filters requiring an HTTPS stream address.
        _https = new CheckBox(_dialog, label: Tr("HTTPS only"));
        layout.Add(_https, flags: SizerFlags.All, border: 6);
        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Button that applies the selected radio filters and closes the filter dialog.
        var apply = new Button(_dialog, label: Tr("Apply"));
        apply.Click += (_, _) => Accept();
        apply.SetDefault();
        // Translators: Button restoring default values in the radio filter dialog, without applying them yet.
        var reset = new Button(_dialog, label: Tr("Reset"));
        reset.Click += (_, _) => Load(new Filters { OnlyWorking = _editingGlobal });
        // Translators: Button closing Radio filters without applying changes.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        buttons.Add(apply, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(reset, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);
        layout.Add(buttons, flags: SizerFlags.All, border: 6);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.Center();
        Load(_editingGlobal ? _global : _local);
        _scope.SelectionChanged += (_, _) => ChangeScope();
    }

    private TextCtrl Field(BoxSizer layout, string label)
    {
        layout.Add(new StaticText(_dialog, label: label), flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 6);
        var field = new TextCtrl(_dialog);
        layout.Add(field, flags: SizerFlags.All | SizerFlags.Expand, border: 6);
        return field;
    }

    internal FilterChoice? Show() { _dialog.ShowModal(); return _choice; }
    public void Dispose() => _dialog.Dispose();

    private Filters Read() => new()
    {
        CountryCode = _country.Value.Trim().ToUpperInvariant(), Region = _region.Value.Trim(),
        Language = _language.Value.Trim(), Tags = _tags.Value.Trim(), Codec = _codec.Value.Trim(),
        MinimumBitrate = (uint)_minimum.Value, MaximumBitrate = (uint)_maximum.Value,
        OnlyWorking = _working.Checked, HttpsOnly = _https.Checked,
    };

    private void Load(Filters filters)
    {
        _country.ChangeValue(filters.CountryCode);
        _region.ChangeValue(filters.Region);
        _language.ChangeValue(filters.Language);
        _tags.ChangeValue(filters.Tags);
        _codec.ChangeValue(filters.Codec);
        _minimum.Value = (int)filters.MinimumBitrate;
        _maximum.Value = (int)filters.MaximumBitrate;
        _working.Checked = filters.OnlyWorking;
        _https.Checked = filters.HttpsOnly;
    }

    private void ChangeScope()
    {
        if (_editingGlobal) _global = Read(); else _local = Read();
        _editingGlobal = _scope.SelectedIndex == 1;
        Load(_editingGlobal ? _global : _local);
    }

    private void Accept()
    {
        var filters = Read();
        if (filters.CountryCode.Length is not (0 or 2) || !filters.CountryCode.All(char.IsAsciiLetter)
            || filters.MaximumBitrate > 0 && filters.MinimumBitrate > filters.MaximumBitrate)
        {
            Wx.MessageBox(
                // Translators: Validation error in Radio filters, where country codes have two letters and bitrate limits define a range.
                Tr("Enter a two-letter country code and a minimum bitrate no greater than the maximum."),
                // Translators: Title of a validation error in Radio filters.
                Tr("Invalid filters"), MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        _choice = new FilterChoice(filters, _editingGlobal);
        _dialog.EndModal(StandardId.Ok);
    }
}
