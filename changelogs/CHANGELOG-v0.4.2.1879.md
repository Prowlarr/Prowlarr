# New Beta Release

Prowlarr v0.4.2.1879 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- [Prowlarr Cardigann Definitions Schema Versions and Validations created](https://github.com/Prowlarr/indexers#schemas)
- [*Coming Soon* - Newznab & All Indexer Definitions to YML - Cardigann v7](https://github.com/Prowlarr/Prowlarr/pull/823)
- Note that users of Newznab (Usenet) Indexers may see that the UI shows Indexers as added that are not.
  - This will be fixed with Cardigann v6 and is due to all the Newznab Indexers sharing the same definition.
  - https://i.imgur.com/tijCHlk.png
 

# Additional Commentary

- [Lidarr v1 coming to `master` as recently released](https://www.reddit.com/r/Lidarr/comments/v5fdhi/new_stable_release_master_v1022592/)
- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
- [Readarr official beta on `develop` announced](https://www.reddit.com/r/Readarr/comments/sxvj8y/new_beta_release_develop_v0101248/)
- Radarr Postgres Database Support in `nightly` and `develop`
- Prowlarr Postgres Database Support in `nightly` and `develop`
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)
- \*Arrs Wiki Contributions welcomed and strongly encouraged, simply auth with GitHub on the wiki and update the page

# Releases

## Native

- [GitHub Releases](https://github.com/Prowlarr/Prowlarr/releases)

- [Wiki Installation Instructions](https://wiki.servarr.com/prowlarr/installation)

## Docker

- [hotio/Prowlarr:testing](https://hotio.dev/containers/prowlarr)

- [lscr.io/linuxserver/Prowlarr:develop](https://docs.linuxserver.io/images/docker-prowlarr)

## NAS Packages

- Synology - Please ask the SynoCommunity to update the base package; however, you can update in-app normally

- QNAP - Please ask the QNAP to update the base package; however, you should be able to update in-app normally

------------

# Release Notes

## v0.4.2.1879 (changes since v0.3.0.1730)

 - Don't require user agent for IPTorrents

 - Fixed: (Applications) ApiPath can be null from -arr in some cases

 - ProtectionService Test Fixture

 - Fixed: Lidarr null ref when building indexer for sync

 - Fixed: Lidarr null ref when building indexer for sync

 - Double MultipartBodyLengthLimit for Backup Restore to 256MB

 - Fixed: (IPTorrents) Allow UA override for CF

 - Fixed: Log Cleanse Indexer Response Logic and Test Cases

 - Fixed: Set update executable permissions correctly

 - Fixed: Don't call for server notifications on event driven check

 - Update file and folder handling methods from Radarr (#1051)

 - Running Integration Tests against Postgres Database (#838)

 - Updated NLog Version (#7365)

 - Add additional link logging to DownloadService

 - Fixed: Correctly remove TorrentParadiseMl

 - V6 Cardigann Changes (#1045)

 - Sliding expiration for auth cookie and a little clean up

 - Bump version to 0.4.2

 - Update Sentry to 3.18.0

 - Update Swashbuckle to 6.3.1

 - Bump dotnet to 6.0.6

 - Update AngleSharp to 0.17.0

 - Remove ShowRSS C# Implementation

 - Swallow HTTP issues on analytics call

 - Fix NullRef in analytics service

 - Bump version to 0.4.1

 - Fix Donation Links

 - Fix Tooltips in Dark Theme

 - Fixed: (AnimeBytes) Cleanse Passkey from response

 - Fixed: (Cardigann) Use variables in keywordsfilters block

 - New: (BeyondHD) Better status messages for failures

 - Fixed: VIP Healthcheck not triggered for expired indexers

 - Use DryIoc for Automoqer, drop Unity dependency

 - New: Send description element in nab response

 - (Filelist) Update help text for pass key (#1039)

 - Fixed: (Exoticaz) Category parsing kills search/feed

 - New: (PassThePopcorn) Freeleech only option

 - Fixed: (Cardigann) Searching with nab Parent should also use Child categories

 - Fixed: Better Cleansing of Tracker Announce Keys

 - Automated API Docs update

 - Update FE dev dependencies

 - Ensure .Mono and .Windows projects have all dependencies in build output

 - Fixed: (Gazelle) Parse grouptime as long or date

 - Fixed: (ExoticaZ) Category Parsing

 - Fixed: Input options background color on mobile

 - Fixed: Update AltHub API URL (#1010)

 - Automated API Docs update

 - New: Dark Theme

 - New: Move to CSS Variables for Colorings

 - New: Native Theme Engine

 - diversify chartcolors for doughnut & stackedbar

 - Translated using Weblate (Chinese (Simplified) (zh_CN))

 - Catch Postgres log connection errors

 - Clean lingering Postgres Connections on Close

 - New: Instance name in System/Status API endpoint

 - New: Instance name for Page Title

 - New: Instance Name used for Syslog

 - New: Set Instance Name

 - Fixed: Use separate guid for download protection

 - Fixed: (RuTracker) Support Raw search from apps

 - Fixed: Localization for two part language dialects

 - Fixed: (AnimeBytes) Handle series synonyms with commas (#984)

 - New: Add Lidarr and Readarr DiscographySeedTime Sync

 - New: Add Sonarr SeasonSeedTime Sync

 - Fixed: Indexer Tags Helptext

 - Automated API Docs update

 - New: Seed Settings Sync

 - New: Only sync indexers with matching app tags

 - Indexer Cleanup

 - Bump version to 0.4.0

 - Bump version to 0.3.1

 - Translated using Weblate (Chinese (Simplified) (zh_CN))

 - Fixed: Correct User-Agent api logging

 - Other bug fixes and improvements, see GitHub history
