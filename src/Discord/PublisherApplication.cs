namespace LunaPlayer.Discord;

/// <summary>The repository owner's public Discord application identity, shared by every Luna install.
/// This is NOT an API key, bot token, client secret, or user credential.</summary>
internal static class PublisherApplication
{
    // MERGE BLOCKER: the repository owner must replace this empty value with the Application ID
    // of their own Discord developer application named "Luna Player", then commit and test it.
    // See docs/discord-maintainer-setup.md. Never commit a token or secret here.
    internal const string ApplicationId = "";

    internal static bool IsConfigured => IsValidApplicationId(ApplicationId);

    internal static bool IsValidApplicationId(string? value)
        => value is { Length: >= 17 and <= 20 } && value.All(char.IsAsciiDigit)
            && ulong.TryParse(value, out var id) && id > 0;
}
