namespace LunaPlayer.Application;

internal interface IApplicationDispatcher
{
    void Post(Action action);
}
