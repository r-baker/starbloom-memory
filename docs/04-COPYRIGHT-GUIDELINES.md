# Copyright & Legal Guidelines for This Mod

**Not legal advice.** This is a practical working reference for keeping
the project inside normal, well-established fan-project norms — not a
substitute for an actual lawyer if this ever grows into something with
real commercial stakes.

## The core model this project relies on

This is a **free, non-commercial fan mod**: original creative/narrative
work using the Gundam UC setting and characters, built as data/code that
modifies a game you and each user already own, distributed for free.
Every rule below exists to keep it inside that model. If any of these
change (charging money, distributing copyrighted assets directly, claiming
official status), the legal picture changes with it — revisit this doc
before doing any of those things.

## Do

- **Write original narrative content set in the UC universe**, including
  using existing characters (Char, Ramba Ral, etc.) in new, non-reproduced
  scenarios — this is standard fan fiction, broadly accepted practice.
  Branching outcomes (Char dying and staying dead, alternate battle
  results) are original creative extrapolation, not reproduction.
- **Build original stat conversions** (mech-to-'Mech translations, weapon
  stats, movement profiles) — these are your own mechanical
  interpretations, not transcriptions of an official rulebook.
- **Write original 3D models from reference**, the same way any fan artist
  draws a character from reference — an original interpretation, not a
  scan or direct copy of official model sheets/render assets.
- **Clearly label the mod as an unofficial, non-commercial fan project**
  with no affiliation to Sunrise, Sotsu, Bandai Namco, or Harebrained
  Schemes/Paradox. A short disclaimer in the mod's description/README is
  standard practice across this entire modding scene.
- **Distribute for free only.** No sale price, no paywall, no exclusive
  "supporter" tiers that gate the mod itself.
- **Require the user already own BattleTech.** The mod modifies files a
  person already legally owns — same model every other BattleTech mod
  (RogueTech, BEX, etc.) operates under.

## Don't

- **Don't reproduce verbatim dialogue, narration, or manga/script text**
  beyond a short, clearly-attributed phrase here or there. Full scenes or
  lines lifted directly from the source material cross the line fan
  fiction generally stays behind.
- **Don't reproduce official artwork, renders, or model sheets directly**
  (scanning, tracing, or using an AI tool to closely reproduce a specific
  official image). Original work inspired by the designs is fine;
  reproduction of the designs themselves is not.
- **Don't include copyrighted music, voice clips, or sound effects**
  ripped from the anime/games. Original or licensed-for-use audio only if
  sound gets added later.
- **Don't monetize the mod** — no ads embedded in it, no Patreon-exclusive
  builds, no selling it as a bundle. This is the single biggest thing that
  changes a project's legal risk profile; free fan projects get treated
  very differently than anything with money attached.
- **Don't imply official endorsement** — no claiming Sunrise/Bandai/Sotsu
  or Anthropic-affiliated status, no using official logos as if
  officially licensed.
- **Don't redistribute BattleTech's own game files** as part of the mod
  download — the mod should be data/JSON/code that modifies a copy of the
  game the user already owns, not a bundle containing the base game's
  copyrighted assets.
- **Don't include the `dev-reference/` folder in any release build.** This
  folder is a private compiled research reference (wiki-sourced specs for
  MS/ships/weapons/equipment) used to speed up our own original stat
  conversion work — it's a dev tool, not distributable content. Confirm
  it's explicitly excluded from any packaging script or installer file
  list before ever building a release. See `dev-reference/README.md` for
  the full rule.
- **`reference-mod/` (other BattleTech mods kept locally for study — BEX,
  RogueTech) now lives at the BattleTech root, NOT under `Mods/`** — moved
  there 2026-08-20 after it caused ModTek to actually load both mods as
  active content (see `03-TECHNICAL-NOTES.md`). This also resolves the
  packaging concern structurally: a release build only ever bundles
  `Mods/GundamUC-Units/`, so `reference-mod/` living outside `Mods/`
  entirely can't accidentally end up in a package. Same underlying
  courtesy principle still applies if it's ever moved back for any
  reason: it's third-party modders' work, not ours to redistribute.

## Using code/patterns from other BattleTech mods (BEX, RogueTech, etc.)

- **Default mode: study technique, don't lift content.** This matches the
  existing rule in `03-TECHNICAL-NOTES.md` — read how another mod solves
  a problem (custom mission framework structure, CAB asset bundle wiring,
  DLC-content interaction handling) and apply the *technique* to our own
  original content. This is standard, expected practice in the BattleTech
  modding community.
- **If we do end up using part of their actual code or JSON** (not just
  the technique, but an adapted/reused chunk) — **credit them for it.**
  Concretely: note the source mod and what was adapted in a CREDITS
  section (in the mod's README or a dedicated CREDITS file once one
  exists), not just in dev-only notes that won't ship with the mod.
- **Before adapting anything beyond "studied the pattern," check that
  specific mod's own license/README for its actual terms** — this hasn't
  been done yet for BEX or RogueTech specifically, and "give credit" is
  this project's baseline courtesy, not a substitute for whatever terms
  the source mod actually publishes (some community mods permit reuse
  freely, others ask for permission first, others restrict commercial
  reuse). Check the specific mod's own terms at the point of actually
  adapting something from it, not assumed in advance.

## If this ever changes scope

If distribution ever moves toward something with real commercial intent —
selling the mod, running ads against it, a large enough audience that
Sunrise/Bandai/Sotsu take notice — that's the point to actually consult a
real IP lawyer rather than continue relying on this doc. Everything above
describes normal, low-risk fan-project practice; it isn't a shield against
a determined rights holder if the project's scale or monetization model
changes.
