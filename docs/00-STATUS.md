# Current Status — Read This First

## Step 6 slice 3 — MECHANICAL hour granularity, BUILT, NOT YET TESTED (2026-09-17)

**Important correction to what v0.0.1 actually delivered.** Research on the
24-hour question surfaced that slices 1-2 produced a *display* clock only:
with `HoursPerTick = 6`, the suppressed ticks did nothing at all, so
`DaysPassed`, travel, repairs, injuries and events still resolved once per
day. The sim was mechanically identical to vanilla, just rendering an hour
and running 4x slower in wall-clock. That could never deliver the actual
goal (sortie more than once a day; pilot rest in hours instead of eating a
week of a year-long war). User confirmed full mechanical granularity is
what's wanted.

**Built this slice:**
- `HoursPerTick` 6 → 1, paired with `DayElapseTimeNormal` 1.25 → 0.3 and
  `DayElapseTimeFast` 0.33 → 0.1 in this mod's `SimGameConstants.json`.
  These two MUST move together — the thresholds must stay above one frame
  or time passage becomes frame-rate dependent (see `03-TECHNICAL-NOTES.md`).
  0.1s is ~6 frames at 60fps, ~3 at 30fps. Day pacing lands at 7.2s
  (normal) / 2.4s (fast), close to the previous build's 5s.
- `GundamUCClock.AdvanceHourlySystems()` now runs on every suppressed tick:
  `TravelManager.OnDayPassed()`, `UpdateInjuries()`,
  `UpdateMechLabWorkQueue()`. All three verified public via decompile, all
  three are self-contained "one unit of work per call" logic.
- Deliberately NOT advanced hourly: date, events, milestones, contract
  expiry, Flashpoint gating, finances — those are genuinely day-scale and
  firing them 24x more often would spam events and expire contracts far
  too fast.

**Balance reinterpretation, deliberate:** per-call rates are unchanged, so
costs authored as "days" now resolve in that many HOURS. A 6-day pilot
recovery becomes 6 hours; a 5-day refit becomes 5 hours. That IS the
feature. Travel collapses the same way (3-day transit → 3 hours); if that
feels too fast the lever is scaling travel costs in
`GetInSystemTransitTime`/`StarSystemNode.Cost`, not the hourly method.

**Known cosmetic side effect:** `SGTimeGreebleAnimator.TimePerDay` is set
once from `DayElapseTimeNormal` at Init, so the day-pip sweep now completes
in one tick rather than sweeping across a day. (Vanilla already had this
mismatch in fast-forward.) Not addressed yet.

**Needs verification:** pilots heal in hours not days, repairs complete in
hours, travel still arrives correctly, and — critically — events/contracts/
Flashpoints/the Gundam arrival still fire at the correct *day* cadence and
are NOT sped up 24x.

## Step 6 slices 1+2 VERIFIED IN-GAME — tagged v0.0.1 (2026-09-16)

Confirmed working in a real career: the tick advances in 6-hour steps
(00:00 → 06:00 → 12:00 → 18:00), the date rolls over correctly at 00:00,
and the clock reads e.g. `00:00   31 DECEMBER, UC 0078` on one line with
the backing plate sized to match.

Two UI issues found and fixed during verification:
1. The full date string wrapped, dropping the year onto the DayPips row.
   Fixed by disabling word wrapping (the real guarantee) and sizing the
   rect from TMP's own `preferredWidth` rather than a guessed constant.
2. The dark backing plate didn't grow with the text. It is not a
   serialized field on `SGTimePlayPause` (confirmed via decompile), so it
   had to be reached by walking the live hierarchy — the label's parent
   turned out to BE the plate, so widening the parent by the same delta
   tracks it correctly.

**Still open for Step 6 (so this is 0.0.1, not 0.1.0 — the roadmap point
is NOT complete):**
- **True 24-hour clock.** Currently 6-hour granularity, forced by the
  frame-rate constraint documented in `03-TECHNICAL-NOTES.md`. Next task.
- **Night missions** — the actual gameplay payoff of the whole step.
  Requires a Harmony override of `LineOfSight`'s spotter/sensor-range
  methods driven by our hour clock (RogueTech's LowVisibility pattern).
  Not started.

## Roadmap Step 6 slice 1 — hour-granular time (2026-09-16)

First slice of the time-granularity rework. Two research passes preceded
any code (see `03-TECHNICAL-NOTES.md` for the full writeup), and they
changed the plan twice for good reasons:

1. **The roadmap's assumed hook was wrong.**
   `SpottingVisibilityMultiplier`/`Absolute` are NOT fields in
   `SimGameConstants` — they're per-actor stealth properties on
   `AbstractActor`, not a global day/night dial. Corrected in the notes.
   The real night-mission lever is a Harmony override of `LineOfSight`'s
   spotter/sensor-range methods (the approach RogueTech's LowVisibility
   mod uses). That's slice 3 work, not built yet.
2. **A naive "1 hour per tick" would have silently broken.** Dividing the
   stock `DayElapseTime*` constants by 24 puts the fast threshold below a
   single 60fps frame, which — combined with `Update()`'s hard
   `realTimeElapsed = 0f` reset — would have made campaign time passage
   frame-rate dependent. Caught before building. Ticks are 6 in-fiction
   hours instead, with the stock constants left untouched.

**Built:** `GundamUCClock` (hour-of-day state via CompanyStats, matching
the existing `GundamUC_*` convention) and
`SimGameState_OnDayPassed_HourClock_Patch` — a Prefix that counts each
stock tick as 6 hours and only lets the untouched original `OnDayPassed`
body run once 24 accumulate. **This is the only skip-original Prefix in
the mod**; the reasoning and the "don't copy this casually" warning are
documented in `03-TECHNICAL-NOTES.md`. The debug `timeSkip` lump path is
explicitly passed through and documented as a known intentional gap.

**Nothing else changed** — no JSON edits, and no changes to travel, the
repair queue, injuries, Flashpoint gating, or the existing Gundam-arrival
patches, all of which inherit the new granularity automatically because
they already do one unit of work per tick rather than N per N days.

**Expected in-game:** days should now take ~4× longer in real time (4
ticks per day instead of 1) at both normal and fast speed. That is the
intended, easily-tunable consequence of this slice, not a bug — pacing
gets restored via the `DayElapseTime*` constants once the mechanism is
proven.

**Needs verification before slice 2:** travel countdown, repair/refit
paydown, pilot injury healing, Flashpoint gating, and above all the
Prototype Gundam arrival event must all still fire correctly across
several in-game days. Slice 2 (visible hour readout on `SGTimePlayPause`)
does not start until this is confirmed.


## CONFIRMED WORKING: all three starting units, full career battle, won (2026-09-11)

User tested all three fixes from this session's final pass in a real
Career battle and won. Confirmed working: Handheld panels correct and
per-mech (no more cross-mech leaking) for Gundam, Guncannon, and Guntank;
Gundam's hand-weapon exclusivity system works in live combat (switched
between Beam Rifle and Hyper Bazooka mid-battle via the HUD toggle).
Diagnostic `Debug.Log` calls added during the previous debugging pass have
been removed now that the fix is verified (no behavior change, DLL
rebuilt and redeployed).

**New polish item, explicitly deferred, not started:** user flagged that a
weapon's HUD slot icon looks identical whether it's simply out of ammo or
its mounted location has been destroyed — potentially confusing, since
those are very different situations (reload/switch weapons vs. permanently
lost). Also explicitly flagged: current weapon tonnage values (Beam Rifle
5t, Bazooka 3.5t, Field Rifle 3.9t, Guntank's trimmed cannons) are a first
pass to fit under each chassis's weight cap, not a final balance pass —
user said "we can rebalance later," meaning these numbers are expected to
be revisited, not treated as final.

## Handheld panel invisibility — REAL root cause finally confirmed via log data, not guessed (2026-09-11, sixth pass)

Three prior fix attempts this same day (ClearInventory, destroy-old-widget,
reorder+SetAsLastSibling) were all reasonable theories that turned out
wrong or incomplete — none of them were verified against actual runtime
data first. Added `Debug.Log` calls to both `InitWidgets` and `LoadMech`'s
Postfixes (active state, sibling index, item count, computed
`anchoredPosition`) and had the user reproduce the bug once more, then read
`output_log.txt` directly (`%USERPROFILE%\AppData\LocalLow\Harebrained
Schemes\BATTLETECH\output_log.txt` — this game uses the older Unity
`output_log.txt` name, not `Player.log`). The logged data showed the
**exact** cause immediately: the widget's `activeSelf`/`activeInHierarchy`
were `true` and item counts were correct in every single case — the ONLY
thing that ever differed between a working and a broken run was
`anchoredPosition`: `(0, 112)` on the very first `InitWidgets` call ever
made in a MechLab session (confirmed visible), and `(0, 482)` on every
call after that, regardless of which mech (confirmed present, active,
correctly populated, but never visible again — pushed off-screen).

Root cause: the position formula was
`leftArmWidget.rect.height + 200f`. `leftArmWidget`'s `RectTransform.rect`
reads a small/not-yet-laid-out value on the very first `InitWidgets` call
in a fresh scene (before Unity's layout system has completed its first
pass), then its true, much larger settled height on every subsequent
call — a ~370-unit jump that pushes the panel off the visible screen area
every time after the first. Fixed by dropping the unstable `rect.height`
term entirely, using a flat `200f` offset instead. The three earlier
"fixes" (data-clear, destroy-ordering, sibling-order) were not wrong to
try given the information available at the time, but none of them touched
the actual variable that mattered — this is the concrete lesson: when a
bug is this stubborn across multiple reasonable-seeming fixes, add logging
and read the real runtime log before attempting a fourth blind change.

**Not yet re-tested in-game** — needs a full restart. Diagnostic logging
left in place for this next verification pass (harmless — visible only in
`output_log.txt`, easy to strip once confirmed fixed).

## Handheld widget existed and held the right data but wasn't visually rendering — reordered (2026-09-11, fifth pass)

Third iteration on the same underlying issue. After the "destroy old widget
first" fix, user's screenshot showed real progress: Gundam's tonnage
(59.99/60) correctly included Beam Saber + Bazooka, and Left Arm's own real
widget correctly showed only the Shield (Beam Saber genuinely swept away) —
so the sweep/data logic was fully correct. But no Handheld panel was
visible on screen at all, and the same was reported for Guncannon's Field
Rifle. Diagnosis: destroying the previous mech's widget BEFORE creating
today's one meant that if anything about the destroy went wrong, or even
just due to both clones briefly coexisting at the identical computed
screen position in the same frame, the old (undestroyed) one could win the
sibling-order race and render on top — panel logically present, visually
invisible. Restructured `MechLabPanel_InitWidgets_StoragePanel_Patch`:
capture the old widget reference up front but don't touch it yet; fully
build, position, `Init()`, and register today's widget FIRST; explicitly
call `SetAsLastSibling()` so it can never lose a render-order race
regardless of what else still exists under the shared parent; only THEN
destroy the previous widget, in its own try/catch, once it's no longer
possible for that cleanup to affect anything about the current mech's
panel. **Not yet re-tested in-game** — needs a full restart.

## Handheld panel still leaked across mechs after the ClearInventory fix — real root cause found (2026-09-11, fourth pass)

User re-reported the exact same symptom after the earlier `ClearInventory()`
fix (Bazooka showing in Guncannon's Handheld panel; Guncannon's own new
Field Rifle not showing in its own). That fix was real but insufficient —
turned out to be treating a symptom, not the cause. Re-verified via direct
decompile rather than re-guessing: `MechLabPanel.InitWidgets()` is **not**
called once per panel lifetime as originally assumed — it's called from
`MechLabPanel.SetData()`, confirmed to fire again on every mech switch
within the same MechLab session. So `MechLabPanel_InitWidgets_StoragePanel_Patch`
also re-runs on every switch, and it was creating a brand-new widget clone
each time via raw `UnityEngine.Object.Instantiate` (confirmed NOT pooled —
ruled out a RewardsPopup-style shared-pool repeat directly via decompile)
and overwriting `GundamUCHipHolder.Widgets[__instance]` with the new one —
but never destroying the OLD mech's widget GameObject first. Both clones
get positioned at identical screen coordinates every time (computed the
same way relative to `leftArmWidget`), so the stale, undestroyed one could
end up rendering on top depending on sibling order, showing whichever
mech was viewed previously instead of the one actually loaded. Fixed by
destroying any existing tracked widget for that exact panel instance at
the top of `InitWidgets`' Postfix, before creating the new one — the
earlier `ClearInventory()` call in the `LoadMech` patch is left in place
(harmless, still correct for its own reasoning) but this is the real fix.
**Not yet re-tested in-game** — needs a full restart.

## Guncannon rifle + Guntank weight audit — all 3 starting units now weight-balanced (2026-09-11, third pass)

**Guncannon**: added the handheld rifle its own chassis flavor text already
promised ("arm-mounted hardpoints reserved for a handheld rifle not yet
fielded") — `Weapon_Ballistic_FieldRifle-Guncannon.json`, a deliberately
generic/low-power AC/2-tier weapon (Damage 20, MaxRange 400, Tonnage 3.9,
InventorySize 2), `AllowedLocations:"Arms"`, tagged `gundamuc_weapon` +
`gundamuc_storage` + `gundamuc_requireshand` so it participates in the same
Handheld-panel/hand-exclusivity systems as Gundam's gear. Mounted by
default on RightArm (swappable), with an AC/2 ammo box in RightTorso.
Registered in `mod.json`. Guncannon's pre-existing loadout (Vulcan,
Low-Recoil Cannon, Gatling Gun, ammo, heat sinks) was already computed at
~65.06/70 tons before this addition — the new rifle+ammo (4.9t) was sized
specifically to use nearly all of the remaining budget, landing the full
default loadout at **69.96/70 tons**.

**Guntank**: no hands (`ChassisTags` empty, confirmed), so no Handheld
system involvement — this pass was pure ammo/weight audit. Ammo was
already correct (2× MG ammo boxes correctly feed both Quad Autocannons +
the Vulcan since all three share the MG ammo category; 2× AC/10 ammo boxes
feed the twin Large-Caliber Cannons) — nothing to fix there. **Real,
previously-undiscovered problem found while computing its actual total**:
Guntank's default loadout came out to **~100.19 tons on an 80-ton
chassis** — over by more than 20 tons, never caught before because nobody
had run the actual `CalculateTonnage` math against it until this audit.
Root cause: twin Large-Caliber Cannons at 13t each (26t) and twin Quad
Autocannons at 8t each (16t) were simply never weight-checked when
originally built. Trimmed proportionally — Large-Caliber Cannon 13t→6.7t,
Quad Autocannon 8t→4.1t — landing the full default loadout at
**79.79/80 tons**.

**All three starting units now land close to their chassis cap with a
small safety margin, computed via the confirmed real tonnage formula (not
estimated):** Prototype Gundam 59.99/60, Guncannon 69.96/70, Guntank
79.79/80.

**Not yet tested in-game at all** — this whole pass (Guncannon's new rifle,
both weight audits) needs a full game restart (DLL unchanged this round,
but JSON needs a fresh load) and a real MechLab/Skirmish check per unit:
confirmed tonnage readout matches these numbers, the new Field Rifle
appears in Guncannon's Handheld panel and fires correctly, and no location
overflows its `InventorySlots` (Guncannon's RightArm now holds Field
Rifle(2)+Ammo would-be-in-RightTorso — confirm no slot conflicts on load).

## Handheld capacity was way too generous, plus final Gundam weight pass (2026-09-11, second pass)

Direct user correction: Handheld's fake hardpoint totals (2 Ballistic + 2
Energy + 1 Missile + 1 Support = room for up to 6 ranged items) let the
player load far more ranged weapons into the panel than "one hand" should
ever hold — should be exactly 1 melee (Beam Saber) + 1 ranged backup
(Bazooka), nothing else. Root cause of the *over*-generosity: Beam Rifle
was still tagged `gundamuc_storage`, meaning the primary arm weapon was
ALSO being swept into Handheld alongside the actual backup gear — no
longer necessary now that the drag/lock/hardpoint-bookkeeping bugs that
originally justified rerouting it are all fixed; it just lives on its own
real arm widget like any normal weapon now. Fixed:
- Removed `gundamuc_storage` from Beam Rifle.
- `totalBallisticHardpoints` 2→1 (Bazooka's category, the only one left
  with anything to hold), Energy/Missile/Support all→0.
