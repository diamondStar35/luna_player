using System.Globalization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Spotify.Playback;

/// <summary>Bounded playback diagnostics. Never records tokens, credentials, keys, or CDN URLs.</summary>
internal static class Diagnostics
{
    private const long MaximumBytes = 256 * 1024;
    private static readonly object Sync = new();
    private static bool _started;
    internal static string FilePath => Path.Combine(Paths.RootDirectory, "spotify-playback.log");

    internal static void Write(string stage, string? track = null, string? detail = null, Exception? error = null)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Paths.RootDirectory);
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length >= MaximumBytes)
                    File.Move(FilePath, FilePath + ".previous", overwrite: true);
                var text = string.Empty;
                if (!_started)
                {
                    _started = true;
                    var executable = Environment.ProcessPath;
                    text = $"process={executable} built={File.GetLastWriteTimeUtc(executable!):O}{Environment.NewLine}";
                }
                text += DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + " " + stage;
                if (track is not null) text += " track=" + track;
                if (detail is not null) text += " " + detail;
                for (var cause = error; cause is not null; cause = cause.InnerException)
                {
                    text += " cause=" + cause.GetType().Name;
                    if (cause is AudioKeyUnavailableException key) text += " keyCode=" + key.Code;
                    if (cause is HttpRequestException http) text += " http=" + (int?)http.StatusCode;
                }
                File.AppendAllText(FilePath, text + Environment.NewLine);
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Diagnostics must never interrupt playback.
        }
    }
}
