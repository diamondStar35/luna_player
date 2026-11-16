using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MpvNet;

namespace LunaPlayer.Media.Discs;

/// <summary>Disc inspection owns a silent mpv instance and runs entirely on a worker.</summary>
internal static class Catalog
{
    internal static DiscInfo Read(string path, DiscFormat format, CancellationToken cancellationToken)
    {
        var directory = Resolve(path, format);
        var manager = Path.Combine(directory, format == DiscFormat.Audio ? "AUDIO_TS.IFO" : "VIDEO_TS.IFO");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identity = Fingerprint(manager, cancellationToken);
            using var mpv = new MPV(options: new Dictionary<string, object?>
            {
                ["vo"] = "null", ["ao"] = "null", ["pause"] = true, ["idle"] = "yes",
                ["terminal"] = false, ["keep_open"] = "yes", ["disc_menu"] = false,
                ["media_controls"] = false, ["sub_auto"] = "no", ["sid"] = "no",
            }, optionalOptions: new Dictionary<string, object?> { ["load_scripts"] = false, ["osc"] = false });
            var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var complete = mpv.OnEvent(_ => ready.TrySetResult(true), MpvEventId.FileLoaded);
            using var failed = mpv.OnEvent(_ => ready.TrySetResult(false), MpvEventId.EndFile);
            mpv.LoadFileAsync(Addressing.Build(format, directory));
            if (!ready.Task.Wait(TimeSpan.FromSeconds(20), cancellationToken) || !ready.Task.GetAwaiter().GetResult())
                throw new DiscException(Failure.Unreadable);
            cancellationToken.ThrowIfCancellationRequested();
            var titles = ReadTitles(mpv.GetProperty("edition-list"));
            if (titles.Count == 0)
                throw new DiscException(format == DiscFormat.Audio ? Failure.NoAudioTitles : Failure.NoVideoTitles);
            if (format == DiscFormat.Audio && !Probe.Tracks(mpv.GetProperty("track-list") as IEnumerable<object?> ?? []).HasAudio)
                throw new DiscException(Failure.NoAudioTitles);
            var selected = Convert.ToInt32(mpv.GetProperty("current-edition"), CultureInfo.InvariantCulture);
            if (!titles.Any(title => title.Index == selected)) selected = titles[0].Index;
            var root = Path.GetPathRoot(directory);
            var drive = string.IsNullOrEmpty(root) ? null : new DriveInfo(root);
            var name = drive?.DriveType == DriveType.CDRom ? drive.VolumeLabel : string.Empty;
            if (string.IsNullOrWhiteSpace(name)) name = Path.GetFileName(Path.GetDirectoryName(directory));
            return new DiscInfo(directory, format, identity, name ?? string.Empty, titles, selected);
        }
        catch (Exception exception) when (exception is MpvException or IOException or UnauthorizedAccessException
            or InvalidOperationException or FormatException or OverflowException)
        {
            throw new DiscException(Failure.Unreadable, exception.Message);
        }
    }

    internal static string Resolve(string path, DiscFormat format)
    {
        try
        {
            var location = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var zone = format == DiscFormat.Audio ? "AUDIO_TS" : "VIDEO_TS";
            var directory = Path.GetFileName(location).Equals(zone, StringComparison.OrdinalIgnoreCase)
                ? location : Path.Combine(location, zone);
            var manager = Path.Combine(directory, zone + ".IFO");
            if (!File.Exists(manager))
            {
                if (format == DiscFormat.Audio && File.Exists(Path.Combine(location, "VIDEO_TS", "VIDEO_TS.IFO")))
                    throw new DiscException(Failure.NoAudioTitles);
                throw new DiscException(Failure.InvalidDisc);
            }
            using var stream = File.OpenRead(manager);
            Span<byte> header = stackalloc byte[12];
            var expected = format == DiscFormat.Audio ? "DVDAUDIO-AMG" : "DVDVIDEO-VMG";
            if (stream.Read(header) != header.Length || !Encoding.ASCII.GetString(header).StartsWith(expected, StringComparison.Ordinal))
                throw new DiscException(Failure.InvalidDisc);
            return directory;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException)
        {
            throw new DiscException(Failure.Unreadable, exception.Message);
        }
    }

    internal static IReadOnlyList<Title> ReadTitles(object? value)
    {
        var titles = new List<Title>();
        if (value is not IEnumerable<object?> entries) return titles;
        foreach (var entry in entries)
        {
            if (entry is not IDictionary<string, object?> fields) continue;
            // mpv adds this synthetic entry to DVD editions even when disc-menu is disabled.
            if (fields.TryGetValue("title", out var label) && label as string == "Disc Menu") continue;
            if (fields.TryGetValue("id", out var id) && id is not null)
            {
                var index = Convert.ToInt32(id, CultureInfo.InvariantCulture);
                if (index >= 0 && !titles.Any(title => title.Index == index)) titles.Add(new Title(index));
            }
        }
        return titles;
    }

    private static string Fingerprint(string path, CancellationToken token)
    {
        using var stream = File.OpenRead(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[16384];
        int read;
        while ((read = stream.Read(buffer)) > 0) { token.ThrowIfCancellationRequested(); hash.AppendData(buffer, 0, read); }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
