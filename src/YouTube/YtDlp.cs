using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LunaPlayer.Configuration;

namespace LunaPlayer.YouTube;

/// <summary>What one run of yt-dlp produced.</summary>
/// <param name="Lines">Its output, blank lines dropped.</param>
/// <param name="Diagnostic">The first line of its complaint, when it failed. Empty when it did not.</param>
internal readonly record struct YtDlpRun(IReadOnlyList<string> Lines, string Diagnostic)
{
    internal bool Failed => Diagnostic.Length > 0;
}

/// <summary>Talks to yt-dlp.</summary>
///
/// <remarks>
/// The optional half of the player's YouTube support. Everything it does, the player can already do
/// itself; what it adds is a second opinion for the videos YoutubeExplode cannot work out, which is what
/// the setting that turns it on is for. A faithful port of the Python player's <c>youtube/resolver.py</c>
/// and <c>youtube/download.py</c>, including the order of the fallbacks, because a video the two players
/// disagree about is a video one of them gets wrong.
///
/// Every method here runs a program and waits for it, so every one of them belongs on a worker thread.
/// </remarks>
internal sealed partial class YtDlpClient
{
    /// <summary>What to ask for when only the sound is wanted.</summary>
    private const string AudioFormat = "bestaudio[ext=m4a]/bestaudio/best";

    /// <summary>What to ask for at each video quality. The Python player's three format strings.</summary>
    private static string VideoFormat(YouTubeQuality quality) => quality switch
    {
        YouTubeQuality.Low => "best[height<=?360][ext=mp4]/best[height<=?360]/best[ext=mp4]/best",
        YouTubeQuality.Best => "best[ext=mp4]/best",
        _ => "best[height<=?720][ext=mp4]/best[height<=?720]/best[ext=mp4]/best",
    };

    internal static string Format(bool audioOnly, YouTubeQuality quality)
        => audioOnly ? AudioFormat : VideoFormat(quality);

    /// <summary>Turns a video into something playable.</summary>
    ///
    /// <remarks>
    /// Three attempts, in the Python player's order: ask for everything about the video as JSON and read
    /// the address out of it; ask for the address alone under the same format; ask for the address alone
    /// under no format at all. Each is more likely to work and less likely to be what was asked for than
    /// the one before, which is why they are tried in that order rather than the reverse.
    /// </remarks>
    internal ResolveOutcome Resolve(
        string watchUrl, YouTubeResult item, bool audioOnly, YouTubeQuality quality, CancellationToken token)
    {
        // Both, not just yt-dlp. Without Deno it still runs, and the addresses it produces are throttled
        // to the point of being unplayable - a failure that looks like a broken video rather than a missing
        // program, which is the worst way for this to go wrong.
        if (!Tools.HasAll)
            return ResolveOutcome.Failed(ResolveFailure.MissingComponents);
        var format = Format(audioOnly, quality);
        var diagnostic = string.Empty;
        try
        {
            var full = Run(["--no-playlist", "--dump-single-json", "-f", format, watchUrl], token);
            if (RateLimited(full))
                return ResolveOutcome.Failed(ResolveFailure.RateLimited, full.Diagnostic);
            if (!full.Failed && Parse(full.Lines) is JsonElement data)
            {
                var described = Describe(data, watchUrl, item);
                if (PickStream(data) is string address)
                    return Ready(described, address);
                item = described;
            }
            diagnostic = full.Diagnostic;

            var formatted = Run(["--no-playlist", "-g", "-f", format, watchUrl], token);
            if (RateLimited(formatted))
                return ResolveOutcome.Failed(ResolveFailure.RateLimited, formatted.Diagnostic);
            if (formatted.Lines.Count > 0)
                return Ready(item, formatted.Lines[0]);
            diagnostic = formatted.Diagnostic.Length > 0 ? formatted.Diagnostic : diagnostic;

            var bare = Run(["--no-playlist", "-g", watchUrl], token);
            if (RateLimited(bare))
                return ResolveOutcome.Failed(ResolveFailure.RateLimited, bare.Diagnostic);
            if (bare.Lines.Count > 0)
                return Ready(item, bare.Lines[0]);
            diagnostic = bare.Diagnostic.Length > 0 ? bare.Diagnostic : diagnostic;
        }
        catch (OperationCanceledException)
        {
            return ResolveOutcome.Cancelled;
        }
        catch (Exception failure)
        {
            return ResolveOutcome.Failed(ResolveFailure.Unknown, failure.Message);
        }
        return ResolveOutcome.Failed(ResolveFailure.NoStream, diagnostic);
    }

