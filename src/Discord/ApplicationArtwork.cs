using System.Diagnostics;
using System.Text.Json;

namespace LunaPlayer.Discord;

internal interface IApplicationArtwork
{
    Task<string?> ReadAsync(string applicationId, CancellationToken token);
}

/// <summary>The application icon is public metadata, distinct from named Rich Presence assets.</summary>
internal sealed class ApplicationArtwork : IApplicationArtwork
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(3), MaxResponseContentBufferSize = 64 * 1024 };

    public async Task<string?> ReadAsync(string applicationId, CancellationToken token)
    {
        try
        {
            var json = await Client.GetStringAsync($"https://discord.com/api/v10/applications/{applicationId}/rpc", token).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            return IconUrl(applicationId, document.RootElement);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            token.ThrowIfCancellationRequested();
            Trace.TraceWarning("Could not refresh Discord application artwork: {0}", exception.Message);
            return null;
        }
    }

    internal static string? IconUrl(string applicationId, JsonElement application)
    {
        if (application.ValueKind != JsonValueKind.Object
            || !application.TryGetProperty("icon", out var icon) || icon.ValueKind != JsonValueKind.String) return null;
        var hash = icon.GetString()!;
        var digits = hash.StartsWith("a_", StringComparison.Ordinal) ? hash[2..] : hash;
        return digits.Length == 32 && digits.All(char.IsAsciiHexDigit)
            ? $"https://cdn.discordapp.com/app-icons/{applicationId}/{hash}.png?size=256" : null;
    }
}
