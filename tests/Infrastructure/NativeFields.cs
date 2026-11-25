using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LunaPlayer.Tests.Infrastructure;

internal static class NativeFields
{
    internal static object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, object owner, string name)
        => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
}
