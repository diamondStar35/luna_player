using LunaPlayer.Configuration;
namespace LunaPlayer.Playlist;

internal sealed class Navigation(List<PlaylistItem> items)
{
    private readonly List<PlaylistItem> _items = items;
    internal readonly ShuffleOrder Order = new();
    internal int SourceIndex { get; set; } = -1;
    internal double? PendingStart { get; set; }
    internal bool Enabled { get; private set; }

    internal bool MoveNext(bool wrap)
        => Enabled ? MoveNextShuffled(wrap) : MoveNextSequential(wrap);

    internal bool MovePrevious(bool wrap)
        => Enabled ? MovePreviousShuffled(wrap) : MovePreviousSequential(wrap);

    /// <summary>The item Next would choose without changing the current item.</summary>
    internal PlaylistItem? PeekNext(bool wrap)
    {
        if (_items.Count < 2)
            return null;
        if (Enabled)
        {
            SyncShufflePosition();
            if (Order.Position < 0)
                return _items[Order[0]];
            var position = Order.Position + 1;
            if (position >= Order.Count)
            {
                if (!wrap)
                    return null;
                position = 0;
            }
            return _items[Order[position]];
        }
        var index = SourceIndex + 1;
        if (index >= _items.Count)
        {
            if (!wrap)
                return null;
            index = 0;
        }
        return index >= 0 && index < _items.Count ? _items[index] : null;
    }

    internal bool GoToFirst() => GoToIndex(0);

    internal bool GoToLast() => GoToIndex(_items.Count - 1);

    internal bool GoToIndex(int index)
    {
        if (index < 0 || index >= _items.Count)
            return false;
        if (Enabled)
        {
            SyncShufflePosition();
            var fileIndex = Order[index];
            if (fileIndex == SourceIndex)
                return false;
            SourceIndex = fileIndex;
            Order.Position = index;
        }
        else
        {
            if (index == SourceIndex)
                return false;
            SourceIndex = index;
        }
        PendingStart = null;
        return true;
    }

    internal bool ToggleShuffle()
    {
        Enabled = !Enabled;
        if (Enabled)
            CreateShuffleOrder();
        else
            ClearShuffleOrder();
        return Enabled;
    }

    internal double? TakePendingStart()
    {
        var value = PendingStart;
        PendingStart = null;
        return value;
    }

    private bool MoveNextSequential(bool wrap)
    {
        if (_items.Count == 0)
            return false;
        if (SourceIndex < 0 || SourceIndex >= _items.Count)
        {
            SourceIndex = 0;
            PendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (SourceIndex + 1 >= _items.Count)
        {
            if (!wrap)
                return false;
            SourceIndex = 0;
        }
        else
        {
            SourceIndex++;
        }
        PendingStart = null;
        SyncShufflePosition();
        return true;
    }

    private bool MovePreviousSequential(bool wrap)
    {
        if (_items.Count == 0)
            return false;
        if (SourceIndex < 0 || SourceIndex >= _items.Count)
        {
            SourceIndex = _items.Count - 1;
            PendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (SourceIndex <= 0)
        {
            if (!wrap)
                return false;
            SourceIndex = _items.Count - 1;
        }
        else
        {
            SourceIndex--;
        }
        PendingStart = null;
        SyncShufflePosition();
        return true;
    }

    private bool MoveNextShuffled(bool wrap)
    {
        if (_items.Count == 0)
            return false;
        SyncShufflePosition();
        if (Order.Position < 0)
        {
            Order.Position = 0;
            SourceIndex = Order[0];
            PendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (Order.Position + 1 >= Order.Count)
        {
            if (!wrap)
                return false;
            Order.Position = 0;
        }
        else
        {
            Order.Position++;
        }
        SourceIndex = Order[Order.Position];
        PendingStart = null;
        return true;
    }

    private bool MovePreviousShuffled(bool wrap)
    {
        if (_items.Count == 0)
            return false;
        SyncShufflePosition();
        if (Order.Position < 0)
        {
            Order.Position = Order.Count - 1;
            SourceIndex = Order[Order.Position];
            PendingStart = null;
            return true;
        }
        if (_items.Count == 1)
            return false;
        if (Order.Position <= 0)
        {
            if (!wrap)
                return false;
            Order.Position = Order.Count - 1;
        }
        else
        {
            Order.Position--;
        }
        SourceIndex = Order[Order.Position];
        PendingStart = null;
        return true;
    }

    internal void SyncShufflePosition()
    {
        if (!Enabled)
            return;
        Order.Sync(_items.Count, SourceIndex);
    }

    /// <summary>Creates the one random order used until shuffle is turned off or a new playlist is opened.
    /// The current item is shuffled like every other item; keeping its old numeric position is what made
    /// the first and last entries remain at the edges.</summary>
    internal void CreateShuffleOrder()
    {
        Order.Create(_items.Count, SourceIndex);
    }

    internal void ClearShuffleOrder()
    {
        Order.Clear();
    }
}
