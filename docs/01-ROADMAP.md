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

## Milestone 0.1.0 — Finish Step 6: night missions

The clock exists; this is the payoff it was built for. Launching at 02:00
should produce a genuinely harder night fight.

- Harmony override of `LineOfSight`'s spotter/sensor-range methods, driven
  by `GundamUCClock`'s hour-of-day (RogueTech's LowVisibility does exactly
  this — proven pattern, see `03-TECHNICAL-NOTES.md`).
- Vanilla's `MoodController` day/night tags are **visual/audio only** and
  baked per map — they are not a gameplay switch and cannot be set from a
  contract. Confirmed. Our clock must drive this itself.

**Done when:** a mission launched at night measurably reduces detection
range and the player can feel the difference.

---

## Milestone 0.2.0 — Sides: Federation vs Zeon

**The single biggest quality jump available, and the current worst bug in
the experience.** Right now the player fights Gundams and Guncannons as
enemies because nothing separates sides.

- A faction/side tag scheme on units (`NoFaction` does nothing — verified;
  nothing reads unit faction tags during lance generation).
- Mod-authored `LanceDef`s with faction-tagged `LanceTags` and
  `unitTagSet`s that select Zeon units, bound to contract faction —
  either via `ContractOverride` files or a Harmony patch injecting into
  `LanceOverride.lanceTagSet` before `RequestLance`.
- An exclusion mechanism so **stock Inner Sphere 'Mechs stop appearing**
  and Federation units stop showing up on the enemy side.

**Done when:** every enemy lance is Zeon, every allied unit is Federation,
and no Locust ever appears again.

---

## Milestone 0.3.0 — The war has a shape

The year currently has exactly one dated event in it (the GM unlock).

- War phases across UC 0079 with escalating opposition (early Zaku-only →
  Gouf/Dom later), reusing the proven date-gate pattern.
- The major historical beats as scripted events or contracts.
- Date-gated unit availability for both sides.

**Done when:** fighting in month 10 feels different from month 1, and the
campaign reads as a war rather than random contracts.

---

## Milestone 0.4.0 — The war ends

- Campaign concludes at the OYW's canonical end (Jan UC 0080, ~day 377).
  Stock `GameLength` is 1200 days, which currently runs to UC 0082.
- A real conclusion — victory/defeat state, not just the vanilla career
  score screen.

**Done when:** you can reach the end of the war and be told you reached it.

---

## Milestone 0.5.0 — It reads as Gundam

- UC contract names, mission briefings and flavour text replacing the
  3025 Aurigan Reach writing the player currently sees on every mission.
- Faction identity: the player works for the Earth Federation, not Davion.

**Done when:** nothing in normal play says "Aurigan" or "Davion".

---

## 1.0.0 — Full playthrough, tested

Play the entire war start to finish. Balance pass on tonnage/damage/
economy. Fix what that surfaces. Tag `1.0.0`.

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
lore encyclopedia · mission-duration hour costs · travel-time rescaling
(deferred by decision 2026-09-18 until map work)

**Rule:** anything on this list that becomes genuinely blocking for 1.0.0
gets promoted *deliberately*, with a note here explaining why — not
absorbed silently mid-slice.
