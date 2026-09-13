# Gundam UC / BattleTech Mod — First Working Milestone

## Goal for this milestone (deliberately small)

Not "the whole One Year War." Just this:

> A campaign starting **a few days before the outbreak of the One Year
> War (UC 0079).** The Federation force consists of early Operation V
> prototypes — the **Guntank (RTX-65)** and **Guncannon First Type
> (RCX-76-02)** — since GM mass production doesn't begin until later in
> the war (after Operation Odessa). The player's commander pilots the
> **Prototype Gundam (RX-78-1)** — not the Full Armor Gundam from
> *Thunderbolt*, which is a late-war customized unit and doesn't fit this
> early a timeline.

That's it. Get that loop working end-to-end before touching anything else in
the design doc. A mod that does one small thing correctly beats a design doc
that does everything on paper and nothing in-game.

---

## Critical lesson learned — ALWAYS use complete JSON files, not sparse overrides

Through hands-on debugging (a genuine multi-hour session — see below for the
full story if useful later), we confirmed that **sparse/partial JSON
overrides break this specific game installation.** ModTek's own general
documentation recommends only including the fields you want to change in
an override file. On this setup, that approach reliably causes a
`NullReferenceException` in the base game's own weapon-indexing code
during MDD rebuild, which then hangs the mechbay/skirmish screen forever
for every unit referencing that weapon.

**The fix, confirmed working end-to-end:** every override file — weapons,
units, anything — must be a **complete copy of the original stock JSON**,
with only the specific field(s) you want changed actually modified. Copy
the whole file, edit what you need, keep everything else identical.

This is the opposite of ModTek's own general advice, but it's what
actually works on this install, confirmed via direct A/B testing (sparse
override hung the game; complete-file override loaded cleanly and applied
correctly, confirmed via in-game damage value). **Treat this as a hard
rule for every future unit/weapon/component file in this project**, not
just a one-off fix for the AC5/Medium Laser test.

---

## Step 0 — Toolchain setup (one-time, ~1-2 hours)

1. **Own the base game.** BattleTech (HBS, 2018) on Steam/GOG. You need the
   actual game files to mod against.
2. **Install BTML + ModTek.**
   - Repo: https://github.com/BattletechModders/ModTek
   - Follow their install guide exactly — it patches `Assembly-CSharp.dll`
     once via an injector, then everything after that is drop-in folders.
   - Verify install: main menu should show a `/W MODTEK` tag after launch.
3. **Get a C# dev environment**, since some of this will eventually need
   Harmony patches, not just JSON:
   - Visual Studio Community (free) or Rider if you have a license.
   - .NET Framework matching what BattleTech's Unity build targets (ModTek's
     docs/readme will specify — check this when you install, don't assume,
     Unity/Mono versions across a 2018 game can be particular).
4. **Get a JSON/text diff tool you trust** (VS Code is fine — you probably
   already use it). Most of what you'll do at first is JSON editing, not C#.
5. **Clone a few existing simple mods to read, not run**, so you can see
   real `mod.json` structure and file layout conventions:
   - https://github.com/BattletechModders/ModTek (the loader itself, has docs)
   - https://github.com/BattletechModders/IRBTModUtils (shows how movement
     stats get patched via Harmony — useful reference for later)
   - Anything small on the BattletechModders GitHub org — pick one under
     ~500 lines to actually read start to finish.

**Checkpoint:** you can launch the game with ModTek active and see the
`/W MODTEK` tag. Nothing modded yet — just the pipe is working.

---

## Step 1 — "Hello World" mod (confirms your pipeline works)

Before touching anything Gundam-related, do the modding equivalent of
printf("hello world"): pick one existing stock unit (say, the base game's
Locust) and change one visible number — its walk speed, or its armor value —
via a ModTek JSON override. Load the game, start a skirmish, confirm the
number changed.

This step exists purely to prove:
- Your file goes in the right folder
- Your `mod.json` is well-formed
- ModTek picks it up and merges it over the base file
- You can see the effect in-game

If this works, every other JSON-driven change in the project is the same
mechanical pattern, just on different files. This is the single most
important step to not skip — it turns "modding" from an abstract idea into
a five-minute feedback loop you'll repeat hundreds of times.