    /// <summary>The details of one video, for a link the user gave rather than chose from a list.</summary>
    internal YouTubeResult? Video(string watchUrl, CancellationToken token)
    {
        var run = Run(["--no-playlist", "--dump-single-json", watchUrl], token);
        return Parse(run.Lines) is JsonElement data ? Describe(data, watchUrl, YouTubeResult.None) : null;
    }

    /// <summary>The text the uploader wrote under a video, or null when it could not be read.</summary>
    internal string? Description(string watchUrl, CancellationToken token)
    {
        var run = Run(["--no-playlist", "--dump-single-json", watchUrl], token);
        return Parse(run.Lines) is JsonElement data && Text(data, "description") is string text && text.Length > 0
            ? text
            : null;
    }

    /// <summary>Every video in a playlist, and what the playlist is called.</summary>
    internal (string Title, IReadOnlyList<YouTubeResult> Items)? Playlist(string link, CancellationToken token)
    {
        var run = Run(["--flat-playlist", "--dump-single-json", "--ignore-errors", link], token);
        if (Parse(run.Lines) is not JsonElement data)
            return null;
        var items = new List<YouTubeResult>();
        if (data.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object && Entry(entry) is YouTubeResult found)
                    items.Add(found);
            }
        }
        return (Text(data, "title") ?? string.Empty, items);
    }

    /// <summary>Saves a video into <paramref name="folder"/>, reporting as the bytes arrive.</summary>
    ///
    /// <remarks>
    /// The name is left to yt-dlp's own output template, as the Python player leaves it, so a file saved by
    /// either player is called the same thing. Progress is read back off its own output: it prints one
    /// line per update and the line carries a percentage and a total, which is all the window shows.
    /// </remarks>
    /// <param name="report">The name being written, the bytes so far and the bytes expected. Called from
    /// the thread this runs on.</param>
    internal void Download(
        string watchUrl,
        string folder,
        bool audioOnly,
        YouTubeQuality quality,
        Action<string, long, long> report,
        CancellationToken token)
    {
        var arguments = new List<string>
        {
            "--newline",
            "--progress",
            "--no-warnings",
            "--no-playlist",
        };
        if (audioOnly)
            arguments.AddRange(["-x", "--audio-format", "m4a"]);
        else
            arguments.AddRange(["-f", VideoFormat(quality)]);
        arguments.AddRange(["-o", Path.Combine(folder, "%(title)s.%(ext)s")]);
        if (Tools.DenoRuntime is string runtime)
            arguments.AddRange(["--js-runtimes", runtime]);
        arguments.Add(watchUrl);

        var name = string.Empty;
        var run = Run(arguments, token, line =>
        {
            if (Destination().Match(line) is { Success: true } destination)
                name = Path.GetFileName(destination.Groups["path"].Value.Trim());
            if (Progress().Match(line) is not { Success: true } progress)
                return;
            var percent = double.Parse(progress.Groups["pct"].Value, CultureInfo.InvariantCulture);
            var total = Bytes(progress.Groups["size"].Value);
            report(name, (long)(total * percent / 100.0), total);
        });
        if (run.Failed)
            throw new InvalidOperationException(run.Diagnostic);
    }

    /// <summary>The version of the yt-dlp beside the player, or an empty string when there is none.
    /// </summary>
    internal string Version(CancellationToken token)
    {
        if (!Tools.HasYtDlp)
            return string.Empty;
        var run = Run(["--version"], token, timeout: TimeSpan.FromSeconds(60));
        return run.Failed || run.Lines.Count == 0 ? string.Empty : CleanVersion(run.Lines[0]);
    }

    /// <summary>Has yt-dlp replace itself with the newest build on a channel.</summary>
    ///
    /// <remarks>
    /// Its own updater rather than a fresh download, which is what the Python player does and is worth
    /// keeping: yt-dlp knows how to replace a running executable on Windows and a plain overwrite does not.
    /// </remarks>
    /// <param name="report">Each line yt-dlp prints, so the window shows what it is doing.</param>
    internal (string Before, string After, bool Updated) SelfUpdate(
        YtDlpChannel channel, Action<string> report, CancellationToken token)
    {
        if (!Tools.HasYtDlp)
            throw new InvalidOperationException("yt-dlp is not available.");
        var before = Version(token);
        var name = ChannelName(channel);
        var run = Run(["--update-to", $"{name}@latest"], token, report, TimeSpan.FromMinutes(5));
        if (run.Failed)
            throw new InvalidOperationException(run.Diagnostic);
        var after = Version(token);
        var updated = after.Length > 0 && before.Length > 0 && !string.Equals(before, after, StringComparison.Ordinal);
        if (!updated)
        {
            // A first install through --update-to reports no version change, because there was no version
            // before it. Its own words are the only evidence that something happened.
            updated = run.Lines.Any(line =>
                line.Contains("Updated yt-dlp to", StringComparison.OrdinalIgnoreCase));
        }
        return (before, after.Length > 0 ? after : before, updated);
    }

    internal static string ChannelName(YtDlpChannel channel) => channel switch
    {
        YtDlpChannel.Nightly => "nightly",
        YtDlpChannel.Master => "master",
        _ => "stable",
    };

    /// <summary>The repository each channel is built from.</summary>
    internal static string ChannelRepository(YtDlpChannel channel) => channel switch
    {
        YtDlpChannel.Nightly => "yt-dlp/yt-dlp-nightly-builds",
        YtDlpChannel.Master => "yt-dlp/yt-dlp-master-builds",
        _ => "yt-dlp/yt-dlp",
    };

    // ---- running it ----

    private static YtDlpRun Run(
        IEnumerable<string> arguments,
        CancellationToken token,
        Action<string>? onLine = null,
        TimeSpan? timeout = null)
    {
        var all = new List<string> { "--no-warnings", "--extractor-args", "youtube:player_client=android" };
        all.AddRange(arguments);
        // Only added when it is not already there: the download path puts it in itself, because it builds
        // its own argument list rather than going through the common prefix.
        if (Tools.DenoRuntime is string runtime && !all.Contains("--js-runtimes"))
            all.AddRange(["--js-runtimes", runtime]);

        using var process = Tools.Start(Tools.YtDlpPath, all);
        // Killed the moment the token is set rather than at the next line of output. yt-dlp can sit for a
        // long time saying nothing - a slow site, a retry, a stalled connection - and a Cancel button that
        // only answers when the program next speaks is a Cancel button that does not work.
        using var abort = token.Register(() => Stop(process));
        var lines = new List<string>();
        var errors = new List<string>();
        // Read on a thread of its own. A program that fills one pipe while nothing drains the other stops
        // there for good, and yt-dlp writes a great deal to both.
        var reading = Task.Run(() =>
        {
            string? line;
            while ((line = process.StandardError.ReadLine()) is not null)
                errors.Add(line.Trim());
        }, CancellationToken.None);
        try
        {
            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                token.ThrowIfCancellationRequested();
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                onLine?.Invoke(trimmed);
                lines.Add(trimmed);
            }
            var span = timeout ?? TimeSpan.FromMinutes(3);
            if (!process.WaitForExit((int)span.TotalMilliseconds))
                throw new TimeoutException($"yt-dlp did not finish within {span.TotalSeconds:F0} seconds.");
            // A killed process exits like any other, so the abort is reported here rather than left to look
            // like a program that failed.
            token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            Stop(process);
            throw;
        }
        finally
        {
            reading.Wait(TimeSpan.FromSeconds(2));
        }
        if (process.ExitCode == 0)
            return new YtDlpRun(lines, string.Empty);
        return new YtDlpRun([], ShortDiagnostic(errors) is { Length: > 0 } complaint
            ? complaint
            : ShortDiagnostic(lines) is { Length: > 0 } fallback
                ? fallback
                : $"yt-dlp exited with code {process.ExitCode}.");
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            // It finished between the test and the kill, which is the outcome that was wanted.
        }
    }

    // ---- reading what it said ----

    private static ResolveOutcome Ready(YouTubeResult item, string address)
        => ResolveOutcome.Ok(new Resolved(
            item.Url.Length > 0 ? item : item with { Url = address },
            address,
            // yt-dlp is asked for one address, not a pair. Its format strings prefer the streams that
            // carry sound and picture together, so there is never a second one to go with it.
            null,
            StreamPicker.ExpiryOf(address)));

    private static bool RateLimited(YtDlpRun run)
        => run.Diagnostic.Contains("HTTP Error 429", StringComparison.OrdinalIgnoreCase)
            || run.Diagnostic.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase);

    private static JsonElement? Parse(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
            return null;
        try
        {
            using var document = JsonDocument.Parse(string.Join('\n', lines));
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not a failure worth its own path: every caller treats "no data" and "unreadable data" the
            // same, and the message they show says which of their own jobs did not finish.
            return null;
        }
    }

    /// <summary>The address to play, out of everything yt-dlp said about a video.</summary>
    private static string? PickStream(JsonElement data)
    {
        if (Text(data, "url") is { Length: > 0 } direct)
            return direct;
        // A format that needs joining is reported as its parts. The first is the one that carries the
        // picture, and the Python player takes it for the same reason: without ffmpeg there is nothing to
        // join them with.
        if (!data.TryGetProperty("requested_formats", out var parts) || parts.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.ValueKind == JsonValueKind.Object && Text(part, "url") is { Length: > 0 } address)
                return address;
        }
        return null;
    }

    /// <summary>What yt-dlp says about a video, falling back to what the caller already knew.</summary>
    private static YouTubeResult Describe(JsonElement data, string watchUrl, YouTubeResult known)
    {
        var found = Entry(data);
        if (found is not YouTubeResult item)
            return known.Url.Length > 0 ? known : known with { Url = watchUrl, Title = watchUrl };
        return known.Url.Length > 0 ? item with { Url = known.Url } : item;
    }

    private static YouTubeResult? Entry(JsonElement entry)
    {
        var url = Text(entry, "webpage_url") ?? string.Empty;
        var id = Text(entry, "id") ?? string.Empty;
        if (url.Length == 0)
        {
            var raw = Text(entry, "url") ?? string.Empty;
            url = raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? raw
                : id.Length > 0
                    ? ExplodeClient.WatchUrl(id)
                    : raw.Length > 0 ? ExplodeClient.WatchUrl(raw) : string.Empty;
        }
        if (url.Length == 0)
            return null;
        // Spelled the one way the player spells them, so an address from here and one from a search name
        // the same cache entry and the same playlist source.
        url = ExplodeClient.Canonical(url) ?? url;
        var duration = entry.TryGetProperty("duration", out var seconds)
            && seconds.ValueKind == JsonValueKind.Number
            && seconds.TryGetDouble(out var value) && value > 0
                ? TimeSpan.FromSeconds(value)
                : (TimeSpan?)null;
        return new YouTubeResult(
            id,
            Text(entry, "title") is { Length: > 0 } title ? title : url,
            Text(entry, "channel") ?? Text(entry, "uploader") ?? string.Empty,
            duration,
            url,
            Text(entry, "channel_url") ?? Text(entry, "uploader_url") ?? string.Empty);
    }

    private static string? Text(JsonElement value, string name)
        => value.TryGetProperty(name, out var found) && found.ValueKind == JsonValueKind.String
            ? found.GetString()?.Trim()
            : null;

    private static string ShortDiagnostic(IReadOnlyList<string> lines)
    {
        var first = lines.FirstOrDefault(line => line.Length > 0)?.Trim() ?? string.Empty;
        return first.Length <= 220 ? first : string.Concat(first.AsSpan(0, 220).TrimEnd(), "...");
    }

    /// <summary>Drops the program's own name from the front of a version string, as it prints it.</summary>
    private static string CleanVersion(string text)
    {
        var line = text.Trim();
        return line.StartsWith("yt-dlp", StringComparison.OrdinalIgnoreCase)
            ? line["yt-dlp".Length..].Trim(' ', ':', '-')
            : line;
    }

    /// <summary>A size as yt-dlp prints it - "12.34MiB", "1.2GB" - in bytes.</summary>
    private static long Bytes(string text)
    {
        var match = Size().Match(text.Trim());
        if (!match.Success)
            return 0;
        var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
        var multiplier = match.Groups["unit"].Value.ToUpperInvariant() switch
        {
            "KB" => 1000L,
            "MB" => 1000L * 1000,
            "GB" => 1000L * 1000 * 1000,
            "TB" => 1000L * 1000 * 1000 * 1000,
            "KIB" => 1024L,
            "MIB" => 1024L * 1024,
            "GIB" => 1024L * 1024 * 1024,
            "TIB" => 1024L * 1024 * 1024 * 1024,
            _ => 1L,
        };
        return (long)(value * multiplier);
    }

    [GeneratedRegex(@"\[download\]\s+(?<pct>\d+(?:\.\d+)?)%\s+of\s+~?\s*(?<size>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex Progress();

    /// <summary>Either line yt-dlp writes naming a file it is about to produce.</summary>
    /// <remarks>
    /// Both, because an audio-only save writes two. The download names the container it fetched - an mp4 -
    /// and the extraction step then names the m4a that is actually left on disk, which is the name the user
    /// is waiting for. Matching only the first reports a file that will not be there at the end.
    /// </remarks>
    [GeneratedRegex(@"^\[(?:download|ExtractAudio)\] Destination:\s*(?<path>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex Destination();

    [GeneratedRegex(@"^(?<value>[0-9]+(?:\.[0-9]+)?)\s*(?<unit>[KMGT]?i?B)$", RegexOptions.IgnoreCase)]
    private static partial Regex Size();
}
