# Spotify crossfade regression checks

Run on Windows from the repository root with .NET 10 and the bundled native libraries present:

```powershell
dotnet run --project tests/SpotifyCrossfade -p:NativeAotBuildInvocation=true -p:PublishAot=false
```

The checks generate encrypted WAV audio in memory and play it through the real mpv engine with its null output. No Spotify account or network access is needed. They cover shared settings, stream reopening and independent cancellation, native transition adoption, immediate prepared navigation, double Next during an overlap, paused navigation, page prefetching, cleanup, and advancement guards. Audio-key checks verify reuse across cancelled selections, bounded retention, disposal, and retry after a real failure.

The transport audit also simulates canonical track IDs, denied keys, an unavailable audio version, a temporary metadata or CDN failure, and an expired HTTP token. Decoder tests check recovery, a permanent failure, pause preservation, and stale errors from replaced or stopped playback.

For an optional live check using the current Windows user's saved Spotify playback sign-in, append `-- --live` to the command. This fetches two tracks, checks their decrypted headers, and exercises prepared Next and uncached Previous through the null audio output. It does not save or change credentials. Screen-reader behavior still requires manual verification.

Use `-- --live --double` to check two Next commands at 0, 20, 80, 200, and 500 milliseconds apart with a three-second overlap. This reproduces the two-Tab navigation path and exercises cancellation of the intervening track's prefetch.

Use `-- --live --stress` for a longer test across 20 distinct catalog search results, including regional tracks, with both rapid skips and completed loads. It reports navigation checkpoints, including deliberate test waits; these are not first-audible-sample timings. `-- --live --probe <track-id>` checks one track on a fresh connection and compares its catalog playability. `-- --live --probe-sequence` tests multiple key requests without the playback engine.

Scheduling checks cover foreground priority, promotion of shared speculative requests, immediate cancellation of abandoned work, independent retry waits, and bounded temporary refusals without quality fallback. Failure ownership checks require the exact exception, selected track, generation, attempt and playback revision before a dialog can be consumed once. Metadata checks verify that shared requests retain independent decoder ownership.

Playback diagnostics are written to `%APPDATA%\Luna Player\spotify-playback.log`, bounded to 256 KiB plus one rotated file. They include executable timestamps, track/file identifiers, request stages and numeric refusals; they omit credentials, access tokens, decryption keys and CDN URLs.
