# ModTek Technical Notes — Confirmed Through Direct Testing

Everything here was verified empirically against this specific game
install, not assumed from ModTek's general docs (which are sometimes
wrong for this setup — see below). Treat this file as higher-authority
than general ModTek documentation when the two conflict.

## Some stock JSON files are NOT strict JSON — check before hand-copying

Confirmed via `data/simGameConstants/SimGameConstants.json` (the file
governing Career/Campaign mode starting rosters, among hundreds of other
things — see the Step 3 roster-lock notes in `00-STATUS.md`): the stock
file **fails to parse under a strict JSON parser** (tested with
PowerShell's `ConvertFrom-Json`). Found: two C-style `/* */` comments,
several trailing commas before `]`/`}`, and one flat-out missing comma
between two properties (`"MaxMoralePowerLevelLimits": [...]` followed
directly by `"MaxContractsPerSystem"` with no comma at all). The game's
own parser is clearly more lenient than standard JSON.

- `data/constants/CareerDifficultySettings.json`, by contrast, **is**
  strict-valid JSON — this isn't a universal property of all BattleTech
  data files, it varies per file. Check each one before assuming.
- **When producing a full-copy override of a file like this, don't just
  preserve the lenient quirks verbatim** (risk of a typo introducing a
  *real* syntax error the lenient parser also can't recover from).
  Instead, fix the file to be fully strict-JSON-valid (remove comments,
  remove trailing commas, add the missing comma) while changing only the
  intended values. A parser lenient enough to accept the sloppy original
  will always also accept the strict-valid version — leniency only adds
  acceptance, it never removes it — so this is strictly safer, not
  riskier, and it makes the file validatable with normal tools going
  forward (`ConvertFrom-Json`, any JSON linter) instead of silently
  failing every strict check the way the stock file does.

## CRITICAL: Always use complete JSON files, not sparse overrides

ModTek's own docs recommend sparse overrides (only include the fields
you're changing). **On this install, that causes a silent-hanging bug.**

- A sparse override (e.g. `{"Damage": 450}` on a WeaponDef) causes a
  `NullReferenceException` in the base game's own
  `BattleTech.Data.MechComponents_MDDExtensions.UpdateWeaponDef` during
  ModTek's MDD indexing pass.
- The exception is caught silently — game doesn't crash, but the affected
  resource never finishes loading, and every MechDef referencing it hangs
  forever in `AwaitingDependencies`. Mechbay/Skirmish screen spins
  indefinitely.
- Confirmed via direct A/B test: identical sparse override → hang, on two
  unrelated weapon types (one with ammo linkage, one without). Full copy
  of the same file with the same field changed → loads cleanly, damage
  value confirmed correct in-game.
- **Rule: every override/new file must be a complete copy of the source
  JSON, with only the intended field(s) actually changed.** Never write a
  sparse patch.

## CRITICAL: `WeaponSubType` (and likely other "label-looking" string
## fields) are strict enums, not free text — same silent-hang failure
## mode as sparse JSON, different trigger

Confirmed via a real in-game freeze: the game hung at the intro-video
screen (`HoldForIntroVideo`, "press ESC to skip" — screen goes black,
ESC does nothing) on the very first load of new WeaponDef content.

- Root cause found in `ModTek.log`:
  `System.ArgumentException: Requested value 'VulcanGun' was not found.`
  (and the same for 4 other custom weapons) during
  `MDDBCache: Exception when indexing <WeaponId> (WeaponDef)`. This is a
  C# `Enum.Parse` failure — `WeaponSubType` was set to invented
  descriptive strings (`"VulcanGun"`, `"LargeCaliberCannon"`, etc.)
  instead of a real enum member.
- **This is the exact same failure category as the sparse-JSON bug
  above** — an exception during MDD indexing gets caught silently, the
  resource never finishes loading, and everything depending on it (here:
  every MechDef using any of the 5 broken weapons — i.e. both our
  starting units) hangs forever. The only difference from the sparse-JSON
  case is *what* threw the exception, not *how* the game responds to it.
  Confirms this MDD-indexing-swallows-exceptions behavior is a general
  pattern on this install, not a one-off tied to sparse files
  specifically — **assume any malformed/invalid field value in a
  WeaponDef (or likely ChassisDef/MechDef) fails this same way: silent,
  and manifests as a frozen loading screen, not a crash or log ERROR
  banner.**
- **`WeaponSubType` valid values (confirmed exhaustively from every stock
  WeaponDef in `data/weapon/`):** `AC2`, `AC5`, `AC10`, `AC20`, `Gauss`,
  `Flamer`, `MachineGun`, `Melee`, `DFA`, `PPC`, `PPCER`, `LRM5`,
  `LRM10`, `LRM15`, `LRM20`, `SRM2`, `SRM4`, `SRM6`, `SmallLaser`,
  `SmallLaserER`, `SmallLaserPulse`, `MediumLaser`, `MediumLaserER`,
  `MediumLaserPulse`, `LargeLaser`, `LargeLaserER`, `LargeLaserPulse`,
  `AIImaginary`. No custom/invented values — pick whichever existing
  value best matches the new weapon's actual behavior (in practice: the
  same value already chosen for `PrefabIdentifier` and `ammoCategoryID`,
  since stock weapons keep all three in sync — e.g. `AC10` file has
  `WeaponSubType: "AC10"`, `PrefabIdentifier: "AC10"`,
  `ammoCategoryID: "AC10"`).
- **Don't assume other "looks like a label" fields are free text either**
  — `Type` and `Category` are also validated against real enums
  (`data/enums/WeaponCategory.json` for `Category`); `WeaponSubType` just
  happened to be the one that wasn't checked against a known enum file
  before use this session. When adding a new WeaponDef, prefer copying
  every non-numeric field's value from the closest matching stock weapon
  rather than inventing a new string, unless a field is confirmed free
  text (like `Description.Name`/`UIName`/`Details`, which are just
  display strings).
- **Diagnostic tip:** `Mods/.modtek/ModTek.log` is large (multi-MB) —
  don't try to read it in full. Grep for the mod's own name first to
  find the Manifest-processing section, then grep for
  `Exception|\[ERROR\]|\[FATAL\]` to find real failures fast. A `[WARNING]`
  is not automatically the cause of a freeze — cross-check against
  actual `Exception` lines before assuming a warning is the culprit.

## Filename / internal Id casing must match EXACTLY

- ModTek merges based on the internal `Id` field (often nested, e.g.
  `WeaponDef.Description.Id`), not the filename — but the filename should
  still match it exactly, case-for-case.
- Windows filesystems are case-insensitive, so a mismatched-case file
  will load without error, but ModTek's merge key comparison is
  case-sensitive. Result: mod "succeeds" per log but silently merges into
  a non-existent/wrong entry, with zero effect in-game.
- Real example hit: stock file was `Weapon_Autocannon_AC5_0-STOCK` (caps
  STOCK). Our file was `...-Stock` (mixed case). Looked fine in logs
  ("Add"/"Merge" succeeded), had zero effect in-game.
- Always verify exact casing against the real stock file before naming
  an override.

## CONFIRMED: `unit_release` MechTag required for a unit to appear in the
## Skirmish/mechbay selection list

Diagnosed without a log line (unusual for this file) and **confirmed by
an actual in-game retest** (2026-08-20) — Guntank and Guncannon both
appeared correctly in the mechbay, with the right hardpoint weapon
names, immediately after this fix.

- After fixing the two bugs below, the game loaded completely cleanly
  (no errors, no warnings for our files at all), but Guntank and
  Guncannon still didn't appear in the Skirmish mechbay selection list —
  a clean load with a missing-from-UI symptom means a filter, not a load
  failure, and filters don't necessarily log anything.
- Checked `data/tags/UnitTags.json` (the game's real tag registry, not
  invented) — `unit_release` is a registered tag.
- Checked every stock `MechDef`: 90 of 104 have `unit_release`. The 14
  that don't are conspicuously all non-selectable content — test dummies
  (`_TESTDUMMY`, `_TARGETDUMMY`) and Flashpoint-specific hero units that
  only ever appear through scripted story content, never normal
  selection. Every normal player-selectable stock mech has it.
- Our two MechDefs didn't have it (oversight when `MechTags` was first
  written — copied the general shape of a stock example but not this
  specific tag).
