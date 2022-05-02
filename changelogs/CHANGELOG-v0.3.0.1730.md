# New Beta Release

Prowlarr v0.3.0.1730 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- Automated API Documentation Updates recently implemented
- [*Coming Soon* - Better \*Arr App  Sync](https://github.com/Prowlarr/Prowlarr/pull/983)
- [*Coming Soon* - Newznab & All Indexer Definitions to YML - Cardigann v6](https://github.com/Prowlarr/Prowlarr/pull/823)
- Note that users of Newznab (Usenet) Indexers may see that the UI shows Indexers as added that are not.
  - This will be fixed with Cardigann v6 and is due to all the Newznab Indexers sharing the same definition.
  - https://i.imgur.com/tijCHlk.png

# Additional Commentary

- Lidarr v1 coming to `develop` as beta soon^(tm)
- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
- [Readarr official beta on `develop` announced](https://www.reddit.com/r/Readarr/comments/sxvj8y/new_beta_release_develop_v0101248/)
- Radarr Postgres Database Support in `nightly`
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)

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

## v0.3.0.1730 (changes since v0.3.0.1724)

 - Fixed: Prevent endless loop when calling IndexerUrls for Torznab

 - Deleted translation using Weblate (Chinese (Min Nan))

 - Fix some translations

 - Other bug fixes and improvements, see GitHub history

## v0.3.0.1724 (changes since v0.2.0.1678)

- Fixed: Prevent endless loop when calling IndexerUrls for Newznab ( #982 )

- Fixed: Default List for Cardigann LegacyLinks

- New: Auto map known legacy BaseUrls for non-Cardigann

- Fixed: (BTN) Move to HTTPS ( #979 )

- Typo for myanonamouse.

- Fixed: (MoreThanTV) Better Response Cleansing ( #928 )

- New: SceneHD Indexer

- Fixed: (MaM) Handle Auth Errors & Session Expiry

- Fixed: Remove Indexer if categories were changed to not include in sync ( #912 )

- Fixed: Sync Indexers on App Edit

- Cleanup Config Values ( #894 )

- Fixed: (Cardigann) Handle json field selector that returns arrays ( #950 )

- New: Schedule refresh and process monitored download tasks at high priority

- Centralise image choice, update to latest images

- Don't return early after re-running checks after startup grace period ( #7147 )

- Fixed: Delay health check notifications on startup

- New: Add date picker for custom filter dates

- Bump Monotorrent to 2.0.5

- Remove old DotNetVersion method and dep

- New: Add backup size information ( #957 )

- Fixed: (BeyondHD) Use TryCoerceInt for tmdbId ( #960 )

- Fixed: (TorrentDay) TV Search returning Series not S/E Results ( #816 )

- Fixed: (CinemaZ and ExoticaZ) Better Log Cleansing

- Fixed: (exoticaz) Category Parsing

- Fixed: (Indexer) HDTorrents search imdbid + season/episode

- Bump version to 0.3.0

 - Other bug fixes and improvements, see GitHub history