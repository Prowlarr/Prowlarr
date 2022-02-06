# New Beta Release

Prowlarr v0.2.0.1448 has been released on `develop`

A reminder about the `develop` branch

- **develop - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first. This version will receive updates either weeklyish or bi-weeklyish depending on development.**

# Announcements

- Automated API Documentation Updates recently implemented
- [*Coming Soon* - Newznab & All Indexer Definitions to YML - Cardigann v5](https://github.com/Prowlarr/Prowlarr/pull/823)
- Note that users of Newznab (Usenet) Indexers may see that the UI shows Indexers as added that are not.
  - This will be fixed with Cardigann v5 and is due to all the Newznab Indexers sharing the same definition.
  - https://i.imgur.com/tijCHlk.png

# Additional Commentary

- Lidarr v1 coming to `develop` as beta soon^(tm)
- Readarr official beta on `develop` coming soon^(tm) currently dealing with metadata issues
- [Radarr](https://www.reddit.com/r/radarr/comments/sgrsb3/new_stable_release_master_v4045909/) v4.0.4 released to `master` (stable)
- [Radarr Postgres Database Support coming soon (PR#6873)](https://github.com/radarr/radarr/pull/6873)
- [Lidarr Postgres Database Support in development (Draft PR#2625)](https://github.com/Lidarr/Lidarr/pull/2625)

# Releases

## Native

- [GitHub Releases](https://github.com/Prowlarr/Prowlarr/releases)

- [Wiki Installation Instructions](https://wiki.servarr.com/prowlarr/installation)

## Docker

- [hotio/Prowlarr:testing](https://hotio.dev/containers/prowlarr)

- [lscr.io/linuxserver/Prowlarr:develop](https://docs.linuxserver.io/images/docker-prowlarr)

------------

# Release Notes

## v0.2.0.1448 (changes since v0.2.0.1426)

 - Sync Indexers on app start, go to http if not sync'd yet

 - Misc definition handling improvements

 - Fixed: Updated ruTorrent stopped state helptext

 - Fixed: Added missing translate for Database

 - Fixed: Download limit check was using the query limit instead of the grab limit.

- Other bug fixes and improvements, see github history
