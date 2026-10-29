## Merge blocker: repository owner must configure Luna's Discord application

**Do not mark this PR ready, merge it, or release it until the repository owner pushes a follow-up commit
setting `PublisherApplication.ApplicationId` in `src/Discord/PublisherApplication.cs` to the public
Application ID of their own Discord application, then verifies the configured build with Discord desktop.**

The application ID is intentionally blank in the initial feature commit. Discord Rich Presence will not
work until it is committed and Luna is rebuilt. The rest of the player continues to work normally.

This needs a **public Application ID**, not an API key, bot token, client secret, account token, or public
key. Do not commit any secret. End users must not supply developer credentials or create their own apps.

## What is new

- Native C# Discord IPC integration: no Python runtime, Discord bot, SDK, or extra native dependency.
- Listening/watching activity, titles, artists/YouTube channel names, pause/resume, seek- and speed-aware
  timing, track changes, idle behavior, and cleanup on disable or exit.
- YouTube thumbnails, canonical YouTube/Spotify media buttons, and a Luna download button. Raw stream
  addresses, signed URLs, IPTV credentials, and local paths are not used as buttons.
- Background reconnection, bounded I/O, continuous ping/pong handling, nonce matching, and coalesced updates.
- Opt-in Preferences page with keyboard-accessible controls, screen-reader labels, and F1 help. Hiding
  details also hides media artwork and media links. Windows media controls remain independent.
- One publisher-owned application ID in code. No per-user ID field or persisted ID; an unconfigured build
  disables the enable control, explains why, and never connects to Discord.
- Automated payload, IPC, lifecycle, configuration, and playback-mapping tests, plus documentation and
  translation-template updates. Stale nested `bin`/`obj` outputs are excluded from test-source discovery
  so old builds do not cause duplicate assembly attributes. The foreign Python reference is not shipped or committed.

## Repository owner: required setup

1. Sign in to the [Discord Developer Portal](https://discord.com/developers/applications) and create an
   application named **Luna Player** under your account or team.
2. Copy **Application ID** from **General Information**. This is public; it is not an API key or secret.
3. Replace the empty `PublisherApplication.ApplicationId` constant in `src/Discord/PublisherApplication.cs`
   with that actual numeric ID. Do not reuse HexPlayer's ID or a synthetic test ID.
4. Optionally upload a PNG of Luna's icon in **Rich Presence assets**, with asset key `luna`.
5. **Push a follow-up commit containing your Application ID onto this PR's branch before merging.**
   Maintainer edits are enabled; if you cannot push to the fork branch, coordinate with the contributor.
6. Rebuild, run the full managed and Native AOT tests, enable Discord sharing in both applications, and
   verify that the real Discord profile displays Luna correctly.

See `docs/discord-maintainer-setup.md` in this PR for the full maintainer setup and verification checklist.

## Verification

<!-- Replace this section with the exact commands/results before submitting or updating the PR. -->

- [ ] Full managed tests pass on this commit.
- [ ] Full Native AOT test runner passes on this commit.
- [ ] Translation-comment validation and translation-template generation pass.
- [ ] Repository owner has pushed the real Application ID in a follow-up commit.
- [ ] Live Discord profile, reconnect, privacy, timing, and exit behavior verified with that ID.
- [ ] Keyboard navigation, screen-reader announcements, F1 help, and persisted preferences checked manually.

Automated tests use simulated Discord named-pipe endpoints; they do not prove that a real application ID
is configured or valid. Document any manual checks not performed. Keep this PR in draft until the owner's
configuration commit and live verification are complete.
