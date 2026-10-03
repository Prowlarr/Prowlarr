# RuTracker fixtures

`titles.json` contains 27 sanitized source titles selected from 96 native RuTracker
source/output pairs collected on 2026-10-02 UTC with Prowlarr v2.5.2.5491.
The source was the native adapter's Torznab `description`, not a reconstructed
season title. Topic IDs are public fixture identifiers; category IDs are retained.
There are no cookies, credentials, account details, torrent files or download URLs.

- `source`: original tracker title, unchanged.
- `expected`: intended parser output with Cyrillic stripping/tag movement disabled
  and `addRussianToTitle` enabled. Non-anime expectations are the captured baseline;
  anime expectations preserve counts/TV ordinals, translate explicit Japanese audio,
  and do not append an unsupported Russian audio claim.
- `kind`: descriptive corpus label, not a trusted series type or catalog decision.
  In particular `plain_tv_full` is only a count-complete title form; it includes
  sequels and incomplete alternative-language tracks.

The samples cover Chainsmoker Cat, plain TV packs, sequels/cours, partial/ongoing
releases, split audio coverage, TV+Special, OVA, ONA, specials, anime movies,
ordinary TV and ordinary films. JPN and adversarial grammar cases in the C# tests
are explicitly synthetic variants; the real selected titles use JAP.

`search.html` is a reconstructed minimal tracker table with the real Chainsmoker
Cat title, size and seeder count. Topic/download IDs are synthetic `1`; requests in
the tests use the reserved `.invalid` domain. It is not a saved authenticated page.
The parser tests never issue a network request.

These fixtures do not assert Sonarr series identity, full-season completeness,
TVDB numbering, actual audio tracks or import eligibility. No catalog is embedded.