- **Rule confirmed: every player-selectable MechDef needs
  `unit_release` in `MechTags.items`, full stop.** Add it by default to
  every future MechDef in this project (Prototype Gundam, Zaku, GM,
  etc.) rather than re-deriving this each time.

## CRITICAL: ModTek recursively scans EVERY subfolder under `Mods/` for
## `mod.json`, no matter how deeply nested or clearly "not meant to load"

Confirmed via a real broken run: main menu loaded but under the wrong
title ("BattleTech Extended") with a mod-load warning, and
`Mods/.modtek/ModTek.log` had over 21,000 lines mentioning
`reference-mod` (a folder kept locally, purely for reading how BEX and
RogueTech solve modding problems — see `04-COPYRIGHT-GUIDELINES.md`).

- ModTek does not distinguish "a folder I was told is a mod" from "a
  folder that happens to contain a `mod.json` file somewhere inside it."
  It walks all of `Mods/` and treats every `mod.json` it finds as a real
  mod to load — including the ~300+ real `mod.json` files nested inside
  `reference-mod/Extended_Tactics_2_0_0_4/Mods/*` and
  `reference-mod/RogueTech-master/Core/*`.
- Result: it tried to load BOTH BEX and RogueTech as active mods
  alongside ours. BEX partially succeeded (hence the title screen
  takeover). RogueTech mostly failed with cascading
  `Will not load "X"; missing dependencies: [...]` warnings, since its
  individual component mods were never meant to run outside RogueTech's
  own installer/ordering.
- **There is no "ignore this folder" mechanism** other than physically
  keeping non-mod content out of `Mods/` entirely (each mod's own
  `Enabled` field isn't a fix — that means manually editing ~300+
  third-party `mod.json` files, not a real solution).
