using LunaPlayer.Actions;

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

    internal bool Execute(ActionId id)
    {
        if (!_handlers.TryGetValue(id, out var handler))
            return false;
        handler();
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
