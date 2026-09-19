# Roadmap to 1.0.0 — A Playable One Year War

**Rewritten 2026-09-18** after a full audit of what is actually wired up
versus merely built. The previous version of this file was written before
most of the mod existed and had gone stale (it still claimed "the mod
currently has no enemy units", which is false).

## What 1.0.0 means — the scope firewall

> **1.0.0 = a complete, winnable One Year War campaign. You play a
> Federation commander, you fight Zeon, the war progresses through its
> historical year, and it ends.**

That is the whole definition. If a feature is not required for that
sentence to be true, it is **post-1.0.0** and does not get built first —
no matter how good an idea it is. `02-FEATURE-LIST.md` is a **wishlist,
not a plan**; see the deferral list at the bottom of this file.

Versioning follows `06-GIT-WORKFLOW.md`: `0.0.X` per verified slice,
`0.X.0` per completed milestone, `1.0.0` at the definition above.

---

## Done (tagged v0.0.2)

- **Toolchain** — ModTek pipeline, Harmony patching, build/deploy loop.
- **Units** — 10 MechDefs (Guntank, Guncannon, Prototype Gundam, Zaku I,
  Zaku II, GM, Gouf, Dom, Ground Type, Ez-8) + 2 vehicles (Type 61,
  Magella), stat-converted with movement/weapon/chassis defs.
- **Campaign start** — UC 0078-12-30, starting lance of 3x Guncannon +
  1x Guntank, UC pilot name lists, stock Inner Sphere flavour events
  suppressed.
- **Prototype Gundam delivery** — scripted day-1 Career event.
- **GM date-gate** — becomes purchasable day 311 (Operation Odessa).
- **Equipment systems** — hand-weapon Handheld panel + one-weapon-at-a-time
  exclusivity + arm-loss rules; Luna Titanium armour-weight efficiency;
  tiered reactors; MechLab swap-picker restricted to mod gear.
- **Hour-granular time** — true 24-hour clock, mechanically real (repairs,
  injuries and travel resolve on hour boundaries; date, events, contracts,
  Flashpoints and finances stay day-scale).

---

## Milestone 0.1.0 — Finish Step 6: sorties and night missions

The clock exists; this is the payoff it was built for.

- **0.0.3 — Sortie time cost.** A mission consumes 4-8 in-fiction hours
  (carrier-operation scale), advancing the hour clock on contract
  completion. Now that repairs resolve hourly, this is what makes a day
  feel like a day: launch in the morning, repair, launch again at dusk.
  Needs a hook on contract completion; not yet researched.
- **0.0.4 — Day/night classification.** Derive night vs day from
  `GundamUCClock`'s hour-of-day and make it readable at mission launch
  (and visible to the player before they commit).
- **0.0.5 — Night detection penalty.** Harmony override of `LineOfSight`'s
  spotter/sensor-range methods driven by that flag. RogueTech's
  LowVisibility does exactly this — proven pattern, see
  `03-TECHNICAL-NOTES.md`. Note vanilla's `MoodController` day/night tags
  are **visual/audio only**, baked per map, and cannot be set from a
  contract; our clock must drive this itself.

**Tag 0.1.0 when:** a mission launched at night measurably reduces
detection range, and sorties consume real hours.

---

## Milestone 0.2.0 — Sides: Federation vs Zeon

**The single biggest quality jump available, and the current worst flaw in
the experience.** Right now the player fights Gundams and Guncannons as
enemies because nothing separates sides.

- **0.0.6 — Side tag scheme.** A real faction/side tag on every unit.
  (`NoFaction` does nothing — verified; nothing reads unit faction tags
  during lance generation.)
- **0.0.7 — Zeon lance defs.** Mod-authored `LanceDef`s whose
  `unitTagSet`s select Zeon units only.
- **0.0.8 — Bind lances to contract faction.** Either `ContractOverride`
  files with matching `lanceTagSet`, or a Harmony patch injecting into
  `LanceOverride.lanceTagSet` before `RequestLance`.
- **0.0.9 — Exclude stock 'Mechs.** So Locusts and Shadow Hawks stop
  fighting alongside Zakus, and Federation units stop appearing as enemies.

**Tag 0.2.0 when:** every enemy lance is Zeon, every friendly unit is
Federation, and no Inner Sphere 'Mech ever appears again.

---

## Milestone 0.3.0 — The war has a shape

The year currently has exactly one dated event in it (the GM unlock).

- **0.0.10 — War phase state.** Date-driven company tags marking the
  phases of UC 0079, reusing the proven GM date-gate pattern.
- **0.0.11 — Escalating opposition.** Lance selection keyed to phase, so
  early war is Zaku-only and later phases bring Gouf/Dom.
- **0.0.12 — Historical beats.** The war's major moments as scripted
  events at their real dates.

**Tag 0.3.0 when:** fighting in month 10 feels different from month 1.

---

## Milestone 0.4.0 — The war ends

- **0.0.13 — Campaign length.** End at the OYW's canonical close (Jan UC
  0080, ~day 377). Stock `GameLength` is 1200 days, currently running to
  UC 0082.
