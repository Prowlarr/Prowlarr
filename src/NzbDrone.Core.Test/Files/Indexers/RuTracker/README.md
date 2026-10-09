# RuTracker fixtures

`RuTrackerTitleParserFixture.cs` contains 27 inline test cases with sanitized source titles selected from 96 native RuTracker
source/output pairs collected on 2026-10-02 UTC with Prowlarr v2.5.2.5491.
The source was the native adapter's Torznab `description`, not a reconstructed
season title. Category IDs are retained.
There are no cookies, credentials, account details, torrent files or download URLs.

The samples cover Chainsmoker Cat, plain TV packs, sequels/cours, partial/ongoing
releases, split audio coverage, TV+Special, OVA, ONA, specials, anime movies,
ordinary TV and ordinary films. JPN, CHI and KOR grammar cases in the C# tests
are explicitly synthetic variants; the real selected titles use JAP or CHI.

`search.html` is a reconstructed minimal tracker table with the real Chainsmoker
Cat title, size and seeder count. Topic/download IDs are synthetic `1`; requests in
the tests use the reserved `.invalid` domain. It is not a saved authenticated page.
The parser tests never issue a network request.

These fixtures do not assert Sonarr series identity, full-season completeness,
TVDB numbering, actual audio tracks or import eligibility. No catalog is embedded.