- `maxSlots` 20→4 (exactly Beam Saber's 1 slot + Bazooka's InventorySize).
- Bazooka `InventorySize` 4→3 to fit the new tight budget and match the
  user's stated "2-3 slots" expectation for a ranged backup weapon.

**Final weight pass**, now that ammo (added earlier this session — AC/20
box in RightTorso, MG box in CenterTorso) is confirmed present: retuned
Tonnage across the 4 non-stock items to land the fully-equipped default
loadout almost exactly at the 60-ton cap rather than comfortably under it,
per explicit user direction ("keep it at 60 tonne") — Beam Rifle 5t, Bazooka
3.5t, Beam Saber 1.3t, Shield 1t (plus fixed Vulcan 0.5t + 2×1t ammo boxes =
13.3t total gear). Confirmed via the real `CalculateTonnage` formula
(base `InitialTonnage` 31t + armor tonnage, back-derived as a fixed 15.69t
discounted contribution from the 1395-point armor loadout via Luna
Titanium's 0.9 factor — unchanged by any of today's edits) that this lands
at 59.99 tons.

**Explicitly deferred, in the user's own stated order — not started:** a
generic low-power rifle for Proto Guncannon (its own flavor text already
sets this up: "arm-mounted hardpoints reserved for a handheld rifle not
yet fielded"), followed by its own weight-to-max balance pass, then
Guntank last.

## Handheld panel leaked items across every mech in the roster — fixed (2026-09-11)

Direct user report: they owned exactly one Hyper Bazooka, equipped it on
their Gundam, then found every Guncannon in the roster also showing it
equipped. Real bug, not a scope/exclusivity question. Root cause: the
fabricated Handheld widget (`GundamUCHipHolder.Widgets`) lives for the life
of the `MechLabPanel` instance, not per-mech — switching mechs in the same
MechLab session re-runs `LoadMech` on the SAME widget, but
`MechLabPanel_LoadMech_StorageItems_Patch`'s sweep only ever ADDED items to
its `localInventory`, never cleared stale entries left over from whichever
mech was viewed previously. Every real location widget gets torn down and
rebuilt fresh each `LoadMech` call automatically; this fabricated one never
did, because nothing vanilla knows it exists. Fixed with one line —
`storageWidget.ClearInventory()`, a real public vanilla method (confirmed
via decompile, `MechLabLocationWidget.cs:237`) that properly unparents and
pools each item's GameObject and empties the list — called unconditionally
at the top of the Postfix, before the hands-eligibility gate. Safe and
non-lossy: each real location widget (leftArm/rightArm/centerTorso) is
already rebuilt from the currently-loaded MechDef's true saved inventory
before this Postfix runs, so the very next line's sweep immediately
re-populates the Handheld panel correctly from *this* mech's real data —
nothing is lost, it just no longer persists across a mech switch.

**Separately, while investigating:** confirmed Prototype Gundam's own
default `inventory` didn't include the Hyper Bazooka or ammo for either the
Bazooka or the head Vulcan at all — it arrived only via a loose loot-table
grant, not equipped. Added Bazooka (LeftArm, swappable, alongside Beam
Saber + Shield — 7/8 slots used), one AC/20 ammo box (RightTorso), and one
MG ammo box (CenterTorso) directly to the MechDef. Re-trimmed Bazooka/Beam
Rifle/Shield tonnage (4t/3.5t/1t) to absorb the added ammo weight and keep
the fully-equipped default loadout under the 60-ton cap (~59.19t).

**Real, separate, NOT fixed — flagged, not yet decided:** the MechLab
swap-picker filter only checks "is this any of this mod's own gear"
(`gundamuc_weapon` tag), not "is this item meant for this specific unit."
Guncannon has real Ballistic hardpoints on both arms plus
`unit_humanoidHands`, so dragging the Bazooka onto it in MechLab succeeds —
and the same is already true of every other item in the mod (a Zaku Heat
Hawk could go on a GM today). This is a pre-existing, general design
question, not something this fix touches — user hasn't decided whether to
keep it as a shared parts pool or build per-unit restriction.

## Hand-weapon exclusivity system — built, NOT yet tested in-game (2026-09-10)

Real turn-based combat mechanic, not a MechLab equip restriction: Prototype
Gundam carries Beam Rifle, Hyper Bazooka, and a melee Beam Saber, but only
one can be firing from the hand at a time, and none work with both arms
destroyed. Followed two research passes (RogueTech's LAM confirmed NOT a
relevant precedent — a heavyweight dual-3D-model swap system; RogueTech's
CustomAmmoCategories mod's `blockWeaponsInInstalledLocation` mechanic on
`Weapon.HasAmmo` confirmed as the right, lightweight, already-proven
pattern — see `03-TECHNICAL-NOTES.md` for full citations).

**Design (user-specified):** the arm-mounted weapon is active by default at
battle start; committing to a melee attack auto-claims the hand (no manual
step) as long as an arm survives; switching between the two ranged options
needs an explicit player click on that weapon's HUD slot.

**Built in `GundamUCArrivalPatch.cs`:**
- New `gundamuc_requireshand` ComponentTag on Beam Rifle, Bazooka, Beam
  Saber.
- `GundamUCHandWeapon` static helper — tracks the active slot as a custom
  `StatCollection` string stat on the mech (`GundamUC_ActiveHandSlot`),
  lazily defaulted on first read to whichever hand-required Weapon is
  mounted on a real arm.
- `Weapon_HasAmmo_HandExclusivity_Patch` (Postfix on `Weapon.HasAmmo`
  getter) — the actual fire-eligibility gate; forces `false` if neither arm
  survives, or if this weapon isn't the currently active slot.
- `MechMeleeSequence_OnAdded_HandSwitch_Patch` (Postfix) — flips the active
  slot to melee the instant a melee attack is committed, provided the mech
  carries any hand-required item and has a functional arm.
- `CombatHUDWeaponSlot_OnPointerUp_HandToggle_Patch` (Prefix, not
  skip-original) — clicking a benched hand-weapon's HUD slot switches the
  active slot before vanilla's own click-handler runs, so its own
  enable-for-firing logic succeeds immediately afterward instead of needing
  a second click.

**Known nuance, deliberately not addressed:** vanilla's own melee
eligibility (`MeleeRules.GetValidMeleeAttackTypes`) only checks the mech's
one chassis-designated punching arm, not "either arm" — so if only the
*non*-designated arm survives, vanilla itself will already refuse to offer
a melee attack at all, before this system is ever consulted. Not patched
around, to keep this pass's scope to the exclusivity mechanic itself.

**Not yet tested in-game at all.** Next step: confirm in a real Skirmish —
default active weapon at battle start, manual toggle between Beam
Rifle/Bazooka via HUD click, melee auto-switch, and full lockout once both
arms are destroyed.

## Hand-equip system, first slice (2026-08-30)

The dedicated 17b design session finally happened. Three rounds of
engine research (two Explore agents on the base game, one on RogueTech/
BEX as prior art) established the real ground truth:

- BattleTech already has a native fixed-vs-swappable flag on inventory
  items (`IsFixed`, confirmed via decompile), and the vanilla MechLab
  Customize UI already respects it — blocks drag/removal automatically.
  No new UI code needed at all for the lock/unlock mechanic itself.
- No "hip" or "back" location exists anywhere in this engine
  (`ChassisLocations` is a hardcoded 8-value enum), and neither
  RogueTech nor BEX has ever added a new slot category beyond those 8 —
  only more hardpoints *within* them (RogueTech's `CustomComponents`
  `AddHardpoint`). **Per the user's explicit direction, hip/back is
  deferred** until they find a different reference mod that's actually
  solved this — not designed blind this pass.
- Restricting swap slots to only this mod's own gear has real working
  prior art: RogueTech's `IMechLabFilter` + a Harmony postfix on
  `MechLabInventoryWidget.ApplyFiltering`. Confirmed that method is real
  in this game's own `Assembly-CSharp.dll` too, not just RogueTech's DLL.

**Built this pass** (Prototype Gundam only, as the proof-of-concept —
scope explicitly not rolled out to the other 8 humanoid units yet):
- New `gundamuc_weapon` `ComponentTags` convention, added to every
  WeaponDef/melee UpgradeDef this mod ships (18 existing files + the 2
  new ones below) — new standing per-item build requirement alongside
  `unit_release`/`unit_gundamuc`.
- **Real gap found and closed in passing:** none of this mod's existing
  melee Upgrade items (Zaku II/I's heat hawk, GM's/Gouf's/Dom's/Ground
  Type's/Ez8's melee weapons) had `IsFixed` set at all — meaning they
  were all technically swappable in MechLab already, just never
  noticed. Added `"IsFixed": true` to all 7 existing MechDefs' melee
  inventory entries so "fixed" actually means fixed, matching the
  design intent `02-FEATURE-LIST.md` already stated.
- New `Weapon_Energy_BeamRifle-ProtoGundam.json` (Gundam's actual beam
  rifle — the unit previously had only a head Vulcan; the "four items"
  description in `02-FEATURE-LIST.md` never matched real data) and
  `Gear_Actuator_ProtoGundam_BeamSaber.json` (+85 melee, its own real
  beam saber — the GM's beam saber was explicitly written as a
  simplified descendant of this one).
- Prototype Gundam's MechDef: RightArm beam rifle (`IsFixed: false` —
  the swap-enabled slot), LeftArm beam saber (`IsFixed: true`, matches
  every other unit's melee pattern).
- New `MechLabInventoryWidget_ApplyFiltering_Patch` in
  `GundamUCArrivalPatch.cs` — Postfix hiding any Weapon/Upgrade picker
  item lacking `gundamuc_weapon` from the MechLab Customize sidebar.
  Fails open (doesn't hide) on anything it can't confidently resolve to
  a def — ammo/heat sinks/jump jets/mech parts untouched.

DLL rebuilt clean (`dotnet build`, 0 warnings/errors), all touched/new
JSON validated. **Not yet tested in-game** — next session should open
Prototype Gundam's Customize screen and confirm (1) the beam rifle is
draggable/swappable and the beam saber shows the fixed-equipment lock
overlay, (2) only `gundamuc_weapon`-tagged items appear as legal swap
options, (3) a real Skirmish battle to confirm the beam rifle fires and
the beam saber's melee bonus applies. Full design rationale in
`03-TECHNICAL-NOTES.md`.

Explicitly NOT in this pass: hip/back slot system (blocked on further
reference-mod research), Torso-as-reactor/internal-equipment reflavor
(a separate, undesigned system per the user's own note), rolling this
out to the other 8 humanoid units.

**Cross-check note (chat session, 2026-08-30):** the "further
reference-mod research" plan above is now resolved — a chat-side Nexus
search independently reached the identical conclusion (no BattleTech
mod, ever, has added a body location beyond the stock 8) and the
**decision has already been made**: build hip/back as a **pure data
slot**, not a rendered hardpoint at all — see `02-FEATURE-LIST.md` 17f
for full reasoning. This sidesteps the missing-attachment-point problem
entirely rather than solving it, and doesn't need a rigging solution to
exist first. **Don't spend another session searching for a
reference-mod rendering solution — that search is done, the answer was
"nobody has," and the design already routed around it.**

**First in-game test (2026-08-30): beam rifle swap confirmed working.**
User also flagged the beam rifle's 34 damage feeling weak against a
Zaku (165 armor + 100 structure survives a direct hit) — acknowledged,
explicit rebalancing-later item, not touched this pass.

**Follow-up same day: swappable shield added, and the beam saber now
survives losing one arm.** Two real gaps from the first test:
1. No swappable shield existed. Researched the real defense mechanism
   (no armor-point stat is runtime-modifiable; `DamageReductionMultiplierAll`
   / `Float_Multiply` is the real, stock-precedented equivalent — see
   `03-TECHNICAL-NOTES.md`). New `Gear_Shield_ProtoGundam.json`
   (`-15%` damage taken, self-only), mounted LeftArm, `IsFixed: false`.
2. Losing the LeftArm would have deleted the beam saber's entire `+85`
   bonus outright. Researched whether duplicating the item on both arms
   would double-stack (confirmed via decompile: yes, unless capped) and
   whether a stackLimit-based "only one copy counts" trick would survive
   losing the *originating* arm (genuinely unconfirmed — not risked).
   Went with the provably-safe option instead:
   `Gear_Actuator_ProtoGundam_BeamSaber.json`'s bonus halved to `+42.5`
   and mounted once per arm (both `IsFixed: true`) — full `+85` with
   both arms intact, `+42.5` survives losing either one alone, relying
   only on the already-proven "destroyed location disables its own
   components" mechanic every other unit's melee item already uses.

Both new items tagged `gundamuc_weapon`, both validated, no DLL changes
needed (the existing MechLab filter patch gates generically on
`ComponentType`, not a hardcoded list). **Not yet re-tested in-game.**

**Rollout completed + Luna Titanium made default (2026-09-01):** two
asks in one message. (1) Luna Titanium (Refined) added to Prototype
Gundam's own MechDef as a default-equipped, `IsFixed: true` item —
matches how the Vulcan and beam saber are already treated on this unit
(a core identity trait, not just a purchasable option other units also
have access to). (2) The Storage-panel treatment (CenterTorso mount +
`gundamuc_storage` tag) rolled out to all 6 remaining melee items
(Zaku II/I's shared heat hawk, GM's beam saber, Gouf's heat rod, Dom's
heat saber, Ground Type/Ez8's shared beam saber) — the "natural,
low-risk follow-up" flagged multiple times finally done. **Real
capacity problem caught before it shipped:** every one of these
melee items was still `InventorySize: 3`; moving them into a 4-slot
CenterTorso already holding 2-3 heat sinks would have overflowed on
every single unit (checked exact slot math per unit, all 7 would have
exceeded capacity). Reduced all 5 shared/per-unit melee item files to
`InventorySize: 1`, matching the value already used for Gundam's own
saber — same fix, same reasoning, just not yet applied to the others
until now. No new C# needed for the rollout itself — the existing
`MechLabPanel_LoadMech_StorageItems_Patch` already generically handles
any `gundamuc_storage`-tagged CenterTorso item, not just Gundam's.
All touched JSON validated. Not yet tested in-game.

**Resolved same session:** the 17f gap above is now written up in
`02-FEATURE-LIST.md`. User's answer: hip/back storage means a slot
that's immune to being lost when one arm is destroyed and needs no
generated/rendered hardpoint — "pure data slot" was the concept, the
mechanism was ours to find. **Real fix: mount in `CenterTorso`.**
CenterTorso destruction is the coup-de-grace location (mech can't
survive losing it), so an item mounted there is functionally unlosable
while the unit can still fight — costs nothing new mechanically
(Upgrade items already mount in CenterTorso with spare capacity, same
as every unit's heat sinks; melee Upgrade items already render nothing
regardless of location). Also: shield's `-15%` damage-taken multiplier
confirmed as the accepted stopgap for "armor" until/unless a better
mechanism turns up — no action needed there, just a known limitation.

`Gear_Actuator_ProtoGundam_BeamSaber.json` is back to a single `+85`
entry (the interim two-arm-half-value split from the prior entry above
was superseded within the hour, not shipped) — one `CenterTorso`
inventory entry, `IsFixed: true`, full bonus, never degrades from arm
loss. Validated, no DLL changes needed. **Not yet re-tested in-game.**
Natural follow-up, not yet done: the same single-arm fragility exists
on all 7 other units with melee items (Zaku II/I, GM, Gouf, Dom, Ground
Type, Ez8) — moving them to CenterTorso too is cheap and consistent,
flagged in `02-FEATURE-LIST.md` 17f rather than done silently since it
touches every already-shipped unit at once.

**Superseded almost immediately — user wants CenterTorso reserved for
a future engine/reactor equipment system, not used as melee storage.**
Real ask: a genuinely new "Storage" side panel in the MechLab UI,
separate from the 8 real location widgets, immune to combat
destruction. Full research + design in `02-FEATURE-LIST.md` 17f
(rewritten) and the plan file — confirmed via decompile that
`MechLabLocationWidget` is its own independently-loadable prefab, not
fused into `MechLabPanel`, so it can be cloned at runtime with no new
asset-authoring work; `Init()`/`SetData()` are separate and only
`SetData()` needs a real `ChassisLocations` value, so a clone that only
ever gets `Init()` is "purely storage" by construction.

**Step 1 built (2026-08-31): panel placement only, no items yet.** New
`MechLabPanel_InitWidgets_StoragePanel_Patch` in
`GundamUCArrivalPatch.cs` — clones `centerTorsoWidget`'s GameObject,
relabels it "Storage," hides its armor/structure/hardpoint/damage UI
elements (nothing to show yet), positions it below the CenterTorso
widget (a first-guess offset — this project has no way to inspect the
live scene's actual layout system from decompile alone). DLL rebuilt
clean, deployed. **Not yet tested in-game — this is the checkpoint.**
Look at Prototype Gundam's Customize screen: does a "Storage" panel
appear, correctly positioned, without breaking the existing 8 widgets?
Also worth trying (not just looking): hovering/clicking on the new
panel, since it was never given `SetData()` and some of
`MechLabLocationWidget`'s pointer/drop-handler code paths weren't
individually checked for null-safety against a missing `loadout` —
watch `ModTek.log` for exceptions if it looks or behaves oddly. Step 2
(real item display, `gundamuc_storage` tag, hiding storage items from
CenterTorso's own widget) is intentionally not built yet — gated on
this checkpoint per the plan.

**Scare, diagnosed, and Step 1 rebuilt (2026-08-31):** user reported all
Career menu UI gone after the above. Reverted the patch immediately as
a precaution, then checked `ModTek.log` directly — zero exceptions,
and no evidence `InitWidgets` (this patch's hook) had even fired yet.
The screenshot was BattleTech's own normal new-Career establishing
shot, not a crash; UI loaded fine moments later. Not caused by this
patch. Rebuilt anyway with real improvements rather than just
redeploying the same code: researched RogueTech's own MechLab
layout-fix system (`CustomFilters.dll`, `MechLabScrolling`) for its
proven technique — never hand-compute `anchoredPosition`, always
`SetParent(parent, false)` (omitting `false` is a documented cause of
broken UI placement), rely on layout systems over manual math where
one exists. Applied: explicit `SetParent(..., false)`, zero computed
offset (clone placed at its source's exact `anchoredPosition` — an
identity copy, per the user's own suggestion, so it currently overlaps
`centerTorsoWidget` rather than sitting beside it — expected, real
placement is later), `LayoutRebuilder.MarkLayoutForRebuild` on the
shared parent as cheap insurance. DLL rebuilt clean, deployed.
**Checkpoint again:** confirm this loads without incident and the
relabeled "Storage" panel appears (overlapping CenterTorso's, for now)
before any further iteration.

**User confirmed stable, flagged the overlap looked wrong, spotted
real open space near LeftArm (2026-08-31).** Direct in-game visual
feedback the static field dump couldn't have found. Take 3: clone
`leftArmWidget` instead of `centerTorsoWidget`, offset upward by the
source's own height + 15px margin instead of an identity copy — lands
in that open space rather than overlapping anything. Best-effort guess
at the exact margin (no live-scene access); reparenting technique
(`SetParent(parent, false)`, `LayoutRebuilder.MarkLayoutForRebuild`)
unchanged since that part already loaded cleanly twice. DLL rebuilt,
deployed. **Checkpoint again.**

**Take 4 (2026-08-31):** still a sliver of overlap with LeftArm's real
panel at +100px — bumped to +200px. Bigger finding: the individual
per-field hides (armorBar/rearArmorBar/structureText/hardpoints) did
NOT actually take effect in-game — a fully interactive "120/120" armor
slider with working +/- buttons rendered inside the cloned panel
regardless. Root cause not confirmed. Rather than keep guessing at
which specific field reference is wrong, added a more robust sweep:
walk every descendant of the panel except the `inventoryParent` subtree
(so real/future item content is never touched even if a name collides)
and hide anything whose GameObject name contains armor/structure/
hardpoint/quirk/damaged/destroyed — name-based, not dependent on
getting an exact field reference right. Also repositioned
`inventoryParent` up to `anchoredPosition.y = -40` to close the gap
left behind rather than leaving dead space. DLL rebuilt, deployed.
**Checkpoint again** — this is the 4th visual round-trip on this one
panel; expected given zero live-scene access, but worth remembering
Step 2 (real items) is still fully gated on this looking right first.

**Take 5 (2026-08-31): panel fully separated, no overlap, no leftover
armor/hardpoint UI — the name-based sweep worked.** User confirmed
"better" and asked for two more things: shrink the panel from its
inherited ~10-row height to 4-5 rows, and size items appropriately —
beam saber as 1 slot, beam rifle as 4 slots and mechanically mimicking
a real PPC (or a stronger "Beam Magnum"-tier version). Done:
- `storageRect.sizeDelta.y` halved, with `anchoredPosition` compensated
  by half the height reduction to try to keep the already-correct top
  edge from shifting (unconfirmed whether the panel's pivot is centered
  — another guess, flagged as such).
- `Gear_Actuator_ProtoGundam_BeamSaber.json`: `InventorySize` 3 → 1.
- `Weapon_Energy_BeamRifle-ProtoGundam.json`: rebuilt against real
  stock PPC data (`Weapon_PPC_PPC_0-STOCK.json`,
  `Weapon_PPC_PPCER_2-TiegartMagnum.json` — checked, not invented) —
  `WeaponSubType: "PPCER"`, `PrefabIdentifier: "PPC"`, `InventorySize: 4`,
  Damage 75/HeatGenerated 42/MaxRange 600 (between stock PPC and the
  "Magnum" ER variant, slightly exceeding both per "more powerful"),
  and genuinely copied the real PPC's on-hit `AbilityDefPPC` SENSORS
  IMPAIRED status effect — mechanically mimics a PPC, not just
  numerically. DLL + JSON rebuilt, validated, deployed. **Checkpoint
  again.**

**Take 6 (2026-08-31): the sizeDelta shrink had zero visible effect —
confirmed by the user on a full restart, new career, still 8 rows.**
Root cause found by decompile: the striped "empty slot" rows aren't
discrete GameObjects gated by `maxSlots` (only ever set inside
`SetData()`, never called on this widget) — they're a static background
image, and whether it's anchored to stretch with its parent (which
would make resizing the parent actually crop it) isn't something a C#
decompiler can see at all — that's serialized prefab data, not code.
Fix: added a `RectMask2D` component directly to the panel. This clips
all child content to the panel's own rect regardless of the children's
own anchor/size configuration — sidesteps needing to know the
background's actual setup, the standard Unity technique for "crop to
this boundary no matter what's inside." Kept the existing 50% sizeDelta
shrink as the boundary the mask now enforces. DLL rebuilt, deployed.
**Checkpoint again** — 6th visual round-trip on this one panel; each
round has been finding a real, previously-invisible-from-decompile fact
about this specific prefab, not just retrying the same guess.

**Take 7 (2026-08-31):** user reported a brief cosmetic flicker on
first mechbay load (confirmed low-concern, not a repeat of the earlier
scare) and confirmed still 8 rows on a fresh new career. Real suspect
found: this patch's own `LayoutRebuilder.MarkLayoutForRebuild
(parentRect)` call (added earlier as "cheap insurance" in case
`sharedParent` has a LayoutGroup managing all 8 real widgets' uniform
sizing) would immediately have that LayoutGroup re-assert its own
sizing on the clone too, silently undoing the sizeDelta shrink right
after it's set — likely self-inflicted, not a wrong theory about the
background image after all. Fix: added a `LayoutElement` component
with `ignoreLayout = true`, telling any parent LayoutGroup to skip this
child entirely so manual size/position values actually stick. Kept the
RectMask2D from take 6 (harmless, still correct in principle even if
it wasn't the actual blocker). DLL rebuilt, deployed. **Checkpoint
again.**

**Take 8 (2026-08-31): still 8 rows, confirmed by user — three separate
sizing techniques (sizeDelta shrink, RectMask2D, LayoutElement
.ignoreLayout) against an arm-sized clone all had zero visible effect.**
User's own idea, tried here instead of continuing to fight whatever
kept overriding the resize: Prototype Gundam's `chassisdef_
protogundam_RX-78-1.json` has `LeftLeg` at `InventorySlots: 4` vs.
`LeftArm`'s 8 — clone `leftLegWidget` instead (for its naturally
smaller proportions) while still positioning relative to
`leftArmWidget` (the open screen space already confirmed correct
across takes 3-7). Removed the forced 0.5x sizeDelta shrink for this
test — want to see LeftLeg's unmodified natural size first, not
compound two untested size changes at once. Kept RectMask2D and
LayoutElement.ignoreLayout as harmless insurance. DLL rebuilt,
deployed. **Checkpoint again** — if this doesn't land it either, next
real diagnostic step is checking `ModTek.log`/a debug overlay for the
resolved `sizeDelta`/`anchorMin`/`anchorMax` values directly rather
than continuing to guess blind.

**User confirmed take 8 looks good.** Panel sizing/placement is done.

**Step 2 built (2026-08-31): real items now move into Storage, not
just the empty shell.** New `MechLabPanel_LoadMech_StorageItems_Patch`
— confirmed via decompile that `MechLabPanel.LoadMech` is what actually
calls `SetData` on all 8 real widgets with the mech's real per-location
data (`InitWidgets` never does this). Postfix there finds any item in
`centerTorsoWidget`'s real inventory tagged `gundamuc_storage` and
moves it into the Storage panel. Real items stay mounted at
`CenterTorso` in the MechDef (matches 17f's coup-de-grace reasoning,
unchanged) — only which widget displays/handles them changes.
**Important correctness detail caught before shipping a subtly-broken
version:** `MechLabItemSlotElement` tracks its logical owner via a
private `dropParent` field, completely independent of the Unity
Transform hierarchy — a bare `transform.SetParent()` would have moved
the item visually while every drag/remove/tooltip interaction kept
routing back to CenterTorso underneath. Used the item's own public
`SetData(componentRef, mountedLocation, dataManager, dropParent)` to
properly rebind ownership, in addition to reparenting.
`Gear_Actuator_ProtoGundam_BeamSaber.json` tagged `gundamuc_storage`.
**That gap was real, not just theoretical — fixed 2026-09-02.** User
directly reported the beam saber still visually showing inside
CenterTorso despite "moving" to what's now called the Hip Holder panel
(renamed from "Storage" on-screen 2026-09-01, same underlying
mechanism). Root cause was exactly the flagged gap: the item's
GameObject got reparented and its drag/drop ownership rebound, but it
was never actually removed from CenterTorso's own `localInventory`
list, so CenterTorso's widget still considered it present. Fixed:
matching items are now identified in a read-only first pass (mutating
`localInventory` while enumerating it throws
`InvalidOperationException`), then in a second pass genuinely
`Remove()`d from CenterTorso's list, its `usedSlots` decremented to
match, and — new — also `Add()`ed into Hip Holder's own
`localInventory` (needed for that panel to correctly support dragging
the item back out later, not just displaying it). Rolled out to all 7
melee-carrying units at once, same generic patch. DLL rebuilt,
deployed. **Not yet tested in-game.**

**That fix alone wasn't enough — user tested and reported items still
in CenterTorso, Hip Holder completely empty (screenshot, 2026-09-02).**
Real second bug, different from the usedSlots one: the `LoadMech` patch
looked up the Hip Holder widget via
`centerTorsoWidget.transform.parent.Find("GundamUC_StorageWidget")` —
but `InitWidgets` actually parented the widget under `leftArmWidget`'s
parent, not CenterTorso's. If those two widgets don't share a direct
parent in the real UI hierarchy (never actually confirmed either way),
that lookup silently finds nothing and the ENTIRE patch becomes a
no-op — matches the screenshot exactly (Hip Holder renders, correctly
positioned and labeled, but nothing ever moves into it). Rather than
guess at the shared-parent assumption a second time: removed the
re-derivation entirely. New `GundamUCHipHolder.Widgets` — a
`Dictionary<MechLabPanel, MechLabLocationWidget>`, same
state-tracking-dictionary pattern already studied from RogueTech's
`MechLabFixStateTracker` earlier this session — set once in
`InitWidgets` when the widget is created, looked up directly by panel
instance in `LoadMech`. No hierarchy assumptions left in this path at
all. DLL rebuilt, deployed. **Not yet tested in-game — this is the
real test of whether the underlying move-logic (fixed above) actually
works, since the previous test never got past this lookup bug to
exercise it.**

**Confirmed working (2026-09-03):** user's screenshot shows the beam
saber genuinely inside the Hip Holder panel now — the dictionary-lookup
fix resolved it. Flagged, not yet fixed: the item's row doesn't quite
align with the striped background under it — cosmetic, deferred until
there's more than one item in there to judge the alignment against
rather than guessing at another pixel offset blind.

**Built for a real interactive test: Hyper Bazooka
(`Weapon_Ballistic_HyperBazooka-ProtoGundam.json`).** User wants to
verify drag/drop actually works, not just that items can display in
Hip Holder — asked for a real second item delivered alongside the
Gundam so they can manually try moving it themselves. Real stock AC/20
(`Weapon_Autocannon_AC20_0-STOCK.json`: Damage 100, HeatGenerated 24,
InventorySize 4, Tonnage 14) used as the reference baseline, same
discipline as every other weapon this mod ships — Hyper Bazooka lands
close (Damage 105, HeatGenerated 20, Tonnage 12, "Project V" prototype
flavor for the modest edge). Tagged both `gundamuc_weapon` and
`gundamuc_storage` — first weapon (not melee) to carry the storage tag,
confirms the category rule from 17f ("melee + backup guns" both belong
in Hip Holder) actually works end to end, not just in theory.

**Threaded through the existing Gundam-arrival loot table, not just
made purchasable** — matches what was actually asked for ("when we get
the Gundam, also get a hyper bazooka"), not a shop-only item.
`itemCollection_table_gundamArrival.csv` gained 3 new guaranteed rows
(the bazooka + 2x `Ammo_AmmunitionBox_Generic_AC20`, so it's actually
usable in a real battle, not just a drag/drop test), and
`itemCollection_loot_gundamArrival.csv`'s request count went from 1 to
4 to match. **Real unverified assumption, flagged rather than assumed
safe:** the original single-entry/Count-1 shape is confirmed-working
(the Gundam itself has delivered reliably all session), but a
multi-entry/Count-N draw was never independently tested before now —
assumed to mean "draw all N entries without replacement," matching
ordinary loot-table semantics, but not confirmed. Worst case if wrong
is a missing/duplicated reward, not a crash — real checkpoint is the
next fresh-Career Gundam-arrival test actually granting all 4 items.

**That assumption was wrong — user tested and got only AC/20 ammo, no
Gundam, no Bazooka (screenshot, 2026-09-03). Real regression: this
broke the core, already-confirmed-working Gundam delivery.** Cramming
4 entries into the one table (`itemCollection_table_gundamArrival`)
referenced with Count 4 did NOT mean "guarantee all 4" — it turned the
table into a weighted random pool, and since 2 of the 4 entries were
duplicate ammo rows, ammo had double the draw odds of either the
Gundam or the Bazooka. **Fixed properly this time, against real
precedent, not another guess:** found stock BattleTech's own
`itemCollection_loot_ItemDouble_rare.csv` ("This cache contains two
rare items") — it uses two separate top-level Reference rows, each
pointing at its own dedicated single-item table, not one table with
multiple entries. Restructured to match: reverted
`itemCollection_table_gundamArrival.csv` to its original Gundam-only
content (untouched, confirmed-working shape restored), added two new
single-item tables (`itemCollection_table_gundamArrival_Bazooka.csv`,
`itemCollection_table_gundamArrival_Ammo.csv`), each referenced by its
own new row in `itemCollection_loot_gundamArrival.csv` — same shape as
the proven Gundam row, twice more. Registered in `mod.json`, validated.
**Not yet tested in-game — this is the real checkpoint**, since the
previous "should work" attempt also looked reasonable right up until
it didn't.

**Confirmed working (2026-09-03) — screenshot shows Gundam, Beam
Saber, Hyper Bazooka, and Shield all correctly delivered/placed.**
Real follow-up bug: `AllowedLocations: "All"` (copied from stock AC/20)
let the player drag the Hyper Bazooka into RightTorso, not just a hand
— same issue found on the Beam Rifle (identical copy-paste origin,
fixed proactively without waiting for a separate report). Both changed
to `AllowedLocations: "Arms"`. **Confirmed this doesn't conflict with
Hip Holder delivery** — this exact pattern (`AllowedLocations: "Arms"`
on an item that still sits at `CenterTorso` via direct MechDef/loot
data) is already proven working by every melee item in this mod:
`AllowedLocations` only governs PLAYER drag-drop legality in the
MechLab UI, not data-authored placement, which bypasses it entirely.
So the loot-delivered Bazooka still correctly lands at CenterTorso/Hip
Holder, while a player-driven drag now correctly only allows moving it
into an arm to actually use it — matches "equippable in the hand only"
exactly. No DLL changes needed, pure JSON fix.

**Superseded by a real redesign, not another patch (2026-09-03).**
After the AllowedLocations/PrefabIdentifier fixes still didn't resolve
the Bazooka drag bug, and a further research pass ruled out the most
likely vanilla code path entirely (`DropParent` assignment is
unconditional, not gated by ammo/PrefabIdentifier anywhere), pivoted to
what the user actually asked for: a genuine unified **Handheld**
category for anything a hand carries, replacing the plan to force
ranged weapons onto real Arm widgets. Full design in the plan file
history and `02-FEATURE-LIST.md` 17g. Phase 1 built:
- **Handheld widget made a real drop target.** Confirmed via decompile
  `ValidateAdd` throws on this clone's null `loadout` (`Init()` never
  calls `SetData()`). Fix: `loadout`, `totalBallisticHardpoints`,
  `totalEnergyHardpoints`, etc. are all PUBLIC fields (confirmed from
  the class dump) — plain assignment, not Traverse, for everything
  except the private `maxSlots`. `loadout.Location: CenterTorso` is
  deliberate: `RefreshMechComponentData` writes this same value back
  onto a dropped item's real `MountedLocation` on success (confirmed
  via decompile), keeping player-driven drops consistent with this
  mod's own JSON-authored placement.
- **Eligibility gate.** Reused the already-existing `unit_humanoidHands`
  ChassisTag (9/10 chassis have it, Guntank correctly doesn't) — the
  `LoadMech` patch now hides the Handheld panel's GameObject entirely
  for hand-less chassis. Simple visibility gate, no deep validation
  needed since there's nothing to drop into by construction.
- **Renamed "Hip Holder" → "Handheld."**
- **Beam Rifle and Hyper Bazooka re-backed at CenterTorso**, matching
  the beam saber's existing pattern — `AllowedLocations` changed to
  `"CenterTorso"` on both (no longer `"Arms"`), both tagged
  `gundamuc_storage`, Beam Rifle's MechDef entry moved from `RightArm`
  to `CenterTorso`. Bazooka's `PrefabIdentifier` reverted to the honest
  `"AC20"` — the AC10 swap was based on a hypothesis this research
  superseded, no reason to keep an inaccurate visual now that it's
  never mounted on a real chassis widget.
- **Real capacity fix caught in the process:** CenterTorso's default
  4 slots can't fit Beam Saber(1) + Luna Titanium(1) + Beam Rifle(4) =
  6. Bumped Prototype Gundam's own `InventorySlots` at CenterTorso from
  4 to 10 (chassis-specific, not a mod-wide change) — reasonable given
  it's the flagship unit meant to carry more there than a mass-produced
  Zaku.

DLL rebuilt clean, all JSON validated, deployed. **Not yet tested
in-game — this is the real checkpoint for the whole redesign.**
Specifically: does the Bazooka actually drag into Handheld now without
throwing, does the panel correctly disappear for Guntank, do all 3
items still work correctly in a real battle. Phase 2 (weapon stays
usable with only one arm surviving — a genuinely new combat-logic
requirement, no research started) is intentionally not part of this
pass — needs its own dedicated investigation once Phase 1 is confirmed
working.

**Confirmed working (2026-09-04) — Bazooka drags into Handheld
successfully.** Real architectural refinement immediately followed,
not a bug report: user pointed out Handheld should be conceptually an
*extension* of the arm, not a replacement for it — the real
`MountedLocation` needs to stay an actual arm so a future custom 3D
model attaches in the right place (can't swap which location a model
renders from after the fact; CenterTorso would be anatomically wrong
for something held in a hand). Reworked:
- Handheld widget's fabricated `loadout.Location` changed from
  `CenterTorso` to `RightArm` (`CurrentInternalStructure: 50`, matching
  Gundam's real RightArm value) — still bypasses whatever's actually
  broken in the real arm widgets' own drop-handling (the original,
  never-conclusively-diagnosed bug), just writes anatomically-correct
  data on a successful drop instead of CenterTorso.
- All 3 handheld items' `AllowedLocations` reverted from `"CenterTorso"`
  back to `"Arms"`. Bazooka's `PrefabIdentifier` stays `"AC20"` (already
  reverted last pass). Beam Rifle's MechDef entry moved back to
  `RightArm`, beam saber's back to `LeftArm` — both restored to their
  original, most natural placement.
- **The relocate-sweep (`MechLabPanel_LoadMech_StorageItems_Patch`)
  had to be generalized**, not just the data: it only ever swept
  `centerTorsoWidget.localInventory` before. Refactored into a shared
  `SweepWidgetForHandheldItems` helper, now called against
  `leftArmWidget`, `rightArmWidget`, and `centerTorsoWidget` — items
  are now genuinely arm-mounted, so that's where each real widget would
  otherwise display them if not swept into Handheld. Each item's own
  real `MountedLocation` (`item.ComponentRef.MountedLocation`) is now
  passed through instead of hardcoding `CenterTorso`, so the sweep
  stays correct regardless of which specific location an item is
  actually at.
- **Accepted trade-off, not a regression:** this reintroduces normal
  BattleTech "lose that arm, lose the weapon" behavior until Phase 2 is
  built — matches the user's own framing of Phase 2 as the mechanism
  that will later grant survival with either arm intact, not something
  Phase 1 needed to fake in the meantime.
- Gundam's CenterTorso `InventorySlots` bump (4→10, made last pass to
  fit all 3 items there) left as-is rather than reverted — only Luna
  Titanium lives there now, but the extra headroom is harmless and may
  suit future internal-equipment items.

DLL rebuilt clean, all JSON validated, deployed. Not yet re-tested
in-game with this specific rework — the Bazooka's drag success was
confirmed under the previous (CenterTorso-backed) version; this pass
changes *where* a successful drop writes its data, worth confirming
that part specifically still works.

**That still wasn't the full fix — user reported the Bazooka completely
inert to drag from inventory (no visual feedback at all), while every
other weapon dragged fine (2026-09-03).** Isolated via direct
questions rather than guessing blind again: Bazooka-specific, not a
general patch problem. Traced the vanilla drag-gate
(`InventoryItemElement_NotListView.OnBeginDrag`: `if (AllowDrag &&
...)`) but found nothing in vanilla code or this mod's own patches that
obviously explains a false `AllowDrag` — no exception in `ModTek.log`
either (load-time only, doesn't capture a live UI interaction anyway).
**Real root cause, found by re-checking this project's own established
`HardpointDataDef` gotcha rather than a new guess:** read
`hardpointdatadef_dragon.json` directly (Gundam's donor chassis for
borrowed prefabs) — RightArm has real ballistic prefab coverage
(`ac10`/`ac5`/`ac2`/`lbx`/`uac5`/`gauss`), but **no `ac20` entry at
either arm at all** — AC20 coverage only exists at LeftTorso on this
chassis. The Bazooka's `PrefabIdentifier: "AC20"` combined with the
just-added `AllowedLocations: "Arms"` restriction meant there was
genuinely no valid visual prefab for the only legal placement location
— consistent with a silent, no-feedback drag failure, matching "same
category coverage varies per exact location" already learned from the
original Guntank ballistic-hardpoint research this whole project
started with. **Fix:** `PrefabIdentifier` changed to `"AC10"`
(confirmed real Arm coverage) — `WeaponSubType`/`ammoCategoryID`/
`Damage`/every actual game-mechanic field left untouched, so the
weapon still fires as a real AC20-tier hit, just visually borrows the
AC10 prop (this mod has no custom 3D assets at all yet, so a borrowed-
prefab visual mismatch is already the established norm, not a new
compromise). **Not fully proven, evidence-based not confirmed** — the
data gap lines up exactly with the symptom and matches a precedented
failure class, but the exact C# code path from "missing prefab
lookup" to "OnBeginDrag no-ops" wasn't traced line-by-line. Real
checkpoint is whether the Bazooka actually drags now.

**User also confirmed the forward plan, not a new build request:** a
future Hyper Bazooka (Prototype Gundam backup weapon) belongs in Hip
Holder too, matching the "melee + backup guns" category rule already
locked in `02-FEATURE-LIST.md` 17f — nothing to build yet, no unit has
a distinct backup gun today.

**Deferred, not attempted this pass — flagged rather than guessed
at:** user also asked (1) whether the real Head widget could be
shrunk the same way (~4 lines) and (2) about a future
per-unit-dependent Cockpit-equipment concept (Sazabi-style head cockpit
vs. the torso default). Both hold real open risk worth a second look
before building blind: Head is a live functional widget handling the
Vulcan gun's actual equip/remove, not a disposable clone — the exact
resize techniques that failed 3 times against the Storage clone before
LeftLeg's natural sizing fixed it were never confirmed to be *safe*
against a real widget, only that they didn't visibly work. And Cockpit
isn't an inventory-equippable `ComponentType` in this engine at all
(confirmed earlier this session — Engine/Gyro/Cockpit/Actuators are
scalar `ChassisDef` fields, never removable inventory rows in vanilla)
— a location-dependent Cockpit-equipment system would be new,
unprecedented territory, not a small follow-up.

---

Last updated: Claude Code session, 2026-08-20 — **Guntank and Guncannon
are confirmed working in-game** (Step 2 checkpoint from `01-ROADMAP.md`
achieved), **and Step 3's Career-mode roster lock is confirmed working
too**: a real Career start produced exactly 4x Guncannon + 1x Guntank,
no randomization. Both roadmap checkpoints now genuinely met, not just
believed-correct-on-paper. **Step 4 (Prototype Gundam arrival) is now
fully built, including Prototype Gundam's MechDef — ready to test.**

Getting here took four load-test/diagnose/fix cycles in one session,
three real bugs found via `ModTek.log` analysis:
1. Invalid `WeaponSubType` enum values on all 5 custom WeaponDefs (froze
   the intro video — silently-caught MDD indexing exception, same
   failure class as the prior session's sparse-JSON bug).
2. `reference-mod/` (BEX + RogueTech, kept for study) was living inside
   `Mods/` and got loaded as active content by ModTek's recursive scan
   — moved it to the BattleTech root, outside `Mods/` entirely.
3. Both MechDefs were missing the `unit_release` tag required for
   Skirmish mechbay visibility — found by pattern-matching against all
   104 stock MechDefs, not a log line, but confirmed correct by this
   retest.

All three fully documented in `03-TECHNICAL-NOTES.md` — read it before
writing the next MechDef, since #1 and #3 are easy to repeat otherwise.


## Done
- **Step 3 roster lock implemented** (per the expanded Step 3 spec in
  `01-ROADMAP.md`): Career mode's starting 5-'Mech roster is now fixed
  at 1x commander Guncannon + 3x Guncannon + 1x Guntank (`StartingLance`
  in `SimGameConstants.json`), `StartWithRandomMechs` set to `false`,
  and the player-facing "Randomize starting 'Mechs" difficulty toggle
  hidden/locked (`Visible`/`Toggle`: false, `DefaultIndex`: 0 in
  `CareerDifficultySettings.json`) rather than just defaulted off —
  confirmed deliberate per user, since this is meant to be the fixed
  narrative start for now, not a player option (a random/era-relevant-MS
  toggle is planned for later once the mod supports starting at other
  dates). Both files are full-copy overrides wired into `mod.json` as
  `ShouldMergeJSON: true` (overriding existing stock IDs, unlike our
  brand-new unit files). See `03-TECHNICAL-NOTES.md` for a real gotcha
  hit while doing this: the stock `SimGameConstants.json` isn't actually
  strict JSON (comments, trailing commas, one missing comma) — fixed to
  strict-valid while copying rather than preserved verbatim.
  **Confirmed in-game (2026-08-20):** a real Career start produced
  exactly 4x Guncannon + 1x Guntank, no randomization, no toggle visible
  in the setup screen. Step 3 roster lock genuinely done, not just
  believed-correct.
  - **Side confirmation from this test:** Guncannon shows under its max
    tonnage in the mechbay (~5 tons short) — expected and correct, not a
    bug. Matches the known, already-documented gap: LeftArm/RightArm are
    intentionally unequipped pending the 17b handheld rifle+shield
    system (see the Guncannon MS entry). Useful data point for later:
    the game does track/display empty-hardpoint tonnage as literal
    unused capacity in the mechbay UI, which is exactly what the 17b
    rifle+shield work will need to fill in.
  - **Still open, not yet decided/built:** locking the starting
    *planet* (`CareerMode.StartingSystems`, currently the 8 stock
    systems) — separate from the mech-roster lock, not yet resolved
    (see the L1 home-base design note in `02-FEATURE-LIST.md` 17a,
    itself deferred pending the Lagrange node-graph system). Also the
    hard space-deployment gate (Guntank literally can't deploy in space)
    is confirmed **deferred to a soft narrative rule** for now per user
    — the real mechanical gate waits until the space battlefield system
    exists.
  - **Correctly not tested:** the player did not advance time to check
    for the Prototype Gundam delivery event, since that's Step 4 and
    isn't scripted yet — nothing to see there, correct call.
- ModTek pipeline fully validated (see 03-TECHNICAL-NOTES.md for the
  gotchas that cost real debugging time — read this before touching any
  JSON, it'll save you from repeating the same mistakes).
- Design fully locked for the OYW era — see 02-FEATURE-LIST.md for every
  system decision (skills/Newtype, ejection, morale, Minovsky, basing,
  funds, salvage, ship field-repair, etc.)
- Three starting-roster ChassisDef + MovementCapDef pairs written and
  believed structurally correct (based on real stock-file schemas, not
  guessed):
  - `chassisdef_guntank_RTX-65` — stationary fire base, no melee, medium
    armor, borrows Cataphract's prefab (reassigned from Awesome — see
    "Resolved" below)
  - `chassisdef_guncannon_RCX-76-02` — fire support, thin armor
    (historically accurate — got wiped out 12-0 at Mare Smythii), borrows
    Cataphract's prefab
  - `chassisdef_protogundam_RX-78-1` — tanky (Luna Titanium), fast,
    jump-capable, strong melee, borrows Dragon's prefab
- Five fixed WeaponDef files written for Guntank/Guncannon's buildable
  hardpoints (`StreamingAssets/data/weapon/`): Large-Caliber Cannon and
  Quad Autocannon (Guntank), Low-Recoil Cannon and Gatling Gun
  (Guncannon), and a shared Vulcan Gun (Head, all three Federation
  units). Not yet wired into any MechDef or mod.json manifest.
- `unit_humanoidHands` ChassisTag added to Guncannon and Prototype
  Gundam (not Guntank) — marks hand/finger articulation for future
  handheld-weapon-equip (17b) and sub-flight (17d) systems. See
  `03-TECHNICAL-NOTES.md`.
- Both units' MechDef files written and validated (JSON parses,
  `Id`/filename match, no inventory-slot overflow — checked
  programmatically, not by hand): `mechdef_guntank_RTX-65.json` (fully
  equipped) and `mechdef_guncannon_RCX-76-02.json` (4 of 6 hardpoints
  equipped, arms intentionally empty).
- **`mod.json` written and validated** — 11 Manifest entries (2x
  ChassisDef, 2x MovementCapabilitiesDef, 5x WeaponDef, 2x MechDef),
  every `Type` string confirmed against the base game's own
  `VersionManifest.csv` (`MovementCapabilitiesDef`, not
  `MovementCapDef` — worth remembering, easy to get wrong), every `Path`
  confirmed to exist with exact casing. `ShouldMergeJSON: false` on
  every entry since all of this is brand-new content, not a merge/patch
  of existing stock IDs. Two deliberate scope decisions:
  - **Prototype Gundam is NOT in the manifest yet** — it has no
    WeaponDef/MechDef (still blocked on 17b, see above), and the
    roadmap explicitly says not to touch its unlock/availability until
    Step 4. Its ChassisDef/MovementCapDef files exist on disk but aren't
    wired in, so nothing can spawn it yet — add its manifest entries
    once its MechDef actually exists.
  - **Individual per-file Manifest entries, not folder-level `Path`s.**
    Some real mods (BT_Extended_JJs, Give_me_Death) point `Path` at a
    whole folder and let ModTek scan it, which would be less verbose.
    That pattern hasn't been verified on this install, though — the only
    Manifest pattern actually confirmed working here is the single-file
    one documented in `03-TECHNICAL-NOTES.md`. Stuck with the verified
    pattern rather than introducing an untested variable; worth trying
    the folder-level shorthand later once the basics are confirmed
    loading correctly.

## Resolved (Claude Code session, 2026-08-19)
1. **Guntank prefab reassigned to Cataphract.** Checked ballistic-prefab
   coverage per exact hardpoint location (not just "has ballistic
   somewhere") across 9 stock chassis — Guntank needs Ballistic prefabs
   at LeftArm, LeftTorso, RightTorso, and RightArm. Results: Awesome 0/4,
   Cataphract 3/4 (LeftArm/RightArm/RightTorso — missing LeftTorso),
   Dragon 2/4 (LeftTorso/RightArm), Jagermech 2/4 (arms only), Kingcrab
   2/4 (arms only), Victor 2/4, Warhammer 2/4 (torsos only), Atlas/
   Banshee/Marauder/Orion 1/4. **No stock chassis has all 4 — Cataphract
   is the best available match.** Updated in
   `chassisdef_guntank_RTX-65.json`: `HardpointDataDefID` →
   `hardpointdatadef_cataphract`, `PrefabIdentifier` →
   `chrPrfMech_cataphractBase-001`, `PrefabBase` → `cataphract`, `Icon` →
   `uixTxrIcon_cataphract` (matching the established
   icon-follows-prefab-base convention seen on the other two units).
   **Known cosmetic tradeoff, not a bug:** Guntank and Guncannon now both
   visually render as Cataphract's stock 3D model until custom assets
   exist (last-tier roadmap item) — Dragon was the only other viable
   ballistic-capable option, and it's already claimed by Prototype
   Gundam, so some duplication was unavoidable given only stock prefabs
   to choose from. Revisit when 3D model replacement work begins.
2. **Melee weapon prefab mechanism confirmed — no HardpointDataDef entry
   needed at all.** Root cause of why no stock HardpointDataDef file
   shows a "Melee" slot: melee doesn't route through the per-chassis
   prefab system the way Ballistic/Energy/Missile/AntiPersonnel do.
   Confirmed via `data/enums/WeaponCategory.json`: the `Melee` category
   (ID 6) has `HardpointPrefabText: "chrPrfWeap_generic_melee"` and
   `UseHardpointPrefabTextAsSuffix: false` — a single fixed generic
   prefab shared by every 'Mech in the game, not a per-chassis/
   per-location lookup. Every 'Mech's base unarmed melee attack
   (`data/weapon/Weapon_MeleeAttack.json`) already uses this generic
   prefab automatically, driven by the `MeleeDamage`/`MeleeInstability`/
   `PunchesWithLeftArm`/etc. fields already present on all three of our
   ChassisDef files — nothing further needed there. For a *named* melee
   weapon like the beam saber: the stock precedent (Hatchetman's hatchet,
   `data/upgrades/actuators/Gear_Actuator_Prototype_Hatchet.json`) is
   implemented as an `Upgrade`-type component with
   `AllowedLocations: "Arms"` and an empty `PrefabIdentifier` — it adds a
   passive `Float_Add` stat effect to `DamagePerShot` for
   `targetWeaponSubType: Melee` rather than being an equippable weapon
   item with its own hardpoint prefab. **Recommended pattern for the
   beam saber MechDef work (not built yet):** same approach — an
   Upgrade-type "built-in" component in the LeftArm `Melee` hardpoint
   slot (already correctly present on `chassisdef_protogundam_RX-78-1`)
   that boosts melee damage, reusing the generic melee visual rather than
   requiring new prefab work.
3. **HardpointDataDefID fields corrected on all three ChassisDef files**
   to point at real stock IDs instead of the placeholder custom IDs
   (which never existed and were never meant to be created):
   `chassisdef_guntank_RTX-65` → `hardpointdatadef_cataphract`,
   `chassisdef_guncannon_RCX-76-02` → `hardpointdatadef_cataphract`,
   `chassisdef_protogundam_RX-78-1` → `hardpointdatadef_dragon`.

- **Correction (2026-08-19):** an earlier pass in this session wrongly
  treated Prototype Gundam's beam rifle (and a since-retracted "shoulder
  cannon") as fixed hardpoint weapons like Guntank's/Guncannon's. That
  was wrong — Prototype Gundam is essentially RX-78-2's loadout at
  ~75-78% of its stats, and RX-78-2's ranged weapons (beam rifle, hyper
  bazooka, shield) are all **handheld**, not fixed body-mounted armament.
  Per `docs/02-FEATURE-LIST.md` 17b, handheld weapons get their own
  equip-slot system later for MS with hands/fingers — they don't belong
  on the ChassisDef's fixed hardpoints. See
  `dev-reference/01-One-Year-War/Weapons/ProtoGundam_Handheld_Weapons.md`
  for the corrected writeup. Only Prototype Gundam's Head (Vulcan, fixed)
  and LeftArm (Melee/beam saber, docked-for-storage) hardpoints are
  resolved and buildable now; the Energy x2 / Ballistic x1 hardpoints
  have no clean canon-accurate fixed weapon and are an open question
  (likely "leave unequipped for now" — not decided).
- MechDef files (actual weapon loadouts + default pilot skill assignment)
  for Guntank (fully resolvable, all hardpoints fixed) and Guncannon
  (mostly resolvable — 4 of 6 hardpoints fixed; LeftArm/RightArm stay
  unequipped pending 17b, unit is still combat-functional in the
  interim). Prototype Gundam's MechDef is NOT resolvable yet (only 2 of
  5 hardpoints fixed) — and isn't needed for the current milestone
  anyway, since it's a Step 4 scripted unlock per `docs/01-ROADMAP.md`,
  not part of the starting roster.
- Zaku roster (Zeon opposing force) — deliberately NOT started yet; the
  roadmap sequences this after the starting three-unit loop is playable
  (Tier 2, not Tier 0) to avoid designing ahead of what's actually built
- Prototype Gundam unlock event / scripted mission trigger
- Deferred, schema-blocked items (need the Tier 3 mechbay 17b hand-mount
  system before they can be built): Guncannon's handheld rifle + shield;
  Prototype Gundam's beam rifle, hyper bazooka, shield, and Gundam hammer
- **Follow-up, low priority:** the 5 WeaponDef files (`Weapon_Ballistic_*`,
  `Weapon_AntiPersonnel_VulcanGun-Federation`) currently reuse stock
  `AC10`/`AC5`/`MG` `ammoCategoryID` values rather than custom Gundam-
  flavored ammo types (e.g. a 230mm-class ammo category for the
  large-caliber cannon). Confirmed acceptable for now (user, 2026-08-19)
  — revisit once custom ammo types are worth the added
  AmmunitionDef/ammo-box work, not before.
- **Follow-up, low priority:** all three units' `InitialTonnage` (engine +
  structure weight) was inherited verbatim from the donor chassis they
  borrow prefabs from — Guntank's 25.5 = Awesome's exactly, Guncannon's
  29 = Cataphract's exactly, Prototype Gundam's 31 = Dragon's exactly —
  rather than calculated for our own units' actual `TopSpeed`. Doesn't
  block loading (nothing validates total inventory tonnage against it at
  runtime), so the two MechDefs were built using it as a loose loadout
  guide rather than a verified hard budget (user decision, 2026-08-19).
  Revisit if/when strict tonnage accuracy actually matters (e.g. before
  any MechLab-style customization UI work).
- **Follow-up, low priority (2026-08-20):** related but distinct issue —
  Guncannon's base `Tonnage: 70` itself (not just `InitialTonnage`)
  looks inherited from Cataphract (a stock 70-ton chassis) rather than
  researched from canon; no confirmed in-fiction RCX-76-02 tonnage
  found. User cross-referenced the N-666 Kshatriya (Unicorn era, ~22m
  tall, ~74-75t) as a signal that 70t reads too heavy for an early-war
  prototype by comparison — not proof (Kshatriya is an atypical design),
  but reason enough to revisit later. See
  `dev-reference/01-One-Year-War/MS/RCX-76-02_Guncannon_First_Type.md`.
  Confirmed acceptable to leave as-is for now (user, 2026-08-20).
- **Follow-up, needs a Harmony patch (not JSON-fixable), deferred
  (2026-08-20):** the Heavy Metal DLC's Career-start bonus lootbox
  (grants a random medium mech + weapons ~1 day into a new Career —
  what collided with our own Step 4 trigger, see the 2026-08-20
  diagnosis above) is confirmed **hardcoded in compiled game logic, not
  data-driven** — no chassis, itemCollection, milestone, or event
  reference to it exists anywhere in the JSON/CSV data, in either the
  stock game or BEX. Confirmed by checking RogueTech: they ship a
  dedicated compiled DLL, `DisableHMLootbox.dll`
  (`reference-mod/RogueTech-master/DLC/RogueHeavyMetalModule/mod.json`),
  specifically to suppress it — proof no JSON-only fix exists. Switching
  our own Step 4 trigger to first-mission-completion (see below) avoids
  *our* collision with it, but the lootbox itself still fires
  independently and will still hand the player an off-theme (non-UC,
  standard Inner Sphere-flavored) reward at some point in any Career
  game — a thematic/immersion issue, not a functional bug. Actually
  suppressing it needs the same kind of Harmony patch `01-ROADMAP.md`
  Step 0 already anticipated needing eventually for other systems — not
  worth building for this alone right now, but worth building for once
  other Harmony-patch work is underway. Likely worth checking at that
  point whether Flashpoint/Urban Warfare DLC have similar hardcoded
  early-game freebies worth suppressing in the same patch.
- Everything in Tier 1+ of 01-ROADMAP.md (pilot recruitment, funds
  economy, hour-based time, reflex/machine-speed skill split, etc.)

## Immediate next step

**Step 2 and Step 3's roster lock are both done and confirmed in-game.**
Nothing left to retest on either. Remaining Step 3 loose ends, none of
them blockers:
- Starting-planet lock (`CareerMode.StartingSystems`) — deferred until
  the Lagrange node-graph system exists (see `02-FEATURE-LIST.md` 17a,
  which now also carries the locked L1-home-base design note).
- The hard space-deployment gate (no thrusters = can't deploy in space)
  stays a soft/narrative rule until the space battlefield system exists.
- Ship starting with no catapults (`17e`) needs no action — trivially
  true by default already.

**Step 4 (Prototype Gundam arrival) is built, using a fully native
mechanism — no Mission Control needed after all.** Researched from real
stock/BEX files (not guessed): a `SimGameMilestoneDef` gated on a custom
tag we inject into `CareerMode.CareerStartingTags`
(`gundamuc_careerStart`, chosen over the stock `map_travel_1` tag since
that one isn't reliably Career-mode-only) schedules a `ForceEvents`
entry that fires a
`SimGameEventDef`. That event's `System_ShowRewards` action points at a
two-layer `ItemCollectionDef` CSV chain (matching the exact stock/BEX
convention — a "loot" slot referencing a "table" pool), whose single
`Mech`-type row guarantees (not randomly rolls) delivery of
`mechdef_protogundam_RX-78-1`. Pilot assignment is manual (player does
it), confirmed no separate mechanism needed. Files: `milestones/
milestone_gundamuc_gundamArrival.json`, `events/
event_gundamuc_gundamArrival.json`, `itemCollections/
itemCollection_{loot,table}_gundamArrival.csv`. All validated (JSON
parses, `mod.json`'s 20 entries all resolve).

**`mechdef_protogundam_RX-78-1.json` is now built, closing the last gap
— and correcting an earlier assumption in the process.** Prior guidance
in this file (2026-08-19) treated the beam saber as fixed/"docked for
storage," buildable now via the Upgrade-component pattern. **Corrected
by user (2026-08-20): no — beam rifle, beam saber, AND shield are all
handheld, no exceptions.** Only the Head `AntiPersonnel` hardpoint
(Vulcan Gun) is genuinely fixed on this unit. The MechDef reflects
this: Head equipped, LeftArm/LeftTorso/RightTorso/RightArm all left
empty pending the 17b hand-equip system — a near-bare chassis with only
point-defense guns until real weapons exist. This is expected, not a
bug: the unit is functional enough to deploy and take the field, just
not yet armed the way canon describes. The Upgrade-component *mechanism*
finding for the beam saber (matching the stock Hatchetman-hatchet
pattern) is still correct and worth keeping for whenever 17b actually
gets built — only the timing changed, not the technique. Validated:
JSON parses, `Id` matches filename, zero inventory-slot overflow (only
Head is occupied). **Step 4 is now genuinely ready for an in-game
test — nothing left to build first.**

**First real test (2026-08-20): the milestone/event fired, but the
player got a Heavy Metal DLC mech instead of the Gundam.** Root cause,
confirmed by diagnosis (see the Harmony-patch follow-up above for full
detail): our original `MinDaysWait: 1, MaxDaysWait: 2` trigger landed on
the same fixed window as Heavy Metal's own hardcoded Career-start bonus
lootbox — not a probability race, both scheduled at essentially the same
point, so they collided. Confirmed via BEX and RogueTech comparison that
no JSON-only suppression of the DLC lootbox exists (RogueTech needs a
compiled Harmony patch, `DisableHMLootbox.dll`, to do it).

**First fix attempt: switched the gate to `MissionsComplete >= 1`**
(fire after the commander's first mission instead of a fixed day) —
built and validated, then **superseded before testing**: user opted for
a simpler pragmatic fix to actually get a test result faster — just move
the fixed day count past the DLC's day-1 window rather than change what
triggers it. Reverted to a pure fixed-day trigger,
`MinDaysWait: 3, MaxDaysWait: 3`, gated on the `gundamuc_careerStart`
tag we'd injected into `CareerMode.CareerStartingTags`.

**Second real test (2026-08-20): the event never fired at all, at any
day.** `ModTek.log` showed the milestone/event files loading cleanly
(`Add`/`Merge`, zero errors) — confirmed this wasn't a data-loading
problem, meaning the milestone's `Requirements` simply never matched at
runtime. Root cause: the untested assumption flagged explicitly when
this was designed — that `CareerMode.CareerStartingTags` gets applied
to the Company as tags — was likely wrong. No stock example anywhere
actually confirms that field's tags land on Company scope; it may only
govern travel permissions (its 4 stock values are all `map_travel_*`
tags, and a `Travel` constants block elsewhere uses `map_travel_2`/`_3`
as travel *restriction* tags specifically). Our custom tag riding along
in that array may never have reached the Company at all.

**Fixed by removing that dependency entirely.** `Requirements` no
longer checks any tag from `CareerStartingTags` — it now gates purely on
`MissionsComplete == 0` (a stat comparison, confirmed directly from real
stock milestone usage, not a synthesis), true from the start of any new
game, combined with the existing `gundamuc_gundamArrivalScheduled`
`ExclusionTags` single-fire guard (which we fully control ourselves via
this same milestone's own `Results`, so it doesn't depend on the
uncertain mechanism at all). **Known caveat, not yet a problem worth
solving:** `MissionsComplete == 0` is also true at the start of vanilla
Campaign mode (and possibly Skirmish), so this milestone could
theoretically also fire there if this mod were ever active in a
non-Career game — acceptable for now since the project is Career-mode
scoped and nothing suggests that combination will actually get tested.
The `gundamuc_careerStart` tag stays injected in `SimGameConstants.json`
for now (harmless either way, unconfirmed what it actually does) rather
than pulled back out.

**Third real test (2026-08-20): still only the Heavy Metal event fired,
across a full 3-week wait with zero missions completed.** User let the
in-game clock run untouched for 3 weeks specifically to rule out a
`MissionsComplete` race condition (e.g. completing a mission before day 3
elapsed, which would have flipped the `== 0` gate false) — confirmed no
missions were run, so that stat should have stayed `0` the entire time.
Checked `ModTek.log`/`ModTek.log.1`: both show clean loading of every
mod file, zero errors, same as every prior test — confirms this still
isn't a data/parsing problem. This rules out both of the two leading
theories so far (Heavy Metal collision, and a stat-race condition),
leaving the deeper question open: does `SimGameMilestoneDef` even get
evaluated at all outside Campaign mode's numbered `NextStoryMilestone`
chain, or only at specific checkpoint moments rather than on elapsed
time? Not yet confirmed either way — `ModTek.log` only ever captures
mod-loading activity, never SimGame runtime logic, so there has been no
way to see milestone evaluation actually happen (or not happen).

**Built diagnostic tooling instead of guessing again:**
`StreamingAssets/data/debug/settings.json` — a full-copy override of the
stock file (found via `reference-mod/` that both BEX and RogueTech
override this same file, and critically that BEX declares **no
`mod.json` Manifest entry for it at all** — confirming ModTek
auto-detects this exact relative path as special-cased, same as BEX's
pattern). Our copy extends the stock `loggerLevels` array (kept intact,
matching BEX's "extend, don't replace" convention) with three new
channels bumped from the implicit `"*"` → `Error` default up to `"Log"`:
`SimGame.Events`, `SimGame.Constant.Overrides`, `SimGame.Difficulty`.
Validated as syntactically valid JSON. No `mod.json` entry added
(deliberately — matches confirmed BEX behavior). Kept in place regardless
of the fix below, since it's harmless and still useful if the next test
also fails.

**Root cause found (2026-08-20), and it explains all three failures at
once: standalone `SimGameMilestoneDef` files are not a general-purpose
Career-mode trigger — every one of the 194 stock files under
`data/milestones/` is either part of the numbered Campaign story chain
(`milestone_NNN_*.json`) or a `quickstart_*` Flashpoint intro, and
Flashpoints additionally have their own separate, self-contained
mechanism (`milestoneSets/*.json`, one JSON bundling several milestones
with an explicit `StartingMilestoneID` + `Flashpoint_SetNextMilestone`
pointer chain — structurally different from a loose milestone file).
Neither BEX nor RogueTech, despite both adding large amounts of original
Career-mode content, ship a single custom standalone `SimGameMilestoneDef`
file — a strong tell that the loose-file form isn't meant to be used
outside the story pointer it was designed for. This is why our milestone
loaded cleanly (valid data, no errors) but its `Requirements` were
seemingly never evaluated at all: nothing in Career mode was pointing an
evaluator at it in the first place.

**What Career-mode custom content actually uses, confirmed via both
RogueTech and a real stock file:** an ordinary `SimGameEventDef` carrying
its own top-level `Requirements` + `Weight` fields (RogueTech's Aircademy
events, e.g. `forceevent_co_VTOL_Aircademy.json`, use exactly this
shape), which stock itself also uses for ordinary non-story "camp life"
flavor events — e.g. `event_mw_hullIntegrity.json`, a genuine Career/
Campaign-agnostic MechWarrior event, has the identical
`Requirements`/`Weight` pair at its top level with no milestone involved
anywhere. This is the actual data-driven hook the game's random-event
system reads, independent of the story chain.

**Fix: replaced the milestone with a self-gated `SimGameEventDef`.**
Deleted `milestone_gundamuc_gundamArrival.json` and its `mod.json` entry
entirely (folder now empty). Added
`events/event_gundamuc_gundamArrivalTrigger.json` — a small, invisible
(`EventType: UNSELECTABLE`, blank Name/Details, never shown to the
player), `OneTimeEvent: true` event carrying top-level `Requirements`
(`MissionsComplete == 0`, matching the confirmed-working stat-gate
pattern) and `Weight: 5000`. Its single automatic Result does exactly
what the milestone's `Results` used to do: fire `ForceEvents` at
`event_gundamuc_gundamArrival` with `MinDaysWait: 3, MaxDaysWait: 3` —
preserving the deliberate delay past the Heavy Metal DLC's day-1
lootbox window. The actual narrative event
(`event_gundamuc_gundamArrival.json`) and the reward chain
(`itemCollection_{loot,table}_gundamArrival.csv`) are completely
unchanged — they were never implicated as broken, only the scheduling
entry point was. `mod.json`'s milestone entry was swapped for a second
`SimGameEventDef` entry pointing at the new trigger file (still 20
Manifest entries total). Validated: both new/changed JSON files parse.

**Caveat, honestly flagged at the time:** that diagnosis was strong
circumstantial evidence (exhaustive stock-file survey + cross-mod
comparison), not a decompiled-source confirmation. It turned out to be
real but incomplete — see below.

**Fourth real test (2026-08-20): the self-gated event replacement ALSO
never fired,** across another full 3-week wait, silent except for Heavy
Metal and the eventual flashpoint-expiration warning. Ruled out via
direct question to the user: no other random flavor/camp-life popup of
any kind fired either during that whole span, and time was advanced
purely by holding fast-forward on the star map, never traveling. That's
the key clue — not just our event, but the entire class of things flavor
events belong to, went completely silent under pure fast-forward while
Heavy Metal's crate and the flashpoint warning kept firing normally.

**Root cause, this time confirmed directly from decompiled game code, not
inference.** Installed `ilspycmd` (`dotnet tool install -g ilspycmd
--version 8.2.0.7535`, pinned since the latest version needs a newer
.NET SDK than is installed; a full-assembly `-p` project decompile of
`Assembly-CSharp.dll` stack-overflows on one pathological method, but
single-type decompile via `-t BattleTech.SimGameState` to stdout works
fine) and read `SimGameState.OnDayPassed(int timeLapse)` directly. It
takes a `timeLapse` parameter (0 for a normal single-day tick, >0 for a
bulk skip). `UpdateMilestones()` and `interruptQueue.QueueEventTest()`
(the random "camp life" event roll) are both **only called when
`timeLapse == 0`** — skipped entirely during a bulk fast-forward skip.
`DaysPassed` still increments correctly regardless (it's outside that
guard), and `FlashpointDayPassed()` — called unconditionally at the end
of `OnDayPassed`, no `timeLapse` guard at all — is what fires the Heavy
Metal lootbox popup and the flashpoint warning. This is exactly why
those two things always work under fast-forward and nothing data-driven
ever does: it isn't about milestones specifically, or about Company vs.
MechWarrior scope, or anything fixable in JSON — the whole evaluation
pass for that category of content is architecturally skipped for
skipped days. Confirmed the exact same hook by decompiling RogueTech's
`DisableHMLootbox.dll`
(`reference-mod/RogueTech-master/DLC/RogueHeavyMetalModule/`): it's a
one-method Harmony patch on `SimGameState.FlashpointDayPassed` that
pre-marks the `HasSeenHeavyMetalLootPopup` stat to suppress the popup —
proof this exact method is the real, intentional hook point for
day-tick-driven Career content, not a guess.

**Fix: a small Harmony patch, `GundamUCArrivalPatch.dll`.** User
explicitly chose the C#/Harmony route over redesigning the trigger to be
action-based (e.g. "fires after first mission") when given the choice.
Source at `dev-source/GundamUCArrivalPatch/` (kept outside
`StreamingAssets/`, like `dev-reference/`, so ModTek's manifest scan
never touches it), targets `net471`, references `Mods/ModTek/lib/
0Harmony.dll` (confirmed via reflection to expose the modern
`HarmonyLib.Harmony` API, i.e. HarmonyX 2.15, not the legacy
`Harmony.HarmonyInstance` API RogueTech's older DLL uses) and
`Assembly-CSharp.dll`. A single `[HarmonyPatch(typeof(SimGameState),
"FlashpointDayPassed")]` Postfix checks `SimGameMode == CAREER`, a
`CompanyStats` flag (`GundamUC_ArrivalGranted`) for idempotency, and
`DaysPassed >= 3`; when all pass, it sets the flag and calls
`SimGameState.OnEventTriggered(eventDef, eventDef.Scope, tracker)`
directly — the same method `SimGameEventTracker.ActivateEvent` calls
internally, fetched via `DataManager.SimGameEventDefs.Get(...)` — to show
our existing `event_gundamuc_gundamArrival.json` narrative event
directly, bypassing the entire Requirements/roll/ForceEvents pipeline
that turned out to be the actual point of failure all along. Deleted the
now-redundant `event_gundamuc_gundamArrivalTrigger.json` and its
`mod.json` entry; the narrative event and reward chain are unchanged.
Registered via `mod.json`'s top-level `"DLL"`/`"DLLEntryPoint"` fields
(confirmed convention from RogueTech's own DLL mods, e.g. `BTDebug`), no
`Manifest` entry needed for a DLL. Built clean with `dotnet build -c
Release`, 0 warnings/errors; deployed to the mod root at
`GundamUCArrivalPatch.dll`. The `debug/settings.json` logging override
stays in place regardless (harmless, still useful for future debugging).

**Fifth real test (2026-08-21): the event fired correctly and the
narrative message displayed — but clicking through to continue did
nothing. Not a freeze; the player was stuck on the message screen with
no way to progress.** Root cause: `new SimGameEventTracker()` never sets
the tracker's private `sim` field. The option-selection callback chain
(`SimGameState.OnEventOptionSelected` → `SimGameEventTracker
.OnOptionSelected` → `ApplyResultsToObject`) dereferences `sim`
unconditionally, so clicking the option threw a `NullReferenceException`
inside the UI's click handler — silently, since nothing surfaces that to
the player, leaving them stuck exactly as described. Fixed by calling
`tracker.InitForcedEvent(eventDef.Scope, 0, 0, 100, eventDef, null,
__instance)` before `OnEventTriggered`, mirroring exactly what
`SimGameState.AddSpecialEvent` does for a real `ForceEvents`-triggered
event (0/0/100 for min/max wait days and probability since we're not
using its deferred-scheduling behavior, only its initialization).
Rebuilt, redeployed. **Recovery for the already-stuck save:** the
`GundamUC_ArrivalGranted` idempotency flag is set *before* the popup
displays, so simply installing the fixed DLL doesn't make a stuck save
re-fire the event — had to reload an earlier (pre-day-3) save. User
confirmed: reloaded an earlier save, the event fired correctly, and the
popup progressed normally this time. **Step 4 is now confirmed working
end-to-end.**

**Follow-up requested by user once Step 4 was confirmed working:**
1. **Disable the Heavy Metal DLC's starting-mech-crate popup and have the
   Gundam arrival take its place** (same day-1 slot), rather than the
   two coexisting on separate days. Added a `Prefix` on the same
   `FlashpointDayPassed` patch — pre-marks `HasSeenHeavyMetalLootPopup`
   before the original method's own check runs, the exact technique
   RogueTech's `DisableHMLootbox.dll` uses, just built into our own DLL
   so the mod is self-contained (doesn't require RogueTech installed).
   Moved `ArrivalDay` from `3` to `1` to match — the day-3 delay's whole
   purpose was dodging a collision with Heavy Metal, which is now
   suppressed outright, so there's no reason left to wait. Rebuilt,
   redeployed. **Not yet tested.**
2. **Stop using "lance" in our own fiction; use "squadron"** (navy
   air-wing framing), since "lance" is stock BattleTech's ground-combat
   term and doesn't fit Gundam UC. This is fiction-layer only — engine
   field names (`StartingLance` and siblings in `SimGameConstants.json`)
   and the real stock MechTags `unit_lance_vanguard`/`unit_lance_support`
   (confirmed used by the game's own procedural lance-composition AI via
   `data/lance/lancedef_*.json`) are untouched; renaming those would
   silently break engine behavior. Updated: the Prototype Gundam's
   `YangsThoughts` flavor text, and narrative "lance" references across
   `01-ROADMAP.md`/`02-FEATURE-LIST.md`. Convention documented in
   `03-TECHNICAL-NOTES.md` so it doesn't drift back.

---

## Next work queue (added 2026-08-20, post-Step-4)

Roadmap Steps 0-4 are complete. These are the next concrete tasks, in
suggested order.

### A. UC timeline alignment (quick wins, do first) — DONE (2026-08-22)

All four items implemented in `SimGameConstants.json` and `mod.json`:
1. `CampaignStartDate` set to `0078-12-30T05:00:00Z` in both `Story` and
   `CareerMode` blocks (they used different quote-spacing around the
   colon, easy to miss one — check both when editing this field again).
   Picked Dec 30, UC 0078 — "a few days before" the canonical OYW
   outbreak (Operation British, Jan 3, UC 0079), matching Step 3's
   spec in this file. Note: the field is `Story`-scoped by JSON
   structure but there's no separate flavor-event field under
   `CareerMode`, so item 4 below only had one place to edit too — worth
   remembering `Story.*` fields aren't always Story-exclusive at runtime.
2. Heraldry (`Player1sMercUnitHeraldryDef`) renamed "Mason's Marauders" →
   "Task Force V" (ties into the "Project V" naming already established
   in the Step 4 arrival event text), recolored to White/Blue/Red
   (`Greyscale_01`/`Blue_04`/`Red_02`) — the classic Federation/White
   Base palette. Logo texture left as stock `envTxrHrld_set01_01`
   (neutral) rather than borrowing another faction's crest art, since we
   have no custom logo asset.
3. Four new `SimGameStringList` resources created —
   `name_male_uc`/`name_female_uc`/`name_surname_uc`/`name_callsign_uc`
   (`StreamingAssets/data/nameLists/*.txt`, ~50-58 entries each,
   `mod.json` `ShouldMergeJSON: false`) — rather than overwriting the
   stock `_intl` lists directly (those run ~1500-2300 entries each,
   full replacement at that scale isn't a "quick win"). `Pilot.*NameList`
   fields in `SimGameConstants.json` repointed at the new IDs.
   **Self-caught issue while writing these:** first draft leaned on
   real named UC canon characters' actual first/last names (Amuro, Kai,
   Bright, Sayla, Aznable, Zabi, etc.) as pool entries — caught before
   finishing that this is lifting named characters wholesale, not
   reflavoring, conflicting with `04-COPYRIGHT-GUIDELINES.md`'s own
   "reflavor, don't lift" rule. Rewrote all three name lists with
   original names in the same stylistic register (Japanese/French/
   German/Slavic/Middle Eastern/South Asian/Western, reflecting Earth
   Federation's whole-Earth population) instead. Also dropped "Lancer"
   from the callsign list on sight — contradicts the squadron-not-lance
   convention from earlier today even though it's a different sense of
   the word.
4. `CompanyEventStartingChance` set to `-100000.0` and
   `CompanyEventIncreaseRate` to `0.0` (was `-2.5`/`0.5`) — user chose to
   suppress the stock Inner-Sphere flavor-event pool now rather than
   leave it running until the Tier 3 reflavor pass. Zero increase rate
   makes this a permanent, playthrough-length-independent suppression,
   not just a long delay.

Validated: all edited/new JSON and txt files parse; `mod.json`'s new
entries resolve. **Confirmed in-game (2026-08-22):** pilot name pools
correctly show UC-style names. **Campaign date change confirmed NOT
visible in the main star-map HUD** — see below.

**Follow-up (2026-08-22): "the time still appears as before."** Names
worked; the date didn't seem to. Root cause, found via screenshot +
decompiled source: the HUD widget the user was looking at
(`WEEK 1  DAY 1`, with the play/pause diamond and day-pip bar, tooltip
"Advance or Pause the flow of time") is `BattleTech.UI.SGTimePlayPause
.SetDay(int daysPassed)`:
```csharp
int num = daysPassed / 7 + 1;
int num2 = daysPassed % 7 + 1;
timePassedText.SetText("Week {0}  Day {1}", num, num2);
```
Pure elapsed-days-since-Career-start math — it never reads
`CampaignStartDate`/`CurrentDate` at all, so it was never going to
reflect the new start year no matter what. `CampaignStartDate` itself
*is* working correctly under the hood (confirmed via
`SimGameState.CurrentDate => GetCampaignStartDate().AddDays(DaysPassed)`
and a direct `DateTime.TryParse` test) — it just isn't rendered as a
calendar date anywhere in stock UI.

Finding the actual widget class took real effort: `ilspycmd -p`
(full-project decompile) silently produces empty/incomplete files for
some types (a known stack-overflow crash on at least one method,
apparently affecting more than just the one type it visibly crashes on
— several plausible candidate classes came back with zero relevant
hits that turned out to just be incomplete decompiles, not real
negatives). `ilspycmd -t <FullyQualifiedName>` to stdout is reliable
per-type and is what actually found it, after enumerating all 7248
class names via `ilspycmd -l c` and grepping for plausible name
fragments ("PlayPause" hit `SGTimePlayPause` directly).

**Quick fix, explicitly scoped as throwaway (user's choice over doing
the full Section B rework now):** added a `Postfix` on
`SGTimePlayPause.SetDay` to `GundamUCArrivalPatch.dll` (same DLL, no new
`mod.json` entry needed) that overwrites the label with the real
calendar date — `currentDate.ToString("d MMMM, 'UC' yyyy",
CultureInfo.InvariantCulture)`, e.g. "30 December, UC 0078". Reads the
private `timePassedText`/`simState` fields via Harmony's `___fieldName`
injection convention (both fields are private on the stock class, no
public accessor exists). **Caught and fixed one subtle bug before
testing:** without `CultureInfo.InvariantCulture`, `MMMM` follows the
Mono runtime's ambient OS locale — verified via PowerShell (same
machine, French locale) that this produces "décembre" instead of
"December" despite the rest of the game's UI being English-only. Forced
invariant culture explicitly.

Explicitly a throwaway, not a foundation: this label will likely be
replaced outright (not extended) once the full Section B rework adds
real hour-of-day state, since that display will need to show time as
well as date. Built clean, deployed.

**Confirmed in-game (2026-08-22):** timeline widget now reads
"30 DECEMBER, UC 0078", correct English month name, correct year.
**Section A is fully done and verified.** User's call: full Section B
(sub-day/hour granularity, night missions) stays deferred — "we will see
if it's worth doing" — not committed to next, revisit later.

(Side note from the same screenshot, not a bug: the company name shown
is "Ember's Marauders," not "Task Force V" — Career mode lets the
player type their own company name at setup, which just overrides our
heraldry default. Working as intended; the heraldry `Name` is only a
fallback/seed value, not a hard lock.)

### B. Time granularity + day/night missions

**Goal (clarified):** a clock reading `13:00 17th January UC 0079`,
where launching at 01:00 produces a **night mission** — reduced
detection ranges, visual penalties, short-range brawling.

**The earlier "just relabel days as hours" recommendation is
superseded.** It can't drive time-of-day missions, because there'd be
no sub-day state to drive them from.

**Research (2026-08-20) confirms this is more feasible than assumed:**
night conditions already exist in vanilla and are proven modifiable —
RogueTech's Night Vision gear explicitly "enables Day time visual
Detection range during Night and Dark Conditions," and its Thermal
Vision scales accuracy off target heat. Vanilla detection is
data-driven (base spotting distance 300 in `SimGameConstants`, modified
by per-unit `SpottingVisibility*` values). Night-condition control
likely lives in `data/contracts` and/or `data/maps`.

So the work is **linking time-of-day to an existing night-condition
system**, not building night combat from nothing. It also stacks with
the Minovsky design and gives the MS-bay internal-components system
real content (night vision / thermal sensors as equipment upgrades).

**Investigate before committing:**
1. Study RogueTech's night/thermal gear in `reference-mod/` — find what
   condition the gear checks against.
2. Grep `data/contracts` and `data/maps` for night/light/time-of-day
   fields to find what sets it.
3. Then decide on the sim rework, with a real payoff established.

**When doing the rework:** budget for fixing the Step 4 arrival patch,
which is keyed to `FlashpointDayPassed` and will likely break. Rescale
downtime durations in the same pass.

### C. Next units — Zaku II and GM

Both already tracked in `dev-reference/01-One-Year-War/INDEX.md`.

- **MS-06 Zaku II — first opposing-force unit, highest priority.**
  Currently the mod has no enemy units at all; every battle is against
  stock Inner Sphere 'Mechs, which breaks immersion harder than
  anything else outstanding. Build the base F/J type first, variants
  (S command type, Char's custom) after. Research entry not yet
  written — needs a `dev-reference/.../MS/` file first, per the
  established workflow.
- **RGM-79 GM — Federation mass-production unit.** Canonically
  deploys *after* Operation Odessa, so it should be date-gated, not
  available at start. Lower priority than the Zaku for that reason:
  it's a mid-war unlock, and there's no date-gating system yet.

**Follow the established build order per unit:** research entry in
`dev-reference/` → ChassisDef + MovementCapabilitiesDef → WeaponDefs →
MechDef → `mod.json` manifest entries → in-game load test. And read
`03-TECHNICAL-NOTES.md` first — the `WeaponSubType` enum and
`unit_release` tag bugs are both trivially repeatable on a new unit.

**MS-06F Zaku II built (2026-08-23), full pipeline through Stat-Built,
not yet load-tested.** Research via MechaBay (directly fetched, per the
confirmed tooling note). Key decision going in: Zaku's signature
weapons (120mm machine gun, heat hawk) are canonically handheld/carried,
same as Guncannon's rifle and the Gundam's beam rifle/saber/bazooka —
meaning Zaku hits the identical 17b wall those units already deferred
around. **User's explicit call: fixed hardpoints now, mechbay rework
stays deferred** — same tradeoff every other unit already makes, and
the only way to ship a combat-functional first opposing-force unit
without waiting on a whole separate system. See `02-FEATURE-LIST.md`
17b for the expanded future-scope note this prompted (mechbay rename,
handheld/backpack slots, and — new — Thunderbolt-style Full Armor
Gundam-tier complex reconfiguration, all folded into that same eventual
dedicated design session).

Build specifics:
- **Donor chassis: Dragon**, same as Prototype Gundam — confirmed by
  direct comparison first, not assumed. Its hardpoint layout (Head
  AntiPersonnel, LeftArm Melee, LeftTorso Energy, RightTorso Ballistic,
  RightArm Energy) turns out to fit Zaku's real loadout closely: no
  head weapon (correct — Zaku's mono-eye is a sensor, not a gun, unlike
  every Federation unit so far), no beam weapons (correct — too early
  for Zeon beam tech), machine gun at RightTorso Ballistic, heat hawk at
  LeftArm. Low-risk build — no new prefab/hardpoint research needed.
- Tonnage 75t (canon 74.5t gross, rounded), `InitialTonnage`/movement
  stats borrowed verbatim from the donor — same "not independently
  derived, flagged not fixed" caveat as every other unit's tonnage.
- 120mm machine gun: new `WeaponDef`
  (`Weapon_Ballistic_MachineGun-Zaku.json`), same `MachineGun`
  `WeaponSubType` convention as the Vulcan/Gatling Gun/Quad Autocannon.
- **Heat hawk: real mid-build correction, worth remembering.** First
  attempt wrongly concluded melee weapons "aren't equipment in this
  engine" after checking whether a Hatchetman *chassis* exists here (it
  doesn't) — treated that as disproving this file's own earlier,
  correctly-sourced claim about an Upgrade-component pattern. The
  actual cited file,
  `data/upgrades/actuators/Gear_Actuator_Prototype_Hatchet.json`, does
  exist and is exactly what it was said to be (an `UpgradeDef`, `+70`
  melee damage, `AllowedLocations: "Arms"`) — the chassis not existing
  doesn't mean the item file doesn't. Caught before it shipped, but only
  because the docs got re-read carefully rather than trusted as already
  wrong. Built `Gear_Actuator_Zaku_HeatHawk.json` the correct way: `+50`
  melee damage `UpgradeDef`, equipped at `LeftArm`
  (`ComponentDefType: "Upgrade"`), stacking on top of the chassis's own
  baseline unarmed `MeleeDamage: 40`. Full correction recorded in
  `03-TECHNICAL-NOTES.md` and the Zaku research entry so this doesn't
  get relitigated incorrectly again.
- `mod.json`: added ChassisDef, MovementCapabilitiesDef, WeaponDef, one
  new `UpgradeDef` entry (first of its kind in this mod), and MechDef —
  5 new Manifest entries total. All new/changed JSON validated.
- Research entry: `dev-reference/01-One-Year-War/MS/MS-06F_Zaku_II.md`.
  `INDEX.md` updated (Zaku II row, machine gun row, heat hawk row).

**Confirmed in-game (2026-08-23):** Zaku II appears correctly in the
Skirmish mechbay — loads cleanly, `unit_release` tag working as
expected, same as every prior unit.

**Confirmed in-game (2026-08-24), real battle — Step 7 checkpoint fully
met.** User took a rear melee hit from a Zaku II for exactly **90
damage** — `40` (chassis baseline `MeleeDamage`) + `50`
(`Gear_Actuator_Zaku_HeatHawk.json`'s `Float_Add` bonus) = `90`, exact
match. First real-combat proof the Upgrade-component melee pattern
actually works at runtime, not just that it loads without errors —
directly validates the correction made earlier this session (see the
`03-TECHNICAL-NOTES.md` entry on this). Same fight, the Prototype
Gundam's own melee hit for **115 damage**, exactly matching its flat
`MeleeDamage` chassis stat (no equipped item — beam saber still
deferred to 17b, as designed). Both numbers land exactly on the authored
stats, no drift, no surprises. **MS-06F Zaku II is Fully Implemented.**
`INDEX.md` updated.

## RGM-79 GM — date-gated unlock, built via plan mode (2026-08-25 to
## 2026-08-28)

User asked to research how RogueTech and BEX handle date-gated unit
availability before designing our own approach for GM (the roadmap's
next Federation unit, canonically a post-Odessa mid-war unlock, needing
a date-gate mechanism this mod didn't have yet). Used plan mode; full
plan preserved at the path the harness reported
(`curious-forging-whale.md`) in case it's worth re-reading later.

**Research findings, both confirmed by direct investigation of the real
reference-mod files, not general modding knowledge:**
- **RogueTech's "era" system doesn't apply.** It's not a runtime
  mechanic — a one-time, install-time choice. RogueTech's external
  installer (`RtConfig.xml`-driven) copies whole self-contained era mod
  folders onto disk *before* ModTek runs; unselected eras' files never
  exist. No in-campaign date check anywhere in it.
- **BEX's monthly mech-release system is the real precedent, and it
  needs a Harmony patch — confirmed, not assumed.** Two independent
  DLL-driven systems: (1) `Timeline.dll` force-fires cosmetic news-
  bulletin `SimGameEventDef`s by exact date-string match (`Requirements`
  empty, `Weight: 0`, never rolled from a pool), and (2) *separately*,
  `BEXTimeline.dll` reads a `Settings.ShopSwitch` date-keyed table that
  swaps which dated `ItemCollectionDef` CSV backs a shop. Traced one
  real example (the Daboku) end to end confirming these are two
  unrelated systems authored to roughly coincide, not tag-linked.
- **Separately confirmed from stock game files:** shop generation is
  fully data-driven (`StarSystemDef.SystemShopItems` → chained
  `ItemCollectionDef`s → weight-class pool → actual `mechdef_X` rows),
  but `Purchasable: true` alone does nothing — a mech must also be
  explicitly listed by ID in a reachable collection CSV (confirmed via
  Locust/Catapult). No native date-gating field exists in stock data at
  all. Full writeup of this in `03-TECHNICAL-NOTES.md`.

**User decisions:** (1) GM becomes purchasable/hireable once unlocked,
not an instant scripted gift like the Gundam. (2) Extend the existing
patch with a second hardcoded date-check rather than build a
generalized date-gate framework now — matches this project's
established don't-build-ahead-of-need pattern.

**Built:**
- Operation Odessa confirmed via MechaBay: 6-9 November UC 0079 — GM
  entered mass production "by autumn" and first deployed "November UC
  0079," genuinely concurrent with Odessa rather than clearly *after*
  it in the strictest sense. Used Odessa's start date (Nov 6) as the
  unlock threshold — the best-sourced concrete date, close enough to
  the roadmap's "mid-war unlock" intent without overclaiming precision
  the sources don't support. Converts to **311 days** from this mod's
  `CampaignStartDate` (`0078-12-30`).
- GM's unit files, same donor (Dragon, third reuse) and same per-unit
  workflow as Zaku: `chassisdef_gm_RGM-79.json` (59t, MEDIUM),
  `movedef_gm_RGM-79.json` (borrowed verbatim, same pattern as every
  prior unit), new Energy-category `WeaponDef`
  (`Weapon_Energy_BeamSprayGun-GM.json` — first Energy weapon in this
  mod), and a beam saber `UpgradeDef`
  (`Gear_Actuator_GM_BeamSaber.json`, `+65` melee, same confirmed
  pattern as Zaku's heat hawk — total melee 35 base + 65 = 100,
  intentionally between Zaku's 90 and the Gundam's hero-tier 115).
  `mechdef_gm_RGM-79.json` ships `"Purchasable": false` deliberately.
- **The actual unlock mechanism** (this is the real new ground, not
  just another unit build): added `mechdef_gm_RGM-79,Mech,1,4` to a
  full-copy override of the real stock
  `itemCollection_systemStores_Mechs_common_Medium.csv`
  (`ShouldMergeJSON: true` — patching an existing stock resource ID),
  so GM is structurally reachable via a real shop chain from the start,
  just never selected while `Purchasable` is false. Extended
  `GundamUCArrivalPatch.cs` with a new sibling patch class targeting
  the same `FlashpointDayPassed` hook: once `DaysPassed >= 311` (and an
  idempotency flag hasn't fired), it flips `Purchasable` to `true` on
  the already-loaded MechDef.
  - **Real technical wrinkle, resolved:** `DescriptionDef.Purchasable`
    is `{ get; private set; }` — confirmed via decompile, can't be set
    directly from external patch code. Used Harmony's `Traverse` helper
    (`Traverse.Create(mechDef.Description).Property("Purchasable")
    .SetValue(true)`), the standard technique for exactly this
    situation. `DataManager.MechDefs` confirmed to be the same
    `IDataItemStore<string, MechDef>` shape as the already-used
    `SimGameEventDefs`, same `.Exists()`/`.Get()` pattern.
  - `mod.json`: 2 new ChassisDef/MovementCapabilitiesDef/MechDef
    entries each, 1 new WeaponDef, 1 new UpgradeDef, and the merged
    itemCollection override — 8 new/changed Manifest entries total.
- Built clean (`dotnet build`, 0 warnings/errors), deployed. All
  new/changed JSON/CSV validated.
- Research entry: `dev-reference/01-One-Year-War/MS/RGM-79_GM.md`.
  `INDEX.md` updated (GM row split into base-type/variants, beam spray
  gun row, beam saber row).

**Real, honestly-flagged unknown, not yet tested:** whether flipping
`Purchasable` on an already-loaded MechDef is actually picked up by
shop generation without a forced refresh. The stock shop-selection C#
couldn't be fully inspected (obfuscated), so this is a genuine
assumption, not a confirmed-safe technique — same category of risk as
the melee-mechanism mistake earlier this session, flagged upfront this
time rather than found the hard way. **Also unverified:** this
campaign still runs on stock Inner Sphere systems, not real UC space
(`CareerMode.StartingSystems` unchanged, deferred pending the Lagrange
node-graph work) — GM will be reachable via a generic pool, not a
thematically-precise Federation-only location, until that system
exists.

**Not yet tested in-game.** Real checkpoint: fresh Career, fast-forward
past day 311, check whether GM actually appears purchasable in a system
store — mechbay/Skirmish visibility alone won't prove the shop-
injection half works, same lesson as Zaku's own two-stage confirmation.

**Confirmed in-game, Skirmish (chat session, 2026-08-28):** Guncannon,
Guntank, Prototype Gundam, and Zaku II all load and are selectable in a
Skirmish match. **GM correctly not included in this test** — its unlock
is shop-purchase-gated, not mechbay-visibility-gated, so a Skirmish load
can't prove it either way. GM's own checkpoint above (fast-forward to
day 311, check system store purchasability) remains the only real test
for it and is still outstanding.

## Core-roster batch: 2 vehicles + 5 Mobile Suits, plus a Skirmish
## dev-QoL fix (2026-08-29 to 2026-08-30)

User completed a broad OYW research pass in their own session,
expanding `INDEX.md` to ~25 "Not Started" Mobile Suits across both
factions plus a brand-new Vehicles category. Full plan-mode session
(`curious-forging-whale.md`) scoped this down to a realistic first
batch: 2 vehicles (already specified by the user) + a 5-unit "core
roster" pass, following the exact established per-unit workflow
throughout.

**Vehicles (first `VehicleDef`/`VehicleChassisDef` builds in this
mod):**
- **Type 61 Tank** (Federation) — Bulldog donor (heavy/tracked), twin
  150mm cannons + coax/cupola MG. See `Vehicles/Type_61_Tank.md`.
- **Magella Attack** (Zeon) — Striker donor (medium/wheeled). User
  originally wanted a hovertank donor; confirmed exhaustively (checked
  `movementType` across all 20 stock `vehiclechassisdef_*.json` files,
  including both Castle Brian "SLDF Drone" ambush vehicles the user
  specifically remembered as hovercraft) that **no hover movement type
  exists anywhere in this game** — wheeled is the closest analog. See
  `Vehicles/Magella_Attack.md`.
- First use of a real, genuinely different engine schema
  (`Locations`: Front/Left/Right/Rear/Turret, no heat sinks, simpler
  inventory entries than `MechDef`).
- **Confirmed clean-loading in `ModTek.log`** (zero exceptions across
  Add/Merge/Indexing for both). **Real combat testing deferred** — user
  confirmed Skirmish cannot spawn `VehicleDef`s at all for either side
  (confirmed via decompile: `SkirmishUnitsAndLances`, the whole backing
  data structure for Skirmish's lance system, only has a `MechDef`
  variant, zero `Vehicle` references anywhere). Testing these properly
  would need a custom contract/encounter file explicitly spawning them
  as OPFOR — user chose to accept clean-load as sufficient for now
  rather than build that.

**Mobile Suits (5, "core roster first" — user's own scope choice over
building all ~25 at once):**
- **Zaku I (MS-05)** — pre-war (Jul 0077), available from Career start.
  Reuses Zaku II's heat hawk `UpgradeDef` directly (same weapon).
- **Gouf (MS-07B)** — melee/close-assault specialist. First unit with
  genuinely fixed (not canon-handheld) primary weapons on both arms —
  forearm machine guns (LeftArm) + heat rod (RightArm, new `UpgradeDef`,
  `+60`). Required swapping Dragon's default arm-hardpoint assignment
  (donor's own LeftArm=Melee/RightArm=Energy is backwards for Gouf) —
  a clean, low-risk customization since this mod always authors its own
  `Locations` regardless of donor.
- **Dom (MS-09)** — first Assault-class unit built. Genuinely
  hover-mobile (381 km/h canon top speed) — the only unit so far to get
  a *deliberate* movement-stat bump above the reused-donor baseline,
  specifically so its "fast heavy attacker" role isn't silently erased
  by reusing identical numbers to every slower unit. Chest-mounted
  scattering beam gun (first Zeon fixed energy weapon) + heat saber
  (`UpgradeDef`, `+65`).
- **Gundam Ground Type (RX-79[G])** — second real Gundam-lineage unit
  (Luna Titanium armor, built from RX-78-2 spare parts). First beam
  saber built the *correct* way from day one, no compromise — twin
  leg-mounted sabers modeled as one `UpgradeDef` (`+70`, its highest
  melee bonus of any unit except the Prototype Gundam's own hero-tier
  flat 115) — redundant-backup pairing, not stacking, matching this
  mod's no-stacking-melee-bonuses precedent.
- **Ez8 (RX-79[G]Ez-8)** — field-repaired Ground Type variant. Reuses
  Ground Type's beam saber `UpgradeDef` and the shared Federation Vulcan
  gun directly — same frame, same weapons, no new weapon files needed.
  **Lower-confidence build** — MechaBay's own page 404'd this session,
  so specs came from WebSearch snippets instead of a direct fetch,
  flagged per this project's own tooling-note convention rather than
  presented as equally solid.
- **Consistent scope call across Gouf/Dom/Ground Type/Ez8:** all four
  ship available from Career start rather than each getting a one-off
  date-gate Harmony patch entry (which would have meant 4 more
  near-duplicate blocks in the same session). Only GM keeps its precise
  Operation Odessa date-gate, since that's tied to real narrative
  significance already locked into the roadmap. Flagged clearly in each
  research doc and `INDEX.md`, not hidden.
- All 5 built clean, validated, wired into `mod.json` (10 new
  Manifest-relevant files across chassis/movement/weapon/upgrade/mech
  entries). Research entries: `MS/MS-05_Zaku_I.md`,
  `MS/MS-07_Gouf.md`, `MS/MS-09_Dom.md`,
  `MS/RX-79G_Gundam_Ground_Type.md`, `MS/RX-79G_Ez8.md`. `INDEX.md`
  updated throughout, not batched at the end.

**Skirmish dev-QoL fix, with a real mistake worth remembering the shape
of:** user asked to hide stock BattleTech 'Mechs from Skirmish so the
mod's own ~10 units are easy to find while testing. First attempt
patched `BattleTech.UI.LanceConfigurator.Initialize` — compiled clean,
deployed clean, `ModTek.log` showed no errors, and **had zero effect**,
because `LanceConfigurator` turned out to be Career mode's real
mission-prep lance screen (confirmed via decompile:
`LanceConfiguratorPanel` references `SimGameState`/`Contract`/
`Barracks`), not Skirmish at all — a completely different class. Worse
than a no-op: that patch would have silently filtered stock 'Mechs out
of Career's own real mission-prep screen too, an unintended effect on
actual gameplay. **Lesson: a clean build and a clean deploy log are not
confirmation a Harmony patch targets the right class — only the actual
in-game effect is.**

Correct target, found by decompiling `SkirmishMechBayPanel` directly:
`RequestResources()` bulk-loads every `MechDef` in the game into a
private `stockMechs` list via an async load callback; `RefreshMechList`
(only ever called post-load) combines `stockMechs`+`customMechs` into
the public `allMechs` field and hands it to the mechbay widget.
Corrected patch: a **Prefix** on `RefreshMechList`, filtering
`___stockMechs` in place. Added a shared `unit_gundamuc` MechTag to all
10 existing MechDefs (new standing requirement, documented in
`03-TECHNICAL-NOTES.md` — every future unit needs it too, same
silent-failure shape as forgetting `unit_release`).

**Confirmed in-game (2026-08-30):** Skirmish mechbay shows exactly 10
'Mechs — precisely this mod's full roster, zero stock content. Fix
works correctly on the second, corrected attempt.

**Still outstanding from this whole batch:** real combat testing for
Zaku I/Gouf/Dom/Ground Type/Ez8 (mechbay visibility confirmed, weapon
function/melee-bonus-application not yet confirmed the way Zaku
II/GM's heat hawk and beam saber were) — same two-stage confirmation
discipline as every prior unit. Vehicle combat testing remains deferred
per the user's own choice above.

---

## See also

`docs/05-COMMANDS.md` — common terminal commands and workflows (building
the Harmony DLL, decompiling game assemblies, log grepping, cache
clearing, JSON/manifest validation, packaging). Added so routine work
doesn't require a Claude Code session.

**Claude Code: keep `05-COMMANDS.md` current** — add any command used
more than once, with a one-line note on why it's needed.
