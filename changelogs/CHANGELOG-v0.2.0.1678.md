# New Beta Release

Prowlarr v0.2.0.1678 has been released on `develop`

- **Users who do not wish to be on the alpha `nightly` testing branch should take advantage of this parity and switch to `develop`

A reminder about the `develop` and `nightly` branches

- **develop** - Current Develop/Beta - (Beta): This is the testing edge. Released after tested in nightly to ensure no immediate issues. New features and bug fixes released here first after nightly. It can be considered semi-stable, but is still beta.**
- **nightly** - Current Nightly/Unstable - (Alpha/Unstable) : This is the bleeding edge. It is released as soon as code is committed and passes all automated tests. This build may have not been used by us or other users yet. There is no guarantee that it will even run in some cases. This branch is only recommended for advanced users. Issues and self investigation are expected in this branch. Use this branch only if you know what you are doing and are willing to get your hands dirty to recover a failed update. This version is updated immediately.**

# Announcements

- Automated API Documentation Updates recently implemented
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

## v0.2.0.1678 (changes since v0.2.0.1448)

 - Bump moment from 2.29.1 to 2.29.2

 - #834 #256 fix for unable to load the Indexes page

 - Fix .editorconfig to disallow `this`

 - New: MyAnonamouse freeleech support

 - Fixed: (BHD) TMDb Parsing Exception

 - Fixed: MoreThanTV indexer from browse page layout changes (#922)

 - We don't have two Radarrs

 - Fix indent from 37c393a659

 - Fixed: (HDBits) Treat 403 as Query Limit

 - Fixed: (PTP) Treat 403 as Query Limit

 - New: (BTN) Rate Limit to 1 Query per 5 Seconds

 - Fixed: (BTN) Handle Query Limit Error

 - New: (Lidarr/Radarr/Readarr/Sonarr) Improved Errors

 - Fixed: Loading old commands from database

 - Fixed: Cleanup Temp files after backup creation

 - Add Support

 - Translated using Weblate (Finnish)

 - Fixed: Indexer Infobox Error (#920)

 - New: Indexer Description in Add Indexer Modal

 - Fixed: Missing Translates

 - New: Add Search Capabilities to Indexer API & InfoBox

 - Fixed: Update from version in logs

 - Automated API Docs update

 - Translated using Weblate (Chinese (Simplified) (zh_CN))

 - Translated using Weblate (Portuguese (Brazil))

 - Fixed: Validation when testing indexers, connections and download clients

 - Fixed: Prevent delete of last profile

 - New: Load more (page) results on Search UI

 - Update webpack packages

 - Frontend Package Updates

 - Backend Package Updates

 - Bump dotnet to 6.0.3

 - Translated using Weblate (Spanish)

 - Fixed: (Gazelle) Replace Periods for Space in Search Term

 - Fixed: (HDSpace) Replace Periods for Space in Search Term

 - Fixed: (Anthelion) Replace Periods for Space in Search Term

 - Fixed: (Redacted) Map Categories Comedy & E-Learning Videos to 'Other'

 - Fixed: No longer require first run as admin on windows (#885)

 - Translated using Weblate (Chinese (Simplified) (zh_CN))

 - indexer(xthor): moved to YAML definition v5

 - Fixed: '/indexers' URL Base breaking UI navigation

 - Translated using Weblate (French)

 - Fix app settings delete modal not closing and reloading app profiles

 - Translated using Weblate (French)

 - Bump Swashbuckle to 6.3.0

 - Translated using Weblate (Portuguese (Brazil))

 - fixup! New: (DanishBytes) Move to YML

 - New: (DanishBytes) Move to YML

 - Update translation files

 - New: (RuTracker.org) add .bet mirror (#876)

 - Fixed:(pornolab) language formatting

 - New: Housekeeper for ApplicationStatus

 - Fixed: Cleanse Tracker api_token from logs

 - New: (HDTorrents) Add hd-torrents.org as Url option

 - New: (Cardigann) Allow JSON filters

 - Fixed: Convert List<HistoryEventTypes> to Int before passing to DB

 - Fixed: WhereBuilder for Postgres

 - Translated using Weblate (Finnish)

 - Fixed: Make authentication cookie name unique to Prowlarr

 - Update Categories

 - Fixed: Enable response compression over https

 - Fixed: (RuTracker) Update Cats

 - Fixed: Clarify App Sync Settings (#847)

 - Set version header to X-Application-Version (missing hyphen)

 - Go to http if def exists on def server

 - Fixed: (BHD) Handle API Auth Errors

 - Fixed: (Immortalseed) Keywordless Search

 - Fixed: (Cardigann) TraktId was mapping to TvRageId

 - New: (Cardigann) - Cardigann v4 Support for Genre, Year, and TraktID

 - New: (Cardigann) - Cardigann v4 Support for categorydesc

 - New: (Cardigann) - Cardigann v4 Add Support for MapTrackerCatDescToNewznab

 - New: (Cardigann) - Cardigann v4 Improved Search Logging

 - Fixed: Corrected Query Limit and Grab Limit HelpText

 - New: (Avistaz) Better error reporting for unauthorized tests

 - Fixed: (Cardigann) Requests Failing for Definitions without LegacyLinks

 - Bump SharpZipLib from 1.3.1 to 1.3.3 in /src/NzbDrone.Common

 - Fixed: (Cardigann) Smarter redirect domain compare

 - Fixed: (Cardigann) Treat "Refresh" header as redirect

 - Fixed: (Cardigann) Replace legacy links with default link when making requests

 - Other bug fixes and improvements, see GitHub history
