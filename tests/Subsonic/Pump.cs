using System.Collections.Concurrent;
using LunaPlayer.Application;
namespace LunaPlayer.Tests.Subsonic;

internal sealed class Pump : IApplicationDispatcher
{
    private readonly ConcurrentQueue<Action> _posts = new();
    public void Post(Action action) => _posts.Enqueue(action);
    public IDisposable Repeat(TimeSpan interval, Action action) => throw new NotSupportedException();
    internal bool HasPosts => !_posts.IsEmpty;
    internal void Drain() { while (_posts.TryDequeue(out var action)) action(); }
    internal async Task Until(Func<bool> condition) => await Wait(() => { Drain(); return condition(); });
    internal static async Task Wait(Func<bool> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(5, deadline.Token);
    }
}
