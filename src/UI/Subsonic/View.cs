using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void SynchronizeView(bool announce = false)
    {
        var searching = _search.Value.Trim().Length > 0;
        var content = Views.Content(_location, searching, _types.SelectedIndex);
        var section = searching ? content switch
        {
            ViewContent.Artists => Category.Artists, ViewContent.Albums => Category.Albums,
            ViewContent.Songs => Category.Songs, _ => Views.Section(_location)
        } : Views.Section(_location);
        var binding = _binding; _binding = true;
        try
        {
            var node = _categories.FirstOrDefault(item => item.Value == section);
            // Hidden sections stay hidden. Keep their originating branch selected when possible.
            if (!node.Key.IsValid) node = _categories.FirstOrDefault(item => item.Value == _location.Category);
            if (node.Key.IsValid) _tree.Selection = node.Key;
            else _tree.Unselect();
        }
        finally { _binding = binding; }
        var heading = Views.Heading(content); var changed = _resultsLabel.Label != heading;
        _resultsLabel.Label = heading; _list.Name = heading;
        if (changed) _window.Layout();
        if (_visible && announce) _speak(heading);
    }
}