**Checkpoint:** you changed one number in one file, saw it reflected in game.

---

## Step 2 — Reflavor before you invent

Resist the urge to build new mechanics yet. Your first real content pass
should be **reskinning existing BattleTech units as UC units**, because it
lets you learn the data schema without also inventing new systems at the
same time.

Concretely:
1. Pick the **Guntank (RTX-65)** and **Guncannon First Type (RCX-76-02)**
   — the Federation's actual pre-war Operation V prototypes at this point
   in the timeline. Both already built (see `StreamingAssets/data/chassis/`
   and `.../movement/`) — this is your bread-and-butter starting force at
   mission 1.
2. Do **not** touch the Prototype Gundam's loadout/unlock yet. It exists
   as a ChassisDef already, but shouldn't be purchasable/available in the
   starting roster — that's the whole point of this milestone.

Each of these is: copy a stock unit JSON → rename → adjust a handful of
stat fields → drop in `StreamingAssets` mirror folder per ModTek's merge
convention → reload → verify in the mech bay / skirmish list.

**Checkpoint:** Guntank and Guncannon exist as selectable, correctly-named,
correctly-statted units in a skirmish lobby.

---

## Step 3 — Restrict the starting roster

This is the first *systemic* piece, not just reflavoring. You need the
player's available force at campaign start to be Guntank/Guncannon-based,
with no Gundam-lineage unit present at all.

### Confirmed campaign-start spec

- **Career mode, not Campaign.** The stock Campaign is a fixed scripted
  story with its own narrative beats that would actively fight the UC
  storyline; Career is open-ended and procedurally driven, which is what
  the war-index/multiple-fronts design needs. Campaign should be
  disabled.
- **Starting roster: 4x Guncannon (RCX-76-02) + 1x Guntank (RTX-65).**
  **The reason for 4 Guncannons is the opening theater, not just
  attrition:** the first 2-3 months of the OYW are fought entirely in
  space (Loum, the colony drop, fighting around the Sides). Zeon doesn't
  begin its Earth invasion until after the Antarctic Treaty. So the
  starting force needs to be space-capable.
  - **Guncannon has boosters** (MaxJumpjets 2, from the Iron Cavalry
    drop-booster lore) — meets the space-deployment gate in
    docs/02-FEATURE-LIST.md item 17.
  - **Guntank has zero thrusters and therefore literally cannot deploy
    during the space phase.** It sits in the hangar as dead weight until
    the war reaches the ground. This is intentional and falls directly
    out of rules already locked in.
  - Net effect: a 5-unit roster where only 4 are usable at war's start,
    exactly matching the 4-unit standard deployment. **Lose one Guncannon
    and you cannot field a full squadron.** Genuine early-game pressure, and
    it makes the eventual Earth descent a real inflection point — the
    useless Guntank suddenly becomes relevant.
- **Opening sequence timing:** commander arrives to take command →
  **Prototype Gundam delivered ~1 in-game hour later** (uses the
  career-mode "commander's mech arrives shortly after start" pattern) →
  **the One Year War begins ~1 hour after that.** Mirrors canon (White
  Base's first mission was literally retrieving the Operation V
  prototypes, and Side 7 was attacked almost immediately on arrival),
  and sets the intended tone: a crew handed a prototype and thrown into
  a war they aren't ready for.
- **Ship starts with no catapults** — see docs/02-FEATURE-LIST.md 17e.
  Early-game launches take the full 3 turns and arrive overheating.
  Catapults are an early refit goal.

### Implementation approaches

Two ways to do this, roughly in order of difficulty:
- **Easy / data-only:** if you're building on a custom campaign/contract
  set (not the stock campaign), you control what units are placed in the
  player's starting squadron/inventory via the campaign's starting-state JSON.
  Just don't include Prototype Gundam data there yet.
- **Harder / systemic:** a real "tech unlock by date" gate (so no amount of
  salvage or purchase gets you a Gundam before the scripted date) needs a
  Harmony patch hooking whatever governs unit purchase/salvage eligibility.
  This is a "later" problem — for the first milestone, simply *not having
  the Prototype Gundam purchasable in the mod at all yet* achieves the same
  effect for free.

