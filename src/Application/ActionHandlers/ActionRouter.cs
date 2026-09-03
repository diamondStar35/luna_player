using LunaPlayer.Actions;
using LunaPlayer.Application;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class ActionRouter
{
    private readonly Dictionary<ActionId, Action> _handlers = [];

    internal void Register(ActionId id, Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(id, handler))
            throw new InvalidOperationException($"An action handler is already registered for {id}.");
    }

    /// <remarks>
    /// Every command the user gives passes through here, and every one of them is called from a menu, a
    /// button or a key - all of which are wxWidgets calling into this program. So this is the boundary an
    /// exception has to be caught at: past it there are only C++ frames, which it cannot be unwound
    /// through.
    /// </remarks>
    internal bool Execute(ActionId id)
    {
        if (!_handlers.TryGetValue(id, out var handler))
            return false;
        CrashReport.Guard(handler);
        return true;
    }

    internal void EnsureComplete(IEnumerable<ActionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (!_handlers.ContainsKey(definition.Id))
                throw new InvalidOperationException($"No action handler is registered for {definition.Id}.");
        }
    }
}