- **0.0.14 — Conclusion.** A real ending with a victory/defeat state, not
  the vanilla career-score screen.

**Tag 0.4.0 when:** you can reach the end of the war and be told so.

---

## Milestone 0.5.0 — It reads as Gundam

- **0.0.15 — Faction identity.** The player serves the Earth Federation,
  not Davion.
- **0.0.16 — UC contract text.** Mission names and briefings replacing the
  3025 Aurigan Reach writing shown on every contract today.

**Tag 0.5.0 when:** nothing in normal play says "Aurigan" or "Davion".

---

## 1.0.0 — Full playthrough, tested

Play the entire war start to finish. Balance pass on tonnage, damage,
economy, and the hour-scale costs (sortie length, repair, injury, travel).
Fix what that surfaces. Tag `1.0.0`.

---

## Explicitly post-1.0.0 — do NOT build these first

All of these are in `02-FEATURE-LIST.md` and all are deferred. None are
required for a complete, winnable One Year War. Listing them here so the
decision is recorded once and doesn't get re-litigated mid-build:

crew morale & relationships · ship damage/field-repair modelling · the
Argo→custom carrier replacement · Newtype/psycommu mechanics · pilot
ejection/capture/MIA · rank & promotion system · faction reputation as a
real axis · black market · two-tier pilot pool · fatigue tracking · ship
launch catapults · storage capacity limits · salvage exchange · Minovsky
EW · mobile armours · underwater/amphibious combat · multiple simultaneous
fronts · Side colonies as map nodes · 3D model replacement · UI reskin ·
lore encyclopedia

**Travel-time rescaling — deferred by decision 2026-09-18.** Hour-granular
time collapsed transits from days to hours, which is currently too fast.
The fix needs the campaign map finished first, then tuning the stock
travel costs (`GetInSystemTransitTime`, `StarSystemNode.Cost`). **Not new
work:** stock `argoUpgrade_drive1/2` already modify the
`DriveTravelMultiplier` company stat that `GetInSystemTransitTime`
multiplies by, so in-game travel upgrades already shorten transits — the
carrier just needs reflavouring and the numbers tuning.

**Repair-bay upgrades reducing repair time — already works, no build
needed.** Stock `argoUpgrade_mechBay1/2/3` and the automation upgrades
modify `MechTechSkill`, which is exactly what the hourly
`UpdateMechLabWorkQueue()` pays into. Bay upgrades therefore already cut
repair time in hours. Reflavour and tune at 1.0.0's balance pass.

**Rule:** anything on this list that becomes genuinely blocking for 1.0.0
gets promoted *deliberately*, with a note here explaining why — not
absorbed silently mid-slice.
