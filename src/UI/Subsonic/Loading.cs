using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    internal void ContentChanged(Guid server)
    {
        if (_disposed || !_initialized || _server != server) return;
        if (_visible) Reload();
        else _rows = [];
    }
    internal void ConnectionStatus(Guid server, Exception? error)
    {
        if (_disposed || _server != server || error is null) return;
        if (_visible) _speak(Words.Error(error));
    }
    private void Reload()
    {
        if (_disposed || _server == Guid.Empty || !_visible) return;
        _reading.Cancel(); _reading.Dispose(); _reading = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var token = _reading.Token; var revision = ++_revision; var server = _server;
        var query = _search.Value.Trim(); _query = query; var type = _types.SelectedIndex;
        var library = _libraries.SelectedIndex > 0 && _libraries.SelectedIndex <= _libraryIds.Length ? _libraryIds[_libraries.SelectedIndex - 1] : null;
        var location = _location; SynchronizeView(); UpdateButtons();

        var scope = _connections.Profile(server).LibraryIds;
        var derived = _derived;
        Run(async cancellation => derived is not null ? await derived(cancellation)
            : await _connections.Catalog(server).ReadAsync(location, query, type, library, null, cancellation, scope),
            rows => { if (revision == _revision && _server == server) { _rows = rows; _connections.Metadata.Observe(server, rows.Where(row => row.Song is not null).Select(row => row.Song!)); Sort(); } }, token, error =>
            {
                if (derived is null && error is LunaPlayer.Subsonic.RequestException { Kind: LunaPlayer.Subsonic.FailureKind.Unsupported } && query.Length == 0 && location.Container is null)
                { _unsupported.Add((server, location.Category)); Categories(); _rows = []; Publish([]); Error(error); }
                else Error(error);
            });
    }
    private string[] SelectedLibraries() => _libraries.SelectedIndex > 0 && _libraries.SelectedIndex <= _libraryIds.Length
        ? [_libraryIds[_libraries.SelectedIndex - 1]] : _connections.Profile(_server).LibraryIds;
    private void Refresh()
    {
        if (_server == Guid.Empty || _disposed) return;
        _reading.Cancel(); _revision++;
        _player.RetryServerMedia(_server); _unsupported.RemoveWhere(entry => entry.Server == _server);
        _unavailableCommands.RemoveWhere(entry => entry.Server == _server); Categories();
        var server = _server; var revision = _connectionRevision;
        var library = _libraries.SelectedIndex > 0 ? _libraryIds[_libraries.SelectedIndex - 1] : null;
        Run(async token =>
        {
            await _connections.Catalog(server).InvalidateAsync(token);
            return await _connections.InspectAsync(server, refresh: true, token);
        }, information =>
        {
            if (_server != server || revision != _connectionRevision) return;
            SetInspection(information, library); Reload();
        });
    }
    private int _sortRevision;
    private void Sort()
    {
        if (_binding || _disposed) return;
        var rows = _rows; var minimum = _minimumYear; var maximum = _maximumYear;
        var revision = ++_sortRevision; var loading = _revision;
        var field = (SortField)Math.Max(0, _sort.SelectedIndex); var descending = _direction.SelectedIndex == 1;
        Run(_ => Task.FromResult(Ordering.Sort(rows.Where(x => (minimum == 0 || x.Year >= minimum)
            && (maximum == 0 || x.Year <= maximum)), field, descending)), result =>
        {
            if (!_visible || revision != _sortRevision || loading != _revision) return;
            Publish(result);
        });
    }
    private void Publish(Row[] rows)
    {
        if (ListPosition.SameRows(_view, rows) && _columns.SequenceEqual(Columns.For(_location, rows, _query.Length > 0)))
        {
            // The virtual source already points to this array. Updating cells in place preserves
            // native focus, multi-selection and scrolling during playback metadata updates.
            Array.Copy(rows, _view, rows.Length);
            if (_view.Length > 0) _list.RefreshItems(0, _view.Length - 1);
        }
        else
        {
            var position = ListPosition.Capture(_view, _list.GetSelectedIndices(), _list.FocusedIndex, _list.TopItem);
            _view = rows; ConfigureColumns(); _list.SetDataSource(_view, Cell);
            var restored = position.Restore(rows);
            foreach (var index in restored.Selected) _list.SetSelected(index);
            if (restored.Focused >= 0) _list.SetFocused(restored.Focused);
            if (restored.Top >= 0) _list.EnsureVisible(restored.Top);
        }
        UpdateButtons();
    }
    private async void Run<T>(Func<CancellationToken, Task<T>> work, Action<T> done, CancellationToken? cancellation = null, Action<Exception>? failed = null, Command? command = null)
    {
        if (_disposed) return;
        var token = cancellation ?? _token; var server = _server;
        try
        {
            var result = await Task.Run(() => work(token), token).ConfigureAwait(false);
            Post(() => { try { done(result); } catch (Exception error) { Error(error); } }, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { Post(() => { if (command is { } operation) RecordUnavailable(server, operation, error);
            if (failed is not null) failed(error);
            else if (_visible && _window.Enabled) Error(error);
            else if (_visible) _speak(Words.Error(error)); }, token); }
    }
    private void Post(Action action, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return;
        _lifetime.Post(_dispatcher.Post, () => { if (!_disposed && !token.IsCancellationRequested) action(); });
    }
    private void Info(string text) => Services.Messages.Show(text, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, _window, ownerOnly: true);
    private void Error(Exception error) => Services.Messages.Show(Words.Error(error), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _window, ownerOnly: true);
    private bool Confirm(string text) => Services.Messages.Show(text, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, _window, ownerOnly: true) == MessageBoxStyle.Yes;
    private void Filters()
    {
        var form = new Form(_window, Words.Filters); var minimum = form.Number(Words.MinimumYear, _minimumYear);
        var maximum = form.Number(Words.MaximumYear, _maximumYear);
        form.SaveAsync = _ => { if (minimum.Value > 0 && maximum.Value > 0 && minimum.Value > maximum.Value) throw new ArgumentException(Words.InvalidYears); _minimumYear = minimum.Value; _maximumYear = maximum.Value; Sort(); return Task.FromResult(true); };
        Keep(form, form); form.Show();
    }
    private sealed class Callback<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
}
