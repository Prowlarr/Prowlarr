# New Beta Release

Prowlarr v0.4.3.1921 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`**

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.

# Announcements

- [Prowlarr Cardigann Definitions Schema Versions and Validations created](https://github.com/Prowlarr/indexers#schemas)
- [*Coming Soon* - Newznab & All Indexer Definitions to YML - Cardigann v8](https://github.com/Prowlarr/Prowlarr/pull/823)
- Note that users of Newznab (Usenet) Indexers may see that the UI shows Indexers as added that are not.
  - This will be fixed with Cardigann v8 and is due to all the Newznab Indexers sharing the same definition.
  - https://i.imgur.com/tijCHlk.png
 

# Additional Commentary

- [Radarr Develop recently released](https://www.reddit.com/r/radarr/comments/w3kik4/new_release_develop_v4206438/)
- [Lidarr](https://lidarr.audio/donate), [Prowlarr](https://prowlarr.com/donate), [Radarr](https://radarr.video/donate), [Readarr](https://readarr.com/donate) now accept direct bitcoin donations
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

## v0.4.3.1921 (changes since v0.4.2.1879)

 - Fixed: (GazelleGames) Use API instead of scraping

 - Translated using Weblate (Hungarian)

 - Automated API Docs update

 - New: Search by DoubanId

 - Fixed: UI Typos (#1072)

 - Translated using Weblate (Chinese (Traditional) (zh_TW))

 - Update README.md

 - Automated API Docs update

 - Debounce analytics service

 - Fixed: Set Download and Upload Factors from Generic Torznab

 - Translated using Weblate (Portuguese (Brazil))

 - Translation Improvements

 - Cleanup Language and Localization code

 - Added translation using Weblate (Lithuanian)

 - Fixed: BeyondHD using improperly cased Content-Type header

 - Fix NullRef in Cloudflare detection service

 - New: (AvistaZ) Parse Languages and Subs, pass in response

 - Rework Cloudflare Protection Detection

 - New: (FlareSolverr) DDOS Guard Support

 - Bump Mailkit to 3.3.0 (#1054)

 - New: Add linux-x86 builds

 - Remove unused XmlRPC dependency

 - Fixed: (Cardigann) Use Indexer Encoding for Form Parameters

 - Fixed: (Cardigann) Use Session Cookie when making SimpleCaptchaCall

 - Fixed: Delete CustomFilters not handled properly

 - Modern HTTP Client (#685)

 - Bump version to 0.4.3

 - Other bug fixes and improvements, see GitHub history
