using LunaPlayer.UI.Services;
using System.Text.Json;
using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed partial class BrowserDialog
{
    private void EnsureLoaded()
    {
        if (!_visible || _results.Loading || _results.View is not { } view) return;
        var source = _results;
        var changes = view.Updates.ToArray();
        var missing = view.Stations.Skip(source.Rows.Count).ToArray();
        if (changes.Length == 0 && missing.Length == 0)
        {
            if (!source.Loaded) LoadMore();
            return;
        }
        var token = BeginRead();
        Run(async cancellation =>
        {
            foreach (var batch in changes.Chunk(32))
            {
                cancellation.ThrowIfCancellationRequested();
                var rows = batch.Select(_text.Station).ToArray();
                _updates.Post(() =>
                {
                    foreach (var row in rows)
                        if (source.Update(row) && ReferenceEquals(source, _results)) _rowValuesChanged = true;
                }, cancellation);
            }
            foreach (var batch in missing.Chunk(32))
            {
                cancellation.ThrowIfCancellationRequested();
                await PublishAsync(source, batch.Select(_text.Station).ToArray(), cancellation).ConfigureAwait(false);
            }
            return true;
        }, _ =>
        {
            // Keep pending changes until their UI records have actually been applied. Cancellation
            // while another category is selected must not lose an unapplied metadata update.
            view.Updates.RemoveRange(0, changes.Length);
            source.Loading = false;
            if (!source.Loaded) LoadMore(); else QueueNextPage();
        }, token, () => source.Loading = false);
    }

    private CancellationToken BeginRead()
    {
        CancelRead();
        _request = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _results.Loading = true;
        return _request.Token;
    }

    private void LoadMore(bool announce = false)
    {
        if (_disposed || !_visible || _results.Loading || !_results.HasMore || _results.View is not { } view) return;
        var source = _results;
        var before = source.Rows.Count;
        if (announce && source.Loaded && before > 0)
            // Translators: Spoken only when reaching the end of the radio list requests another batch of stations.
            _speak(Tr("Loading more stations…"));
        var token = BeginRead();
        var request = view.Request();
        Run(async cancellation =>
        {
            Exception? warning = null;
            try { await _library.LoadAsync().WaitAsync(cancellation).ConfigureAwait(false); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            {
                // Browsing remains usable with an unreadable local library. Local operations still
                // fail their load, so they cannot overwrite the unreadable data.
                if (request.Category.Kind is CategoryKind.Favorites or CategoryKind.Recent) throw;
                warning = error;
            }
            if (request.Category.Kind is CategoryKind.Favorites or CategoryKind.Recent)
                await _library.FlushAsync().WaitAsync(cancellation).ConfigureAwait(false);
            var page = await _catalog.StationsAsync(request, _library, cancellation,
                (items, readToken) => PublishAsync(source, items.Select(_text.Station).ToArray(), readToken)).ConfigureAwait(false);
            return (Page: page, Warning: warning);
        }, result =>
        {
            // The reader has already published every row. Completion only advances page metadata;
            // formatting and adding the full response again would duplicate the streamed work.
            view.Offset = result.Page.NextOffset;
            view.HasMore = result.Page.HasMore;
            view.Loaded = true;
            source.Loading = false;
            if (source.HasMore && source.Rows.Count == before) LoadMore(); else QueueNextPage();
            if (result.Warning is { } warning && !_libraryWarningShown)
            {
                _libraryWarningShown = true;
                Messages.Show(Catalog.DescribeError(warning), MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            }
        }, token, () => source.Loading = false);
    }

    private void AppendRows(Results source, IReadOnlyList<ResultRow> rows)
    {
        source.Append(rows);
        if (ReferenceEquals(source, _results)) _rowsChanged = true;
    }

    private ValueTask PublishAsync(Results source, ResultRow[] rows, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _updates.Post(() =>
        {
            if (_visible && ReferenceEquals(source, _results)) AppendRows(source, rows);
        }, token);
        return ValueTask.CompletedTask;
    }

    private void RenderRows()
    {
        if (_disposed || !_visible || !_rowsChanged && !_rowValuesChanged) return;
        var countChanged = _rowsChanged;
        var valuesChanged = _rowValuesChanged;
        _rowsChanged = _rowValuesChanged = false;
        if (countChanged) _list.UpdateItemCount();
        if (valuesChanged && _results.Rows.Count > 0)
        {
            var first = Math.Clamp(_list.TopItem, 0, _results.Rows.Count - 1);
            _list.RefreshItems(first, Math.Min(_results.Rows.Count - 1, first + Math.Max(1, _list.CountPerPage)));
        }
        // A restored row cannot be selected until its native item count has been published.
        RestoreStation();
        UpdateButtons();
    }

    private void QueueNextPage()
    {
        if (_disposed || !_visible || _binding || _pageQueued || _results.Loading
            || _results.IsDirectory || !_results.Loaded || !_results.HasMore || _results.Rows.Count == 0) return;
        var source = _results;
        _pageQueued = true;
        _updates.Post(() =>
        {
            _pageQueued = false;
            if (!_visible || !ReferenceEquals(source, _results)) return;
            if (_list.TopItem + Math.Max(1, _list.CountPerPage) >= source.Rows.Count) LoadMore(announce: true);
        }, _lifetime.Token);
    }

    private void CancelRead()
    {
        var request = _request;
        _request = null;
        _results.Loading = false;
        if (request is not null) _ = CancelAsync(request);
    }

    private async Task CancelAsync(CancellationTokenSource source)
    {
        try { await source.CancelAsync().ConfigureAwait(false); }
        catch (Exception error) { if (!_disposed) _updates.Post(() => ShowError(error), _lifetime.Token); }
        finally { source.Dispose(); }
    }

    private void Run<T>(Func<CancellationToken, Task<T>> work, Action<T> completed,
        CancellationToken token, Action? failed = null, bool background = false)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var result = await work(token).ConfigureAwait(false);
                _updates.Post(() => completed(result), token, background);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Navigation and closing abandon the previous read, including its queued UI records.
            }
            catch (Exception error)
            {
                _updates.Post(() => { failed?.Invoke(); ShowError(error); }, token, background);
            }
        });
    }

    private void ShowError(Exception error)
    {
        if (_disposed) return;
        if (!_visible) { _pendingError = error; return; }
        Messages.Show(Catalog.DescribeError(error), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
    }

    private void RestoreStation()
    {
        if (_restoreStation is not { } id) return;
        var index = _results.Rows.FindIndex(row => row.Station.StationUuid == id);
        if (index < 0) return;
        _restoreStation = null;
        _list.SetSelected(index);
        _list.SetFocused(index);
    }
}
