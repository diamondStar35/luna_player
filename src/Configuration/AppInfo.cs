using System.Reflection;

namespace LunaPlayer.Configuration;

/// <summary>What the player calls itself, in each of the forms something needs it in.</summary>
///
/// <remarks>
/// These were spread across a dozen files as literals, which is how a rename turns into a hunt and how the
/// window ends up titled one thing while the settings folder is named another. They live here instead.
///
/// There are deliberately several names, because they are not interchangeable:
/// <see cref="Name"/> is written for a person to read, <see cref="Identifier"/> is the one safe in a file
/// name, a registry key or a mutex, and <see cref="AppUserModelId"/> is what Windows identifies the process
/// by. Changing any of them is a compatibility break - the settings folder, the file associations and the
/// running-instance handshake are all found by these strings, so an existing installation stops finding its
/// own settings if <see cref="Name"/> or <see cref="Identifier"/> moves.
/// </remarks>
internal static class AppInfo
{
    /// <summary>The name shown to the user: window titles, message boxes, the media overlay.</summary>
    internal const string Name = "Luna Player";

    /// <summary>The name used where a space or punctuation would be awkward or unsafe - registry keys, the
    /// message catalogue, the single-instance mutex and pipe.</summary>
    internal const string Identifier = "LunaPlayer";

    /// <summary>Who publishes it. Part of <see cref="AppUserModelId"/>, and the natural owner of any
    /// registry key written under a vendor name.</summary>
    internal const string Publisher = "diamondStar35";

    /// <summary>The AppUserModelID this process registers itself under.</summary>
    ///
    /// <remarks>
    /// Windows identifies a process by this for taskbar grouping, jump lists, and - the reason it is here -
    /// the media overlay, which shows the owner of a playback session as "Unknown app" when the process has
    /// none. The convention is <c>CompanyName.ProductName</c>, no more than 128 characters and no spaces.
    ///
    /// Windows turns this into a name and an icon by matching it against a Start menu shortcut carrying the
    /// same <c>System.AppUserModel.ID</c> property. Until an installer creates one there is nothing to match,
    /// so setting it groups the windows correctly but the overlay may still not have a friendly name to show.
    /// </remarks>
    internal const string AppUserModelId = $"{Publisher}.{Identifier}";

    /// <summary>Where the source lives, for an about box or a link.</summary>
    internal const string RepositoryUrl = "https://github.com/diamondStar35/luna_player";

    /// <summary>The release version. Read from the assembly rather than written out again here, so it
    /// follows the one in the project file instead of drifting from it.</summary>
    internal static string Version { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";
}
