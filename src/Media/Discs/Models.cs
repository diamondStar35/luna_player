using System.Globalization;

namespace LunaPlayer.Media.Discs;

internal enum DiscFormat { Video, Audio }
internal enum Failure { InvalidDisc, NoAudioTitles, NoVideoTitles, Unreadable }
internal sealed class DiscException(Failure failure, string? detail = null) : Exception(detail)
{
    internal Failure Failure { get; } = failure;
}

internal sealed record Title(int Index);
internal sealed record DiscInfo(string Directory, DiscFormat Format, string Identity, string Name,
    IReadOnlyList<Title> Titles, int DefaultTitle)
{
    internal string Address(int title) => Addressing.Build(Format, Directory, title);
    internal string PositionKey(int title) => $"disc://{Format}/{Identity}/{title}";
}

/// <summary>mpv disc addresses carry both the device directory and zero-based title, so queued
/// titles never depend on mutable global dvd-device or edition options.</summary>
internal static class Addressing
{
    internal static bool IsDisc(string? path)
        => path is not null && (path.StartsWith("dvd://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("dvda://", StringComparison.OrdinalIgnoreCase));
    internal static string Build(DiscFormat format, string directory, int? title = null)
        => $"{(format == DiscFormat.Audio ? "dvda" : "dvd")}://{title?.ToString(CultureInfo.InvariantCulture) ?? ""}/{directory.Replace('\\', '/')}";
}