**Verify before building:** confirm whether BattleTech's roster cap and
deployment size are data-driven or hardcoded. If hardcoded, that
constrains the 17e rotation/capacity design and is much better to know
now than after building around it.

**Checkpoint:** starting a new Career, your mech bay contains 4 Guncannons
and 1 Guntank. No Gundam-lineage unit is purchasable in the mod's files
yet — so there's nothing to accidentally unlock.

---

## Step 4 — The unlock moment (your first scripted event)

Now enable the Prototype Gundam (RX-78-1) — already built as a ChassisDef
— as a unique/named hero unit assigned specifically to the commander.
Trigger its arrival via a scripted mission-reward or narrative event
rather than the shop: something like "the commander is assigned Project
V's third unit ahead of the war's outbreak" — this is your Side 7 /
opening-of-the-war beat, not a mid-war requisition. This is where Mission
Control (https://github.com/CWolfs/MissionControl) becomes relevant —
it's built specifically for custom contract/encounter frameworks, which
is what you need for "complete this scripted introduction, receive the
Gundam as a story-driven unlock."

Don't build the full rank/reputation/tech-tree system yet. One hardcoded
"opening event → Prototype Gundam added to roster" event is enough to
prove the narrative-beat pattern before generalizing it.

**Checkpoint:** you can play a skirmish/mission with a Guntank/Guncannon
squadron, trigger the unlock event, and have the Prototype Gundam appear in
your roster afterward, assigned to the commander.

---

## What "done" looks like for this milestone

- Fresh campaign start, a few days before the outbreak of the One Year
  War → Federation squadron is Guntank/Guncannon-based, reflavored and
  correctly named/statted.
- Player plays a mission or two under those constraints.
- A scripted event/mission delivers the Prototype Gundam, assigned to the
  commander specifically.
- Nothing else — no tech tree, no fatigue system, no faction reputation,
  no ship progression yet.

Everything else in the original design doc (rank system, fatigue/rest
tracking, ship upgrades, Newtype mechanics, hero-unit rarity, political
intrigue) becomes an additive milestone *on top of* this working loop, not
a prerequisite for it.

---

## Step 5 — UC timeline alignment (quick wins)

Small, low-risk data changes that make the campaign stop feeling like
reskinned 3025. Do these first — they're cheap and several are
load-bearing for later date-gated systems.

- Set `CampaignStartDate` to the UC 0079 OYW outbreak date (both
  `Story.` and `CareerMode.` copies in `SimGameConstants.json`).
  Load-bearing: unit release dates, war-phase transitions, and the
  Flashpoint chains are all date-driven.
- Replace the stock "Mason's Marauders" heraldry block.
- Swap pilot name pools (`name_male_intl` etc.) for militaristic/
  UC-appropriate lists.
- Decide on `CompanyEventStartingChance` — the stock Inner Sphere
  flavor-event pool is still live.

**Checkpoint:** a new Career starts on a UC-correct date with no visible
3025-era naming.

---

## Step 6 — Time granularity + day/night missions

**Real design goal (clarified by user):** a clock display like
`13:00 17th January UC 0079`, where launching a mission at 01:00 means
fighting a **night mission**. Time-of-day is a gameplay input, not a
cosmetic clock — night should mean reduced detection ranges, visual
penalties, and a genuine push toward short-range brawling.

### Why this is now worth the expensive path

Earlier guidance in this file recommended the cheap "relabel days as
hours" approach. **That recommendation is superseded** — it can't
deliver time-of-day-driven missions, because the engine would have no
sub-day state to drive them from.

Research (2026-08-20) confirmed night conditions **already exist in
vanilla** and are proven modifiable:
- RogueTech ships Night Vision gear described as enabling "Day time
  visual Detection range during Night and Dark Conditions" — so the
  game genuinely has night/dark light states that affect detection.
- RogueTech Thermal Vision gear gives accuracy bonuses scaled to target
  heat with distance decay; Advanced Optics negates "No Visuals"
  penalties within range.
- Vanilla's detection model is data-driven: base spotting distance of
  300 set in `SimGameConstants`, modified by per-unit
  `SpottingVisibilityMultiplier` / `SpottingVisibilityAbsolute`.
- Community pointers put night-condition control in
  `StreamingAssets/data/contracts` and `StreamingAssets/data/maps`,
  and note RogueTech's night gear reacts with stats **and a visual
  effect** — so there's a visual layer too, not just numbers.

**So this is not building night combat from scratch — it's building the
time-of-day → night-condition link on top of systems that already work.**
It also stacks with the Minovsky design (jamming + darkness compounding)
and gives the MS-bay internal-components system real content: night
vision and thermal sensors as genuine equipment upgrades.

### Investigation order (do these before committing to the rework)

1. Study RogueTech's night/thermal vision implementation (already in
   `reference-mod/`). Find **what condition/stat the gear checks
   against** — that's the hook everything else hangs off.
