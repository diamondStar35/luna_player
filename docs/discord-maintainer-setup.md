# Discord Rich Presence: required maintainer setup

## Do not merge or release before configuring the application ID

**The repository owner must push a follow-up commit that sets
`PublisherApplication.ApplicationId` in `src/Discord/PublisherApplication.cs` to the public Application ID
of their own Discord application, then test the configured build before merging this feature.**

The value is intentionally empty in the initial feature commit. Rich Presence cannot work until this
configuration is committed and the player is rebuilt. The rest of Luna still works. Keep the pull request
in draft until the setup and live checks below are complete.

## What credential is required?

Only a **public Discord Application ID** is required. It is safe and necessary to include this public
identifier in the source and compiled application. It identifies Luna to the local Discord desktop app.

This integration does **not** need an API key, bot token, client secret, account token, or public key.
Do not generate a bot for this feature, and never paste any secret into the repository, pull request,
source constant, or user settings. End users should not create developer applications or provide credentials.

## Repository owner: configure Luna once

1. Sign in to the [Discord Developer Portal applications page](https://discord.com/developers/applications)
   with the account or team that should own Luna's integration.
2. Create an application named **Luna Player**. Discord derives the displayed application name from this
   application, so do not reuse another player's ID.
3. In **General Information**, copy **Application ID**. Do not copy the Public Key or a secret.
4. In `src/Discord/PublisherApplication.cs`, replace only the empty string:

   ```csharp
   internal const string ApplicationId = "";
   ```

   Put the real numeric Application ID between those quotes. Do not use a fabricated example ID. Local
   validation checks the numeric format, not whether the application exists or belongs to Luna.
5. Optionally upload a PNG of Luna's icon under the application's **Rich Presence assets**, using the key
   `luna`. Allow Discord time to process it. Public YouTube media can use its thumbnail instead.
6. **Push a separate follow-up commit with the real public Application ID to this PR's branch.**
   Maintainer edits should remain enabled on the fork PR. If GitHub cannot edit the contributor branch,
   coordinate the commit with the contributor; do not merge the unconfigured feature as a workaround.
7. Rebuild Luna, run the managed and Native AOT tests, and complete the live checks below.
8. Only after the ID commit is present and the checks pass, mark the PR ready for review and consider merging.

The ID is fixed for each build. Changing it requires a new build. It is not stored in user preferences,
settings exports, environment variables, or per-user configuration files. Old per-user `discord.clientId`
settings from development builds are ignored and are not written back.

## Required verification before merge

- [ ] The owner has pushed a follow-up commit with Luna's real public application ID.
- [ ] No API key, bot token, client secret, account token, or unrelated application's ID has been committed.
- [ ] The full managed test suite and Native AOT test runner pass on the configured commit.
- [ ] With Discord desktop running, enable **Preferences > Discord > Enable Discord Rich Presence**.
- [ ] Confirm the profile displays **Luna Player**, correct media details, and listening/watching status.
- [ ] Check play, pause, resume, seek, speed changes, track changes, idle behavior, and application exit.
- [ ] Check YouTube thumbnails/channel names and public media buttons from another Discord account if necessary.
- [ ] Check that hidden details also remove media artwork and media buttons, and that raw/private stream
      addresses and local file paths are not used as buttons.
- [ ] Start Discord after Luna, restart Discord, and confirm automatic reconnection without playback disruption.
- [ ] Verify keyboard navigation, screen-reader labels, F1 help, and preference persistence after restart.

Automated IPC tests use isolated local named-pipe servers and a synthetic test ID. Their passing does not
validate a real Discord application or replace the owner's live-client check.