- **Fix:** moved `reference-mod/` to the BattleTech root
  (`F:\...\BATTLETECH\reference-mod\`), one level up, outside `Mods/`
  entirely. Confirmed this is where all such "read-only reference,
  never meant to load" content must live going forward — anything that
  needs to be *readable by Claude Code* but *invisible to ModTek* has to
  be outside `Mods/`, full stop, not just excluded from our own mod's
  Manifest.
- **Diagnostic tip:** if the main menu shows unexpected branding, a
  mod-load warning appears, or the log is suspiciously huge, grep
  `ModTek.log` for `reference-mod` (or any other non-mod folder name
  under `Mods/`) before assuming the problem is in our own mod's files.

## AddToDB manifest field is deprecated / non-functional

- Older references (2018-era forum posts) mention an `AddToDB` field on
  Manifest entries to control MDD indexing.
- Confirmed on current ModTek (v4.5.0): `AddToDB=False is being ignored`
  — logged explicitly as ignored. Do not rely on this field.

## What HardpointDataDef actually is (don't confuse with ChassisDef.Locations.Hardpoints)

- `ChassisDef.Locations[].Hardpoints` = abstract capability data (what
  weapon *category* — Energy/Ballistic/Missile/AntiPersonnel/Melee — can
  go where). This is what determines legal loadouts.
- `HardpointDataDef` = purely **visual**. Maps "if weapon type X is
  equipped at this mount, use this specific 3D prefab" — every entry
  references a `chrPrfWeap_<chassisname>_...` prefab tied to that exact
  stock chassis's 3D model/skeleton.
- Since we have no custom 3D models yet, our custom chassis must borrow
  an EXISTING stock chassis's `HardpointDataDefID` wholesale (not write a
  new one) — and that stock chassis must actually have prefab coverage
  for the weapon types we intend to equip. Confirmed gap: Awesome's
  hardpoint data has zero ballistic weapon prefabs (Laser/PPC/LRM/SRM/
  NARC/AMS only) — unusable as a placeholder for a ballistic-focused unit
  like Guntank.
- **Check coverage per exact hardpoint location, not just "has the
  category somewhere."** Coverage is inconsistent per-location even
  within one chassis — e.g. Cataphract has Ballistic prefabs at
  LeftArm/RightArm/RightTorso but NOT LeftTorso (Energy/AMS there
  instead); Dragon is the mirror case (Ballistic at LeftTorso/RightArm,
  Energy-only arms). Checked 9 stock chassis for Guntank's 4-location
  Ballistic need (LeftArm/LeftTorso/RightTorso/RightArm); best was
  Cataphract at 3/4, nothing hit 4/4. Don't assume "full ballistic
  coverage" without checking each `Locations[].Hardpoints` entry against
  the borrowed chassis's per-location weapon lists.
- **Resolved:** Melee does NOT go through this per-chassis prefab system
  at all — that's why no stock HardpointDataDef file has a "Melee" slot,
  not just the three checked originally. Confirmed via
  `data/enums/WeaponCategory.json`: the `Melee` category has
  `HardpointPrefabText: "chrPrfWeap_generic_melee"` with
  `UseHardpointPrefabTextAsSuffix: false` — one fixed generic prefab
  shared by every 'Mech, not a per-chassis lookup. Base unarmed melee
  (punching) is already fully wired via ChassisDef fields
  (`MeleeDamage`/`MeleeInstability`/`PunchesWithLeftArm`/etc.), which all
  three of our units already have. A *named* melee weapon (beam saber)
  should follow the stock Hatchetman-hatchet pattern instead:
  `data/upgrades/actuators/Gear_Actuator_Prototype_Hatchet.json` is an
  `Upgrade`-type component (`AllowedLocations: "Arms"`, empty
  `PrefabIdentifier`) that adds a passive stat effect
  (`Float_Add` to `DamagePerShot` for `targetWeaponSubType: Melee`)
  rather than being a real equipped weapon with its own prefab. Build the
  beam saber the same way when we get to it.

## Custom ChassisTags convention: `unit_humanoidHands`

- `ChassisDef.ChassisTags.items` exists on every stock chassis but is
  always empty in the base game — confirmed via grep across
  `data/chassis/`, no stock file uses it. There's a master tag registry
  at `data/tags/UnitTags.json` (things like `unit_jumpOK`,
  `unit_wheels`, `unit_electronicWarfare`), but nothing in the base game
  or ModTek appears to validate ChassisTags against that registry at
  load time — it reads as a free-form string list.
- **Decided (2026-08-19):** added a custom tag, `unit_humanoidHands`, to
  mark which of our units have hand/finger articulation and are
  therefore eligible for handheld weapons (`docs/02-FEATURE-LIST.md`
  17b) and sub-flight system mounting (17d) — both systems already
  independently needed this exact same fact, so one flag now serves
  both instead of re-deriving "does this unit have hands" from lore
  each time it comes up.
- This tag is **not read by any existing game or ModTek system** — it
  does nothing on its own right now. It's a forward-looking hook for
  whatever Harmony patch eventually implements the 17b hand-equip system
  and 17d sub-flight eligibility check, so that code can query
  `ChassisTags` instead of hardcoding a per-chassis-ID list.
- Applied so far: `chassisdef_guncannon_RCX-76-02.json` and
  `chassisdef_protogundam_RX-78-1.json` have the tag (both proper bipeds
  with real hand/finger articulation per canon).
  `chassisdef_guntank_RTX-65.json` does NOT have it — single fixed
  posture, tracked-vehicle-with-a-torso, no hands, already excluded from
  sub-flight for the same reason (see its `MS/*.md` entry).
- **Apply this tag going forward** to every new chassis as it's built —
  GM, Zaku I/II, Gouf, etc. all need this determined once and tagged
  rather than re-derived per weapon-research pass. Cannon-variant
  chassis (e.g. a GM variant with an added fixed shoulder cannon) get
  their own separate ChassisDef file per BattleTech's existing
  one-file-per-variant convention (confirmed via stock
  `chassisdef_jagermech_JM6-A` vs `JM6-S`) — the tag travels with
  whichever specific variant file has the hand articulation, independent
  of whether that variant also happens to have additional fixed
  hardpoints.

## Confirmed-working mod.json Manifest pattern

```json
{
    "Name": "YourModName",
    "Version": "0.0.1",
    "Enabled": true,
    "Manifest": [
        {
            "Type": "WeaponDef",
            "Path": "StreamingAssets/data/weapon/YourFile.json"
        }
    ]
}
```
- `Type` must be explicitly declared — auto-detection from path alone did
  not work reliably on this install (caused an early "no types found"
  failure).
- Path is relative to the mod's own folder.
- **ModTek scans every JSON file under the mod's `StreamingAssets/`,
  whether or not it's listed in `Manifest`** — confirmed via
  `ModTek.log`: two files that existed on disk but were deliberately
  left out of `Manifest` (Prototype Gundam's ChassisDef/
  MovementCapabilitiesDef, held back pending unrelated work) still
  produced a log line each: `[WARNING] Can't resolve type, no types
  found for id and extension, either an issue with mod order or typo
  (case sensitivity)`. Non-fatal — ModTek just skips the file — but it
  means "leave a file undeclared to keep it inert" doesn't produce a
  clean log; it produces a warning. If a file genuinely isn't ready yet,
  either declare it properly (safe — a ChassisDef/MovementCapabilitiesDef
  with no MechDef pointing at it still can't be spawned/purchased) or
  keep it physically outside `StreamingAssets/` until it's ready, not
  just outside the `Manifest` list.
- `ShouldMergeJSON: false` is what actually controls add-new vs.
  merge-into-existing (not `AddToDB`, confirmed ignored on this ModTek
  version — see above). Set it explicitly on every new-content entry.

## Using other mods as reference (RogueTech, BEX, etc.)

When Claude Code has full filesystem access, it's worth cloning a
complex, mature mod (RogueTech recommended over BEX — heavier use of CAB
custom 3D assets, custom Flashpoint-style mission content, and deep DLC
content-pack integration, which is closer to what we're building and
directly relevant to the DLC-related MDD indexing issue documented above)
into the workspace alongside our own mod folder and the game files
themselves.

**Rule: this is for studying patterns and technique, not for copying
content.** Read how they solved a problem (e.g. how they structure a
custom mission framework, how they wire up CAB asset bundles, how they
handle DLC-content interactions) and apply the *technique* to our own
original content. Never copy-paste their actual assets, JSON content, or
code wholesale into this mod. Standard, expected practice in this modding
community — the line is "learn from," not "lift from."

## Standalone `SimGameMilestoneDef` files don't fire in Career mode —
## use a self-gated `SimGameEventDef` instead

Spent 3 full test cycles (including a 3-week real-time wait) trying to
schedule a one-time Career-mode event via a custom
`data/milestones/*.json` file (`Type: SimGameMilestoneDef`,
`Results[].ForceEvents` to delay-fire a real event). Loaded cleanly every
time (`ModTek.log`: zero errors), `Requirements` were confirmed true the
entire time, and it still never fired.

**Root cause (confirmed via exhaustive stock-file survey, not guessed):**
every one of the 194 stock files under `data/milestones/` is either part
of the numbered Campaign story chain (`milestone_NNN_*.json`, driven by a
`NextStoryMilestone`-style pointer) or a `quickstart_*` Flashpoint intro.
Flashpoints have their own *separate* milestone mechanism —
`milestoneSets/*.json`, one bundled JSON with a `StartingMilestoneID` and
explicit `Flashpoint_SetNextMilestone` action chaining between entries —
structurally distinct from a loose milestone file. Neither BEX nor
RogueTech, despite both adding substantial original Career-mode content,
ship a single custom standalone `SimGameMilestoneDef`. Nothing in Career
mode ever points an evaluator at a loose milestone file, so its
`Requirements` are simply never checked, regardless of how they're
written.

**What actually works for Career-mode custom triggers:** an ordinary
`SimGameEventDef` carrying its own top-level `Requirements` + `Weight`
fields (same shape as its `Options[].RequirementList`, just one level up,
alongside `Description`/`Scope`). Confirmed via two independent sources:
RogueTech's Aircademy events (`forceevent_co_VTOL_Aircademy*.json`) use
exactly this shape, and — more importantly — a genuine **stock** file,
`data/events/event_mw_hullIntegrity.json`, an ordinary Career/Campaign-
agnostic "camp life" flavor event with no milestone involved anywhere,
has the identical top-level `Requirements`/`Weight` pair. This is the
real data-driven hook the game's random-event system reads.

**Working pattern for "fire once, N days after some Company-scope
condition becomes true, without a milestone":** a small invisible
trigger event — `EventType: "UNSELECTABLE"`, blank `Name`/`Details`,
`OneTimeEvent: true`, top-level `Requirements` gating on a real stat
comparison (e.g. `MissionsComplete == 0`, not a custom tag — see the tag-
scope caveat below), reasonably high `Weight` — whose single automatic
Result does nothing but `ForceEvents` at the real (visible) event with
the desired `MinDaysWait`/`MaxDaysWait`. See
`events/event_gundamuc_gundamArrivalTrigger.json` for the working
implementation.

**Related, still-unconfirmed caveat carried over from the milestone
attempt:** don't assume a custom tag injected into
`SimGameConstants.json`'s `CareerMode.CareerStartingTags` actually lands
on the Company as a queryable tag — no stock example confirms that field
does anything beyond gate travel permissions (its stock values are all
`map_travel_*`). Gate on a real stat via `RequirementComparisons`
instead; it's directly confirmed working (`MissionsComplete` reads
correctly at Company scope).

## CONFIRMED (decompiled source): bulk fast-forward skips the entire
## milestone/random-event evaluation pass — day-tick JSON triggers need a
## Harmony patch, not just the right file type

Supersedes/completes the section above. The self-gated `SimGameEventDef`
fix described above *also* never fired in real testing (3-week wait,
completely silent — not even unrelated flavor events fired). Decompiled
`Assembly-CSharp.dll` (`ilspycmd`, pinned to v8.2.0.7535 — the latest
version needs a newer .NET SDK than ships here; use `-t
<FullyQualifiedTypeName>` to stdout for a single type, since `-p`
full-project decompile stack-overflows on at least one method in this
assembly) to find out why.

`SimGameState.OnDayPassed(int timeLapse = 0)` is the master per-day tick.
`timeLapse` is `0` for a normal single-day advance and `>0` when the
player holds fast-forward and the game batches several days into one
call. **`UpdateMilestones()` and `interruptQueue.QueueEventTest()`** (the
method that rolls the random "camp life" event pool — pilot banter,
Argo maintenance chatter, everything an ordinary `SimGameEventDef` relies
on to fire spontaneously) **only run when `timeLapse == 0`** — they are
silently skipped for every day inside a bulk fast-forward skip. This is
why a held fast-forward can produce a multi-week span with *zero* random
events of any kind, ours or stock's.

Not everything is skipped, though: `DaysPassed` increments correctly
regardless of `timeLapse`, and `FlashpointDayPassed()` — called
unconditionally at the end of `OnDayPassed`, no `timeLapse` guard — is
what fires the Heavy Metal DLC's starting-crate popup and the
flashpoint-expiration warning. That's why those two things always show
up under fast-forward while nothing data-driven ever does. Confirmed
this is the deliberate, known hook point (not a guess) by decompiling
RogueTech's `DisableHMLootbox.dll` — a one-method Harmony patch on this
exact `FlashpointDayPassed` method.

**Practical takeaway: any Career-mode trigger that must fire purely from
elapsed calendar days (independent of travel, missions, or other player
action) cannot be done in JSON alone — it needs a Harmony patch on
`SimGameState.FlashpointDayPassed` (or another method proven to run
unconditionally in `OnDayPassed`).** JSON-only milestones/events remain
correct for anything gated on a real action (mission complete, travel,
contract accepted) — those actions trigger their own unconditional
evaluation passes outside the day-tick shortcut.

**How to fire an existing `SimGameEventDef` from a patch, bypassing the
whole Requirements/roll/ForceEvents pipeline:**
```csharp
SimGameEventDef eventDef = simGameState.DataManager.SimGameEventDefs.Get("your_event_id");
var tracker = new SimGameEventTracker(); // bare instance is fine, no Init() needed
simGameState.OnEventTriggered(eventDef, eventDef.Scope, tracker);
```
This is exactly what `SimGameEventTracker.ActivateEvent` calls internally
— same code path as a normal roll or a `ForceEvents` firing, just
invoked directly instead of waiting for something else to decide to call
it.

**Building/deploying a Harmony patch DLL for this mod (worked, reusable
recipe):**
- `dotnet` SDK 8.0+ is available in this environment. `dotnet tool
  install -g ilspycmd --version 8.2.0.7535 --add-source
  https://api.nuget.org/v3/index.json` for decompiling (the machine has
  no default NuGet source configured — always pass `--add-source`
  explicitly, or `dotnet restore --source ...` for project restores).
- Class library targeting `net471` (matches the game's Mono/.NET
  Framework 4.7.1 runtime), referencing `Microsoft.NETFramework.
  ReferenceAssemblies` (NuGet, needed since there's no full .NET
  Framework dev pack installed) plus direct `<Reference>` `HintPath`s to
  `Mods/ModTek/lib/0Harmony.dll` and `BattleTech_Data/Managed/
  Assembly-CSharp.dll` (both `<Private>false</Private>` — don't copy
  them into the output).
- Confirm which Harmony API a given `0Harmony*.dll` exposes before
  writing patch code — ModTek's `lib/0Harmony.dll` exposes modern
  `HarmonyLib.Harmony` (HarmonyX 2.15, matching `ModTek.log`'s startup
  banner), NOT the legacy `Harmony.HarmonyInstance` API some older
  reference-mod DLLs (e.g. `DisableHMLootbox.dll`, built 2020) use — both
  work via HarmonyX's interop shim, but write new patches against the
  modern API.
- Source lives outside `StreamingAssets/` (this mod: `dev-source/`) so
  ModTek's Manifest scan never sees the `.csproj`/`.cs` files — only the
  built `.dll`, copied to the mod's root folder, needs to exist inside
  the mod for ModTek to find it.
- Register via `mod.json` **top-level** `"DLL": "YourPatch.dll"` and
  `"DLLEntryPoint": "Namespace.ClassName.MethodName"` (a public static
  parameterless method that calls `new Harmony("your.unique.id")
  .PatchAll(Assembly.GetExecutingAssembly())`) — this is a sibling of the
  `Manifest` array, not an entry inside it. Confirmed convention from
  every DLL-shipping mod in `reference-mod/` (e.g. RogueTech's `BTDebug`:
  `"DLL": "BTDebug.dll"`, `"DLLEntryPoint": "BTDebug.Main.Init"`).
- Nested game enums: some types the game exposes as a simple property
  (e.g. `SimGameState.SimGameMode` returning what looks like it should be
  a top-level `SimGameType`) are actually declared as a **nested** enum
  (`SimGameState.SimGameType`, values `INVALID_UNSET`/
  `KAMEA_CAMPAIGN`/`CAREER`/`NONE`) — reference it fully qualified.

## Terminology: "squadron" in fiction, "Lance" stays in the engine

Per user direction (2026-08-21): our own fiction/prose (flavor text,
docs, narrative descriptions) says **squadron** for the player's group of
MS, navy-air-wing style, never "lance" — that's stock BattleTech's own
ground-combat term and doesn't fit Gundam UC framing.

This is a fiction-layer change only. Do **not** rename anything the
engine actually reads by that string:
- `SimGameConstants.json` field names (`StartingLance`,
  `LanceDropTonnageBrackets`, `BattleSimLancePowerDenom`,
  `StartingDebugLance`, `*IsLightLance`/`*IsHeavyLance`) — hardcoded C#
  deserialization keys, renaming breaks loading.
- MechTag values `unit_lance_vanguard` / `unit_lance_support` — confirmed
  real stock tags (`data/lance/lancedef_*.json`'s dynamic lance-builder
  reads them, and multiple stock MechDefs carry them) used by the game's
  own procedural lance-composition AI. Changing the string silently
  breaks that system for our units; keep them as-is.
- The pilot expertise/rank name `"Lancer"` in `SimGameConstants.json` —
  a stock rank title (cavalry-lancer flavor, not the unit-org term), left
  alone as out of scope for this change.

When documenting or writing about the engine's own field/tag, call it by
its real name (`StartingLance`, etc.) even in prose — don't obscure what
the actual JSON key is. Only the narrative concept of "a group of MS"
becomes "squadron."

## RE-CONFIRMED: melee weapons DO use an Upgrade-component pattern —
## an earlier "correction" of this was wrong, don't repeat the mistake

Mid-session (2026-08-23) this note briefly claimed the opposite of what
follows — that melee items aren't equipment at all, just a chassis stat
— based on checking whether a Hatchetman chassis/MechDef exists in this
install (it doesn't) and wrongly treating that as disproving the
Upgrade-component claim. **That check was the wrong one.** The real
claim was always about a specific *upgrade item file*, not the
Hatchetman chassis, and that file exists regardless of whether the
Hatchetman 'Mech itself is present:
`data/upgrades/actuators/Gear_Actuator_Prototype_Hatchet.json`,
confirmed directly. Lesson: when a doc cites a specific file path,
check that exact path before concluding the claim is wrong — checking a
plausible-sounding *different* path and getting a miss isn't the same
thing, and very nearly got a correct finding overwritten with an
incorrect one.

**What's actually true, confirmed from that real file:**
`ComponentType: "Upgrade"`, `AllowedLocations: "Arms"`,
`Purchasable: false`, `ComponentTags: ["BUILT-IN"]` — a passive
`statusEffects` entry does `Float_Add` on `DamagePerShot` for
`targetWeaponSubType: "Melee"` (the Hatchet's is `+70`). This is a real,
equippable item sitting in an Arms hardpoint slot, exactly like any
other fixed weapon in this mod — **not** blocked behind the mechbay 17b
hand-equip system, since it's a normal MechDef inventory entry with
`ComponentDefType: "Upgrade"` instead of `"Weapon"`.

Separately, also true and not in conflict with the above: every 'Mech
also has a universal, non-purchasable, zero-tonnage base melee attack
(`data/weapon/Weapon_MeleeAttack.json`) driven by `ChassisDef`'s own
`MeleeDamage`/`MeleeInstability`/`DFADamage` fields — that's the
*unarmed* baseline (confirmed via Guncannon's `MeleeDamage: 30` vs.
Atlas's punch-specialist `MeleeDamage: 140`). A named melee weapon like
a hatchet or heat hawk is the **Upgrade item's bonus stacked on top of**
that baseline, not a replacement for it — see
`Gear_Actuator_Zaku_HeatHawk.json` for the pattern applied to Zaku (base
chassis `MeleeDamage: 40` + a `+50` Upgrade item in `LeftArm`).

**Practical rule:** a genuine melee *weapon* (not bare fists) = an
Upgrade-type component in the relevant Arms hardpoint, `Float_Add` on
`DamagePerShot` for `targetWeaponSubType: "Melee"`, `AllowedLocations:
"Arms"`. Don't hand-wave it into the chassis stat alone — build the
item.

## CONFIRMED: how a MechDef actually becomes shop-purchasable, and how
## to flip that at runtime for a date-gated unlock

Researched directly from stock game files (2026-08-28), needed for
RGM-79 GM's date-gated availability (first unit in this mod that isn't
available from Career start or a scripted gift).

**`Purchasable: true` alone does nothing.** A `StarSystemDef`'s
`SystemShopItems`/`FactionShopItems` point at `ItemCollectionDef` CSVs,
which chain through `Reference` rows (major pool → weight-class
sub-pool) down to the actual tier that lists MechDef IDs by name, e.g.
`itemCollection_systemStores_Mechs_common_Medium.csv`:
```
mechdef_shadowhawk_SHD-2H,Mech,1,5
```
Confirmed by checking two ordinary stock `Purchasable: true` mechs
(Locust, Catapult) — both are only reachable because they're explicitly
listed by ID in specific collection CSVs, not because the flag alone
puts them in a global pool. **A MechDef needs both**: `Purchasable:
true` and an ID listed in a collection CSV a shop chain actually
reaches.

No native date/era-gating field exists anywhere in stock data — nothing
like `unlockDate`/`ReleaseDate`. A genuinely date-driven unlock (not
"available from day one, just needs the flag") requires a runtime
component; there's no pure-JSON way to make an entry's *eligibility*
conditional on the campaign clock.

**Practical pattern used for GM:** ship the MechDef with `Purchasable:
false` from the start, but list its ID in a real collection CSV anyway
(a merged/full-copy override, `ShouldMergeJSON: true`, since it's
patching an existing stock resource ID) — so it's structurally "in the
rotation" the whole time, just never selected while the flag is false.
A Harmony patch (reusing the same `FlashpointDayPassed` day-tick hook as
the Gundam-arrival patch, for the same reliability-under-fast-forward
reason) flips the flag to `true` once a `DaysPassed` threshold passes.
The existing, unmodified shop-refresh cycle should then start rolling
it in on its own — **not yet confirmed empirically that the flip alone
is picked up without a forced refresh; this is a real unverified
assumption, flagged for the first in-game test of this feature.** If it
doesn't work, the fallback is forcing a shop-inventory refresh call
after the flip (method not yet identified) rather than trying to patch
the shop-selection algorithm itself (its C# is `StringObfuscator.dll`
-obfuscated — a string-search-based approach to finding it directly
didn't work).

**How to flip a private-setter field from a Harmony patch:**
`DescriptionDef.Purchasable` is `{ get; private set; }` — can't be set
directly from external patch code (`mechDef.Description.Purchasable =
true` doesn't compile). Use Harmony's `Traverse` helper instead:
```csharp
Traverse.Create(mechDef.Description).Property("Purchasable").SetValue(true);
```
`DataManager.MechDefs` (an `IDataItemStore<string, MechDef>`, same
shape as the already-used `DataManager.SimGameEventDefs`) is the
accessor — `.Exists(id)` / `.Get(id)`, same pattern as every other
`DataManager`-backed lookup already used in this mod's patch.

## Getting stock 3D models into Blender — extraction required, and Blender isn't the bottleneck

Relevant once 3D asset work starts (currently deferred — everything so
far uses borrowed stock prefabs as placeholders).

**BattleTech's models aren't sitting as loose `.fbx`/`.obj` files.**
Same situation as the DLC assetbundle issue above — meshes are
serialized inside Unity's own compiled format. To study or extract a
stock model, an extraction tool is required first:

- **AssetRipper** (recommended) — extracts meshes *and* armatures
  (skeletons) *and* blend shapes together, to FBX/OBJ/glTF, preserving
  rigging and hierarchy. The only one of the two that's useful for
  seeing how a stock unit is actually rigged.
- **AssetStudio** — confirmed compatible with BattleTech's Unity
  version range, but mesh export is plain `.obj` with no rig data;
  rigged export only via the `Animator → FBX` path for objects with
  bound animation clips. Weaker fit for this specific purpose.

**One thing working in our favor:** BattleTech is a standard Mono
build, not IL2CPP — already established all session, since Harmony
patching against `Assembly-CSharp.dll` only works that way. IL2CPP is
the case that gives these extraction tools real trouble; we shouldn't
hit that.

**Blender is not the wrong tool and doesn't need replacing for Unity
work.** Unity has long had native `.blend` import (drop a `.blend`
into an Editor project's Assets folder, Unity auto-converts via
Blender's own FBX exporter under the hood) — but **that convenience
doesn't apply to modding an already-compiled game.** There's no open
Unity Editor project here to drop files into. The actual pipeline for
this project is: **model in Blender → export FBX → feed that FBX into
whatever tool ends up handling asset-bundle injection** (UABE or
similar) — same destination, different route than a from-scratch Unity
game would use.

**Known gotcha for when export happens:** Blender's default FBX
exporter and Unity's axis/scale conventions don't always align cleanly
out of the box (rotation-offset quirks are a known issue). Dedicated
Blender add-ons exist specifically for Unity-correct export settings —
worth grabbing one rather than fighting default settings by hand.

**Copyright boundary, same rule as reading RogueTech/BEX source
(see below):** opening extracted stock models in Blender to study
topology density, rig setup, or how damage-state variants are
structured is fine — the game is owned. **Reusing any extracted mesh
or rig data in the shipped mod would be copyright infringement.** Study
technique, build original geometry from what's learned.

## NEW STANDING REQUIREMENT (2026-08-29, corrected 2026-08-30): every
## MechDef needs the `unit_gundamuc` MechTag, or it won't show up in
## Skirmish anymore

**Correction, worth remembering the shape of:** the first version of
this patch targeted `BattleTech.UI.LanceConfigurator.Initialize`,
filtering its private `availableUnits`/`availableUnitsEnemy` fields via
Harmony `ref ___fieldName` injection. It compiled clean, deployed clean,
`ModTek.log` showed no error — and had **zero effect on Skirmish**,
because `LanceConfigurator` isn't Skirmish's class at all. Confirmed by
decompiling `LanceConfiguratorPanel` (which owns a `LanceConfigurator`
instance): it references `SimGameState`/`Contract`/`Barracks`, meaning
that whole class is Career mode's real mission-prep lance screen. Worse
than just "didn't work": that patch would have been silently filtering
stock 'Mechs out of Career's own real mission-prep screen too — an
unintended effect on actual gameplay, not just a harmless no-op. A
clean build and a clean deploy log are **not** confirmation a Harmony
patch targets the right class — only seeing the actual in-game effect
is.

**The real Skirmish class is `BattleTech.UI.SkirmishMechBayPanel`**
(`SetData` → `RequestResources` → async `LoadRequest` bulk-loads every
`MechDef` in the game into a private `stockMechs` list via a callback →
`RefreshMechList`, only ever called from that load-complete callback,
combines `stockMechs` + `customMechs` into the public `allMechs` field
and hands it to the mechbay widget). Note: `stockMechs` here means
"not built via the game's own in-UI Custom Mech editor" — both real
stock 'Mechs and this mod's own modded MechDefs land in it together,
since ModTek content is indistinguishable from stock at this layer.
`customMechs` is a completely different, unrelated system
(`ActiveOrDefaultSettings.CloudSettings.CustomUnitsAndLances`) — not
modded content.

The working patch: a **Prefix** on `SkirmishMechBayPanel
.RefreshMechList`, filtering the private `stockMechs` field in place.
Prefix (not Postfix) matters here — it needs to run before
`RefreshMechList` builds `allMechs` from `stockMechs`/hands it to the
widget, and since this method is only ever invoked post-load, there's
no async-timing risk the way there might have been patching something
mid-`LoadRequest`.

**Every MechDef this mod ships must include `"unit_gundamuc"` in its
`MechTags.items` array, or it silently stops appearing in Skirmish
entirely** — same silent-failure shape as the `unit_release` gotcha
above, just a different tag for a different reason. All 10 units built
so far (Guntank, Guncannon, Prototype Gundam, Zaku II, GM, Zaku I,
Gouf, Dom, Gundam Ground Type, Ez8) already carry it. Add it to the
per-unit build checklist alongside `unit_release`.

This is a blanket filter with no toggle — it hides every stock 'Mech
from the Skirmish mechbay unconditionally while this DLL is active, not
just during some debug mode. Easy to revert (delete the patch class)
once it's no longer wanted, but worth remembering it's there if
Skirmish ever seems to be "missing" stock content unexpectedly.

**Confirmed in-game (2026-08-30):** Skirmish mechbay shows exactly 10
'Mechs — precisely this mod's full built roster (Guntank, Guncannon,
Prototype Gundam, Zaku II, GM, Zaku I, Gouf, Dom, Gundam Ground Type,
Ez8), zero stock content. The `SkirmishMechBayPanel.RefreshMechList`
Prefix works correctly.

## CONFIRMED: BattleTech's native fixed-vs-swappable inventory flag, and
## how to restrict MechLab's swap picker to only this mod's own gear
## (17b's first real slice, 2026-08-30)

Researched directly (decompile + JSON survey + a read of RogueTech's
`CustomComponents.dll` as prior art, studied not copied) before building
the first real piece of the long-deferred 17b hand/hip-equip system.

**`IsFixed` is a real, first-class engine concept — not something we
have to build.** `BaseComponentRef.IsFixed` (bool) sits on every
inventory entry (`MechComponentRef`), confirmed both from a decompile
of `Assembly-CSharp.dll` and from real stock files:
`chassisdef_marauder_MAD-CM.json`'s `FixedEquipment` array has an entry
with `"IsFixed": true`; `mechdef_orion_ON1-K_fp_morganKell.json`'s
normal `inventory` entries all carry `"IsFixed": false`. **The vanilla
MechLab Customize UI already reads and enforces this** —
`MechLabItemSlotElement.SetDraggable(!componentRef.IsFixed)` and
`RemoveFromParent()` short-circuit on `IsFixed` — so making a slot
swappable vs. permanently locked needs zero new UI code, just setting
this one field correctly per inventory entry. Field position in the
JSON: sits between `HardpointSlot` and `GUID`.

**Structural components (Engine/Gyro/Cockpit/Actuators) were never a
concern here** — they're not `ComponentType` values at all
(`BattleTech.ComponentType` enum: `NotSet, Weapon, AmmunitionBox,
HeatSink, JumpJet, Upgrade, Special, MechPart`). They live as scalar
fields directly on `ChassisDef`, never as removable `Inventory` rows —
there's nothing to accidentally strip.

**Multiple items in one location can be independently fixed or not** —
confirmed via stock Atlas AS7-D's `LeftArm` (a weapon + 2 heat sinks
coexisting as separate `MechComponentRef` entries). Mutability is a
per-item property, not per-location.

**No "hip" or "back" location exists anywhere in this engine, full
stop.** `ChassisLocations` (`Assembly-CSharp.dll`) is a hardcoded
`[Flags]` enum with exactly 8 values: `Head, LeftArm, LeftTorso,
CenterTorso, RightTorso, RightArm, LeftLeg, RightLeg` (plus bitmask
helpers `All`/`Arms`/`Torso`/`MainBody`/`Legs` — no new physical slots).
Confirmed by scanning every `"Location":` value across every stock
`chassisdef_*.json` — nothing else appears. **Checked whether RogueTech
or BEX have ever solved this and they haven't**: RogueTech's Omni
support (`OmniMech`/`Gear_Gyro_Omnimech` in
`reference-mod/RogueTech-master/Eras/DarkAge3131-/Base/chassis/
chassisdef_vandal_LI-O.json`) marks individual `Hardpoints` entries
`"Omni": true` — still just the 8 standard locations, no new category.
RogueTech's `CustomComponents.dll` (`AddHardpoint`/`ReplaceHardpoint`)
adds *more hardpoints within* an existing location (capped at 4) — also
not a new location. BEX has nothing comparable at all. **A genuine
hip/back slot would mean a wholly new, unprecedented MechLab UI
category — not attempted this pass, per the user's own direction; they
intend to look for a different reference mod before this gets designed
for real.**

**Restricting the MechLab swap picker to only this mod's own weapons —
real, working prior art exists.** RogueTech's `CustomComponents.dll`
implements exactly this via an `IMechLabFilter` interface
(`bool CheckFilter(MechLabPanel panel)`) plus a Harmony postfix on
`MechLabInventoryWidget.ApplyFiltering`
(`MechLabInventoryWidget_ApplyFiltering_Patch`), deactivating any picker
item that fails the check. Confirmed via our own decompile that
`ApplyFiltering(bool refreshPositioning = true)` is a real *vanilla*
public method (not something RogueTech's own DLL invents) — iterates a
public `List<InventoryItemElement_NotListView> localInventory` and
`SetActive`s each one based on the player's filter-checkbox state.

**Implemented as `MechLabInventoryWidget_ApplyFiltering_Patch` in
`GundamUCArrivalPatch.cs`** (original code, technique only borrowed) —
a Postfix that, for every still-active item in `__instance
.localInventory`, resolves a `MechComponentDef` via whichever of
`item.controller.weaponDef` / `item.controller.componentDef` /
`item.weaponDef` is populated (confirmed exact field names from
decompiling `MechLabInventoryWidget.ApplyFiltering` and
`ApplyControllerFiltering`, and `BattleTech.UI
.ListElementController_BASE_NotListView`), and force-hides it if its
`ComponentType` is `Weapon` or `Upgrade` and its `ComponentTags`
(`TagSet`, same type/`.Contains()` API as `MechTags`/`ChassisTags`
already used elsewhere in this mod) doesn't contain the new
`gundamuc_weapon` tag. Deliberately fails open — ammo/heat sinks/jump
jets/mech parts, and anything it can't confidently resolve a def for,
are left untouched — since a false negative (an unwanted stock weapon
slips through) is cosmetic, while a false positive (hiding a
legitimate own-mod inventory row) would break the picker outright.

**New standing requirement: every WeaponDef and melee UpgradeDef this
mod ships needs `"gundamuc_weapon"` in `ComponentTags.items`**, or it
silently won't be selectable as a MechLab swap-in once any slot is
opened up — same silent-failure shape as the `unit_release`/
`unit_gundamuc` tag gotchas above. Add it to the per-item build
checklist going forward.

**Real gap this surfaced and fixed in passing:** none of this mod's 7
existing melee Upgrade items (Zaku II/I heat hawk, GM/Gouf/Dom/Ground
Type/Ez8 beam sabers/heat rod/heat saber) had `IsFixed` set at all
before this pass — meaning they were all technically swappable in
MechLab already, just never noticed since nobody had opened Customize
on them. Added `"IsFixed": true` to all 7, so "fixed weapon hardpoint"
(the design intent `02-FEATURE-LIST.md` already stated for most of this
roster) is now actually true, not just assumed. Existing fixed torso
weapons (Guncannon's cannon/gatling, GM's beam spray gun, both Zakus'
machine guns) were deliberately left untouched this pass — out of scope
per the approved plan, a known gap worth revisiting later.

## CONFIRMED: a real passive defense-multiplier stat exists (shield item),
## and duplicate passive Float_Add effects genuinely stack (arm-loss
## redundancy for the beam saber) — 2026-08-30, second pass

First in-game test of the hand-equip slice worked (beam rifle swappable,
confirmed by user), which surfaced two real gaps: no swappable shield
existed, and losing the LeftArm would silently delete the whole beam
saber bonus rather than degrading gracefully. Two things needed
confirming from real engine behavior before building either — this
project has been burned once already by inventing a field value that
wasn't a real enum member (the `WeaponSubType` freeze), so the same
discipline applied here.

**No armor-point stat exists at runtime — `CurrentArmor`/`AssignedArmor`
really is MechLab-only, as suspected.** But a real, shipped, item-
equippable passive defense stat does exist: `DamageReductionMultiplierAll`,
operation `Float_Multiply`, confirmed on real stock Upgrade components
(`data/upgrades/general/Gear_General_Advanced_Command_Module.json` and
its Lance/Company siblings, all `0.9`), and on `TraitDefEliteMechWarrior`/
`TraitDefVeteranMechWarrior` (also `0.9`) and `AbilityDefBWCL` (`0.8`).
Stock Command Modules use `effectTargetType: "AllLanceMates"` (lance-wide
buff); a self-only version uses `effectTargetType: "Creator"` instead —
same statName/operation, different target scope. Used this for the new
`Gear_Shield_ProtoGundam.json` (`0.85`, self-only, `-15%` damage taken)
— a real mechanic, not a literal "+X armor" number, but the closest
genuine equivalent this engine actually has.

**Duplicate passive `Float_Add` effects from two simultaneously-equipped
components DO stack additively — confirmed via decompile, not
assumed.** `BattleTech.EffectManager.CreateEffect` checks
`GetAllEffectsTargetingWithBaseID(target, effectData.Description.Id)` —
if the existing count for that specific effect ID is below
`durationData.stackLimit`, a brand new independent `StatisticEffect` is
added (`AddEffect`); only once the count reaches `stackLimit` does a new
application get deduped into a `RefreshEffect` (duration-only refresh,
no additional stat change). **`stackLimit` defaults to unset/`-1`, which
reads as "unlimited stacking," not "no stacking"** — the `> 0` check in
the cap logic fails for both `0` and `-1`. Every melee Upgrade item this
mod ships (`Gear_Actuator_Zaku_HeatHawk.json` and siblings) already has
`"stackLimit": -1` — confirmed via this research that two copies of the
same item DO sum their bonuses, not silently dedupe. Stock precedent for
deliberately preventing this: all three Command Modules set
`"stackLimit": 1` specifically to stop double-dipping.

**Superseded within the hour — the dual-arm split below was a stopgap,
not the real fix.** Initially considered mounting the same full-value
(`+85`) item in both arms with `stackLimit: 1` so only one copy's bonus
would ever "count" (rejected — the decompiled stacking logic confirms
creation-time dedup behavior, but doesn't establish what happens to an
already-deduped effect's lifecycle if its originating component gets
destroyed mid-battle, untested and risky), then shipped a halved-value
split instead (`85.0` → `42.5`, mounted once per arm, full bonus with
both arms intact, half survives losing either one). **The user
immediately correctly rejected this too** — losing an arm shouldn't
halve a beam saber's cutting power at all, and a real saber isn't
carried in one specific arm in the first place; it's racked centrally
and drawn by whichever hand is free. **Real fix: see 17f in
`02-FEATURE-LIST.md` — mount the item in `CenterTorso` instead of
either arm.** CenterTorso destruction is BattleTech's own coup-de-grace
location (the mech can't survive losing it), so anything mounted there
is functionally unlosable while the unit can still fight, at full
value, with zero new mechanism — Upgrade-type items already don't need
hardpoint-category legality to mount in a location with spare capacity
(heat sinks already do this in every unit's CenterTorso), and melee
Upgrade items already render nothing (empty `PrefabIdentifier`,
confirmed the generic melee prefab is what actually swings regardless
of which item boosts the damage) — so there was never anything to
solve visually either. `Gear_Actuator_ProtoGundam_BeamSaber.json` is
back to a single `+85` entry, mounted once, `CenterTorso`,
`IsFixed: true`. The dual-arm-split JSON shape (two inventory entries,
same `ComponentDefID`, halved `modValue`) stays documented here as a
real, working pattern in case it's ever needed for something that
*should* degrade with partial damage — just not this.

Both new items (`Gear_Shield_ProtoGundam.json`, and the re-mounted
`Gear_Actuator_ProtoGundam_BeamSaber.json` second copy) carry
`gundamuc_weapon` and slot into the already-built
`MechLabInventoryWidget_ApplyFiltering_Patch` filter with no code
changes — that patch already gates on `ComponentType.Upgrade` generically,
not on a hardcoded item list.

## CONFIRMED: real armor-weight-efficiency items need a genuine Harmony
## patch on vanilla's tonnage math — not the Upgrade+StatisticEffect
## trick everything else in this mod uses (2026-09-01)

User pushed back on an earlier claim that armor weight/points aren't
moddable — correctly. Two different mechanisms were being conflated:

- **Confirmed again, still true:** no `Tonnage`-named `statName` exists
  anywhere for the plain `StatisticEffect` system (grepped stock data
  and all of RogueTech, zero hits) — a passive Upgrade item genuinely
  cannot modify another item's or the mech's own tonnage this way.
- **What the user actually remembered is real, just a different
  mechanism:** RogueTech's Ferro-Fibrous armor
  (`reference-mod/RogueTech-master/Core/RogueModuleTech/Armors/
  Gear_Armor_FerroFibrous.json`) carries `"Custom": {"Weights":
  {"ArmorFactor": 0.89285}}` — a non-vanilla JSON block that only
  RogueTech's own compiled DLL knows how to read. Traced where the math
  actually happens (not in `CustomComponents.dll`, which turned out to
  be just a generic JSON-extension parser — the real logic is in a
  separate dependency, `MechEngineer.dll`, via a Harmony Prefix on
  `MechStatisticsRules.CalculateTonnage` that fully replaces the
  method).

**Confirmed via direct decompile (not just the research agent's
report) — the vanilla method itself:**
```csharp
// BattleTech.MechStatisticsRules.CalculateTonnage
maxValue = 100f;
currentValue = mechDef.Chassis.InitialTonnage;
float num = 0f; // sum of every location's Assigned(Rear)Armor
currentValue += num / (UnityGameInstance.BattleTechGame.MechStatisticsConstants.ARMOR_PER_TENTH_TON * 10f);
for (...) currentValue += mechDef.Inventory[i].Def.Tonnage;
```
One static method, no per-chassis variation. **Confirmed
`MechValidationRules.ValidateMechTonnage` calls this method directly**
(not a redundant reimplementation) — meaning a patch on
`CalculateTonnage` alone correctly fixes overweight/underweight
validation for free. **But `BattleTech.UI.MechLabMechInfoWidget
.CalculateTonnage` (private, UI-display-only) is a SEPARATE, fully
independent reimplementation of the identical formula** — confirmed by
reading its full decompiled body, not assumed from the rules method's
name alone. Leaving this one unpatched would show the player a stale
tonnage number even after the real validation was already correct.

**Implementation choice: Postfix, not a skip-original Prefix —
deliberately different from RogueTech's own approach, safer for this
project.** RogueTech's `MechEngineer.dll` fully replaces
`CalculateTonnage`'s body (`__runOriginal = false`). This mod's
`MechStatisticsRules_CalculateTonnage_ArmorWeight_Patch` and
`MechLabMechInfoWidget_CalculateTonnage_ArmorWeight_Patch` (both in
`GundamUCArrivalPatch.cs`) use ordinary Postfixes instead — every other
patch in this file already is one, and skip-original replacement was
never proven necessary here. The trick: the original method already
adds the FULL (undiscounted) armor tonnage into its result using the
exact same `ARMOR_PER_TENTH_TON` formula the Postfix also uses, so the
Postfix just subtracts that same amount back out and re-adds the
discounted version — mathematically identical to a full replacement,
but provably can't drift from vanilla's own math even if the constant
ever changes, since it's reusing vanilla's real computation rather than
re-deriving it independently.

**Data representation: a plain hardcoded `Dictionary<string, float>`
(`GundamUCArmorWeight.ArmorWeightFactors`) keyed by `ComponentDefID`,
not RogueTech's "Custom" JSON block.** Deliberately skips building any
generic JSON-extension-parsing framework — this mod doesn't need one
for one item, matching the same "small hardcoded block over a
generalized framework until a 3rd/4th thing needs it" discipline
already used for the GM date-gate patch. If multiple armor-weight items
somehow get equipped simultaneously, `GetArmorWeightFactor` takes the
BEST (lowest) factor found rather than stacking them — armor grades are
alternative choices, not additive upgrades, unlike the melee/shield
items where deliberate stacking behavior was already reasoned through.

First real item: `Gear_Armor_LunaTitanium_Improved.json` (`-10%` armor
weight, `CenterTorso`, swappable). Not yet tested in-game.

**Separately caught and fixed while building this:** the three
reactor-tier items from the previous pass
(`Gear_Engine_ReactorTuning.json` and its two `ReactorCore_HighOutput*`
siblings) existed on disk and were previously reported as registered,
but were actually missing from `mod.json`'s `Manifest` array entirely —
a real gap, not just a doc-sync issue, since ModTek only loads what's
declared there. Fixed alongside the new armor item. Worth a quick
`mod.json` sanity pass after any batch of file creation, not just
trusting the earlier edit succeeded.

## CONFIRMED: a reward-delivered item can come back permanently
## grey/undraggable in MechLab — vanilla widget-pool bug, not anything
## in this mod's JSON (2026-09-05)

Spent most of a session chasing this on the Hyper Bazooka before
finding the real cause. Symptom: item delivered via a flashpoint/story
`ItemCollectionDef` loot reward showed up in the MechLab inventory
sidebar permanently greyed out and completely undraggable (`OnBeginDrag`
never even fired) — while the exact same item sold normally from the
Store's Sell tab, proving SimGame ownership was never the problem.

**False leads tried first, all confirmed to have zero effect — don't
repeat these:** `AllowedLocations` restriction, `PrefabIdentifier`
swap, `ammoCategoryID` change, flipping `Description.Purchasable` to
match other working items. None of these touch the actual mechanism.

**Real root cause, confirmed via direct decompile of
`BattleTech.UI.RewardsPopup` and `InventoryItemElement_NotListView`:**
`RewardsPopup.CreateItemEntry` explicitly locks every reward-preview
widget it creates (`SetItemButtonClickable(isClickable: false)`, which
sets `HBSButtonBase.IsLocked = true` on the widget's button component)
so the preview isn't interactive. All item widgets across the entire
game — MechLab inventory, Store, and reward previews alike — are pulled
from one shared `DataManager` GameObject pool keyed only by prefab name
(`ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView`),
with no separation between "locked preview" and "normal inventory"
consumers. **The actual gap:**
`InventoryItemElement_NotListView.SetData` (both overloads — confirmed
by reading both bodies) unconditionally calls `SetDraggable(true)` when
binding a widget to real data, but neither overload calls the matching
`SetClickable(true)`, which is the only method that clears
`IsLocked`. `MechLabInventoryWidget`'s own item-creation path never
calls `SetClickable` either (grepped, zero hits). So any pooled widget
instance that was ever locked by a reward preview stays locked forever,
for whichever unlucky future consumer's item happens to reuse that
exact GameObject.

**A first fix attempt (Prefix on `RewardsPopup.ClearItems()`, unlocking
every controller in `AllSalvageControllers` before its buggy direct
`dm.PoolGameObject` call) was insufficient and shipped a false
confidence.** Root cause: `RewardsPopup.OnClose()` (fired when the
player clicks "Complete") never calls `ClearItems()` at all — it only
pools the popup panel itself via the base `UIModule.Pool()`.
`ClearItems()` only runs at the *start* of the next `PopulateItems()`
call, meaning a single-flashpoint-then-check-MechLab test never
exercises that patch at all. Left in place anyway (harmless, correct
for the multi-popup-reuse case) but **the actual fix** is a Postfix on
both `InventoryItemElement_NotListView.SetData` overloads (found via
`TargetMethods()` filtering by name, to dodge exact-overload signature
matching) that calls `__instance.SetClickable(true)` — this closes the
whole bug class regardless of what poisoned the pooled instance, not
just this one flashpoint's reward.

**Lesson: when a symptom is "works everywhere except one specific UI
list, and only for reward-delivered items," suspect the shared
`DataManager` GameObject pool before touching any of the item's own
JSON.** None of the four JSON-side attempts could ever have worked —
the bug lived entirely in vanilla UI code with no data-level trigger.

**Separately found and fixed in the same pass, different bug:** the
Handheld-panel relocate sweep
(`MechLabPanel_LoadMech_StorageItems_Patch.SweepWidgetForHandheldItems`)
corrected a source arm widget's `usedSlots` when pulling an item out,
but never its separate private per-category hardpoint counters
(`currentEnergyCount`/`currentBallisticCount`/`currentMissileCount`/
`currentSmallCount`, confirmed via decompile of
`MechLabLocationWidget`) — so the arm widget kept believing its
hardpoint was still occupied and silently rejected re-adding that same
weapon. Fixed by calling the widget's own public `RefreshHardpointData()`
after the sweep, which recomputes all four counts straight from
`localInventory` — the same method the widget calls internally after
any normal add/remove.

## Environment details (relevant if debugging recurs)

- BattleTech (Steam), ModTek v4.5.0 (confirmed stable — same bug
  reproduced on the "4.5.1" build too, so this isn't a version-specific
  regression)
- Owned DLC content packs: Flashpoint, Urban Warfare, Heavy Metal
- Suspected (not confirmed) connection between the sparse-merge bug and
  DLC content-pack MDD patching (`PatchMDD for content pack items added
  by mods` in logs) — worth keeping in mind if new, different indexing
  crashes appear later.