2. Grep `data/contracts` and `data/maps` for night/light/time-of-day
   fields to find what actually *sets* the condition.
3. Only then decide on the sim rework, with a concrete payoff rather
   than a hypothetical one.

### The rework itself (once justified)

Converting the sim to sub-day ticks means Harmony work against
`OnDayPassed` / `DaysPassed` / `MechReadyTime` / contract + travel
timers — and would likely break the Step 4 arrival patch, which is
keyed to `FlashpointDayPassed`. Budget for fixing that patch as part of
the work, not as a surprise.

Downtime durations get rescaled in the same pass so the felt weight is
right: a few days' injury stings, a pilot who survived his MS exploding
is out ~30 days and genuinely hurts the roster.

**Checkpoint:** the campaign clock shows date + time of day, launching a
mission at night produces a night mission with reduced detection, and
the Step 4 Gundam arrival still fires correctly.

---

## Step 7 — First opposing force: Zaku II (and later, GM)

The mod currently has **no enemy units** — every fight is against stock
Inner Sphere 'Mechs, which is the biggest remaining immersion break.

- **MS-06 Zaku II first.** Base F/J type, then variants (S command
  type, Char's custom). Highest priority of any unit work.
- **RGM-79 GM after.** Canonically post-Operation-Odessa, so it wants
  date-gating that doesn't exist yet — genuinely a mid-war unlock, not
  a start unit.

Per-unit build order (unchanged, and proven across three units now):
research entry in `dev-reference/` → ChassisDef + MovementCapabilitiesDef
→ WeaponDefs → MechDef → `mod.json` entries → in-game load test.

**Checkpoint:** Zaku IIs appear as opposition in a real battle.

---

## Suggested order after this milestone ships

1. Expand roster: Zaku as the first opposing-force unit (same reflavor
   pattern as Step 2, just enemy-side).
2. GM as a later-war Federation unlock (mass production begins after
   Operation Odessa, meaningfully later in UC 0079) / a second named hero
   unit for a rival NPC pilot (Thunderbolt-style — e.g. a Zeon counterpart),
   same event-driven pattern as Step 4.
3. Basic rank system (this is mostly campaign-state tracking + gating,
   similar difficulty to Step 3).
4. Ship-as-base persistence (bigger — likely needs looking at how the stock
   game's "Argo" ship/base system works, since you'll be piggybacking on
   its existing hooks rather than building from scratch).
5. Fatigue/deployment tracking (novel system — first thing that's fully new
   mechanics rather than reflavored existing ones; save Harmony-patch
   confidence for this).

---

## Where I'm actually useful vs. where you're on your own

**I can do with you, in this chat, right now:**
- Draft the actual JSON for the Ball/fighter/Gundam unit reflavors (stat
  tables, then translated into ModTek-format JSON skeletons)
- Draft the mod.json manifest structure
- Write the scripted unlock event / mission narrative text
- Review/debug JSON or C# you paste in, explain error messages
- Search for current ModTek/MissionControl API specifics when we hit
  something version-specific I shouldn't guess at

**You're doing yourself:**
- Actually running the game, installing the toolchain, testing each step
- Any 3D art/model work
- Final compile/test loop — I can write plausible Harmony patch code, but
  I can't compile or run it against the real game DLLs to confirm it works

Want to start with Step 1 — I can write out the exact `mod.json` and the
stat-override JSON for changing a stock unit's walk speed, as a template
you can adapt?
