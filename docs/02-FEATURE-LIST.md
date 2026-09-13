# Gundam UC Mod — Full Feature List & OYW Build Order

## 0. Decisions locked in this pass

**Milestone marker:** as of this pass, the OYW mechanical foundation is
considered design-complete. Everything from here forward (Flashpoint
chain content, war-index tuning/values, specific regional numbers) is
content and refinement built on top of a finished mechanical base, not
new foundational systems.


- **Starting roster, revised:** early-prototype Guncannon (pre-RX-77, as depicted in *Gundam: The Origin*) + early Guntank, no Gundam-lineage unit present at all except the Prototype Gundam RX-78-1 (commander's unique unlock, present from the war's outbreak — not the later-war Full Armor variant). These prototype units' lack of refinement vs. later production units is the intended "earlier challenge" — a deliberately clunkier, less refined starting force.
- **Pilot ejection — simplified to match BattleTech's default behavior.** Vanilla BattleTech already separates "mech destroyed" from "pilot dead" — pilot death normally requires a specific killing blow (cockpit called shot or a death roll), not just mech loss. **Design decision:** keep this default almost as-is, reflavored as the cockpit sphere separating/drifting free when the MS is destroyed. True pilot death stays rare and tied to cockpit called shots, same difficulty as BattleTech's existing system. Add a **per-unit "no ejection" flag** for select MS/Mobile Armor (certain Zeon designs, most Mobile Armors) that removes this protection entirely — MS destruction on a flagged unit simply means pilot death, no roll needed. This creates real per-unit risk variance without a new system, just a flag check on the existing death-resolution logic.
- **New: pilot capture as a third outcome.** Echoing the anime beat of an enemy MS grabbing and carrying off a cockpit sphere — instead of always resolving to "survived" or "died," a captured-pilot outcome can branch into a rescue mission (or a choice not to attempt one) as a follow-up narrative beat. Cheap to add (a third branch on the same resolution roll) with strong payoff for the narrative-impact pillar.
- **Psycoframe:** confirmed out of scope for OYW — defers alongside the full Newtype ability system to a later era (Zeta/Unicorn-era tech, ~UC 0087+).
- **Minovsky/EW system:** build on BattleTech's existing ECM/ECCM framework rather than a new system — just make the effect near-universal (most/all maps) instead of equipment-gated, matching the in-universe "Minovsky particles are just everywhere" premise. Cheap to implement, high flavor payoff. **Future upgrade path, not OYW scope:** a later tier could turn this from passive/always-on into an active, deployable resource the crew can trigger/clear as a tactical choice — a natural Tier 3+ enhancement once the base always-on version is proven, not a rebuild. **Real official precedent found (2026-09-01), worth using to sharpen the mechanic:** Bandai's own Gundam Assemble tabletop game (official core rulebook) implements Minovsky Particles as exactly this kind of effect — a flat -2 Accuracy penalty on any attack against a non-adjacent target when either the attacker or defender is inside a Minovsky field (not cumulative, capped at -2 even if both sides are inside), with melee/adjacent attacks completely unaffected. This turns "jams long-range sensors" from vague flavor into a precise, testable rule: a to-hit penalty conditional on range ≥2, nothing at range 1. Worth adopting this exact shape rather than inventing our own — it's a proven, official design solving the identical problem.
- **UI reskin:** confirmed grouped into the final tier alongside 3D model replacement and the Argo/ship reskin — not attempted until the gameplay/narrative layers are done.
- **Newtype potential — finalized system:**
  - Keep BattleTech's 4 skill branches (Gunnery, Piloting, Guts, Tactics), each now ranging 0-30 instead of 0-10.
  - Ability-tag thresholds shift proportionally: 2 first-tier abilities unlock at 10 (was 5), 1 second-tier ability unlocks at 20 (was 8) — same specialization pattern as stock BattleTech, just rescaled.
  - Precedented: RogueTech already extends skill caps past vanilla 10 and rescales tag thresholds via Harmony patches — this is proven-feasible modding territory, not novel.
  - Newtype bonus (+1 latent / +5 early / +8 awakened / +15 fully awakened) is implemented as a **separate additive layer applied at dice-roll resolution** (to-hit, evasion, initiative, etc.), rather than allowing the raw stored skill value to exceed 30. This lets a fully-awakened Newtype genuinely outperform the 0-30 cap in actual play without breaking tag-threshold logic or UI elements that assume a bounded skill range.
  - **Counter-mechanic:** a unit-level flag (Unicorn/psycommu-interceptor-style "NT-D system") suppresses the opposing pilot's Newtype bonus layer specifically when fighting a flagged unit — reuses the same conditional-check pattern as the bonus layer itself, so it's a small addition, not a new system. Mechanically this makes late-game Newtype-countering units a genuine "your ace is back to mortal" threat.
  - Awakening trigger tracking (time-in-service, battle count, close-call counter) still needed as supporting infrastructure — see below.
  - **"Killing intent" precognition — a specific ability manifestation, not just numeric bonus.** At higher awakening tiers (awakened/fully awakened), the flat additive bonus layer gains one concrete expressed ability rather than staying purely numeric — e.g. a reroll-on-miss or an early-warning/ambush-immunity effect. Doesn't conflict with the additive-layer system already locked in; it's a specific flavor manifestation of that bonus at the higher tiers, not a separate mechanic.

- **Ship boarding actions: cut from scope entirely.** This is an MS combat game, not an infantry game — confirmed removal, not just deprioritized.
- **Called shots / cockpit kills:** kept as rare/hard to land, matching BattleTech's existing called-shot difficulty curve.
- **New: reactor critical hits.** A destroyed MS can explode and damage nearby units/structures/ship interiors — reuses BattleTech's existing ammo-explosion-crit mechanic and blast-radius logic rather than needing a new system from scratch.
- **Miss vs. graze — a real distinction for very powerful weapons, flagged as unverified before building.** Concept: a "clean miss" deals zero damage as normal, but a sufficiently powerful weapon (Beam Magnum-tier, mega particle cannon) can "graze" on a failed to-hit roll — technically missing the primary hit point but still dealing reduced/partial or even devastating damage, since a beam that strong shouldn't behave like a binary hit/miss rifle. **Real technical hook found tonight:** the WeaponDef files pulled during unit-building both had an `"AOECapable"` field (set false on the examples seen) — vanilla BattleTech's schema already has some area-effect weapon concept built in, which is the natural reflavor target for this rather than inventing new attack-resolution logic. **Unverified, don't build on this assumption yet:** unclear whether AOE weapons deal damage on a failed to-hit roll (which is what "graze" needs) or only hit multiple targets on a successful roll (a different mechanic entirely). Needs checking against an actual stock `AOECapable: true` weapon file before designing any weapon around this — same discipline as every other schema assumption tonight.
- **Crew morale:** overall crew-wide morale plus per-pilot buff/debuff modifiers, MekHQ-style.



| Feature | What it is |
|---|---|
| **Pilot Reflex vs. Machine Speed (2-factor evasion)** | Split BattleTech's single evasion-chevron stat into pilot skill (reflex/piloting) and suit performance (thrust/mobility rating), combined to produce the chevron total. A green rookie in a fast suit and a veteran in a slow suit land at similar evasion through different math. |
| **Hour-based time / injury realism** | Replace day-granularity scheduling with hour-granularity. Missions consume 6-8 active hours. A wounded/lost pilot is out ~2 full weeks, not an abstracted "a few days." |
| **Argo → custom proto-carrier** | Swap the stock ship for an original MS carrier hull, later upgradeable (echoing Nahel Argama-style progression from the original pitch). |
| **3D model replacement + date-gated MS unlocks** | Long-term asset goal (last-step per your call), plus units becoming available on their historical UC release timeline. |
| **Narrative impact of player choices** | Player actions shift faction relations, battle outcomes, character survival (e.g., Char can die and stay dead). |

## 2. What I think is missing

Grouped by how load-bearing each one is.

- **Recruitment & basing — restructured to three tiers, home base always accessible regardless of faction standing.** Generic friendly bases (frequent, fast, faction-aligned) handle resupply, ammo, minor repairs, and recruitment (rookie and veteran) — keeps the mission-to-mission loop moving, and can occasionally come under attack itself, turning a routine stop into a defensive mission (flagged for later fleshing-out, not needed for the first milestone). These may become unavailable/hostile later if the player's faction standing shifts (Titans-era alignment problems). **The player's own home base is separate and always accessible no matter who you're allied with (or not allied with at all)** — this is where everything happens, including major ship upgrades (the carrier physically growing over the campaign — more turrets, more hangar capacity, eventually stepping toward a Nahel Argama/General Revil-class silhouette). This absorbs and broadens the earlier "asteroid shipyard" concept — home base does everything a shipyard did, plus resupply/repair/recruitment, and importantly can never be cut off by a faction schism the way generic friendly bases can.
- **Black market — explicit system, not just a property of currency.** Locked in as a real location-based system: a vendor available at the home base, plus 1-2 neutral colony sites elsewhere on the campaign map. This is the concrete mechanism behind "credits still work even when official Federation requisition is cut off" — becomes especially relevant once faction standing drops or a later-era schism (Titans alignment) closes off generic friendly-base access.
- **Two-tier pilot pool.** Cheap, plentiful, disposable rookies (Thunderbolt-style — thrown into support roles, expected attrition) versus rare, expensive veteran pilots recruited or poached occasionally. Gives the commander's Prototype Gundam a clear mechanical role as primary damage dealer while rookies screen/support and grow into veterans by surviving — reinforces the intended emotional arc.
- **Funds — Federation credit, tracked as two separate values.** (1) Credit balance — spendable currency, survives later even if official channels don't (black market/salvage sales still work when a faction-alignment schism cuts off official requisition). (2) Standing with your own chain of command — separate from faction reputation with the enemy, affects resupply speed/discounts/promotion/mission access. Building this as two axes now costs little and avoids a retrofit when later eras (Titans-era alignment problems) need it.
- **Ship section field-repair — diminishing returns, not full fix.** Crew can field-repair damaged ship sections/armament, but only ever back up to 50% of what was lost. Damage compounds over a campaign without a return trip — creates real pressure to go back to home base rather than tanking damage indefinitely. A destroyed non-vital section (exact list TBD) can be field-repaired once (back to 50%); a second destruction of the same section makes it permanently gone, echoing BattleTech's permanent-location-destruction stakes but applied to the ship. Like BattleTech's repair-priority system, the crew can't fix everything at once — repairs must be prioritized. Full repair/major upgrades only available at the home base (see basing note above).
- **Ship damage visuals — two very different difficulty tiers, worth staying deliberate about which one gets built.** (1) Cheap: a static portrait/icon swap in menus and transitions (healthy → damaged → critical, 3 fixed images swapped at HP thresholds, e.g. <50%/<25%/destroyed) — no different in kind from any other 2D image swap, buildable well before real 3D assets exist. (2) Expensive: a dynamic 3D damage state on the actual ship model changing in real time — same effort tier as the custom ship replacement itself, since it needs multiple damage-state meshes/textures on a model that doesn't exist yet. **Recommendation: build (1) as the real target; only consider (2) as a stretch goal bundled into the final ship-model asset phase, not before.**
- **Ship launch catapults — new concrete battle mechanic.** The ship-as-participant concept (damageable sections, defensive fire) gets one more piece: active mid-battle launch/recovery of units from the ship itself — the classic "launch!" beat. Genuinely new, not yet covered by the existing damageable-building/defensive-fire design — a reinforcement or emergency-recovery action tied to the ship's own turn, not just passive support fire.
- **Ship crew quality — merged with morale rather than a separate third system.** Considered a standalone ship-crew experience/veterancy track (distinct from MS pilot skills and from crew morale) — verdict: not overkill as a concept, but better folded into the existing crew-morale system as one combined "Crew Quality" stat than stood up as a fully separate parallel system, to avoid three interlocking veterancy tracks a solo project has to balance. Give it one concrete mechanical payoff so it's not flavor-only: ties to field-repair effectiveness (better crew = closer to the 50% repair cap, or faster repairs). Casualty/replenishment mechanic kept as designed: a ship-section destroyed past a damage threshold causes crew casualties in that section; replacement crew quality is a weighted blend of surviving veteran experience + number of novice replacements taken on.
- **Pilot capture — MIA mechanic finalized.** A captured pilot is marked MIA with a (tunable, currently 2-day) window to attempt a rescue mission before the outcome is treated as permanently lost. Closes the open question flagged after the ejection/capture design pass.
- **Deferred for later investigation — funnels** (not yet researched). **Wire-tethered psycommu-test unit — researched and confirmed: the MSN-01 Psycommu System Zaku.** Part of Zeon's Bishop Plan (Newtype-weapon research, Flanagan Institute). Each forearm is a detachable 5-barrel mega particle cannon (fire-linked, individually adjustable, hard to evade, warship-killing damage) that operates remotely via thick control wires when detached — tethered, not wireless, exactly the mechanic described. The high-mobility variant even removes the legs entirely for thruster-based mobile-armor-comparable speed, at the cost of very limited operating duration (propellant constraints). Canonically appears at **A Baoa Qu**, the war's final major battle — strong candidate for the "Great salvage" rare hero-unit slot in the salvage table, or a narrative encounter tied to that battle specifically, rather than a starting/common unit.
- **Mobile Armors — distinct unit category, separate from MS.** Rare/boss-tier encounters (Big Zam, Elmeth, Bigro-class), not part of the regular roster progression. Pairs naturally with the "Great salvage" hero-unit slot and the psycommu Zaku precedent above — a genuinely different unit type from Mobile Suits, not just a bigger MS.
- **Underwater/amphibious combat — distinct environment, genuine gap.** Zeon's roster includes amphibious-specialist units later in the timeline (Z'Gok, Gogg, Acguy) — this environment type isn't covered anywhere in current scope and needs its own movement/combat ruleset consideration alongside the already-planned space and Earth-biome environments.
- **Side colonies (1-7) as explicit map nodes** — extends the node-graph campaign map (17a) with the actual Side colonies, not just generic "Lagrange points/space colonies."
- **Multiple simultaneous Flashpoint fronts — the war is bigger than the player, structurally.** Several major historical battles happen in parallel on the campaign map; the player physically cannot attend all of them (time, distance, readiness), and choosing one means missing or only indirectly affecting the others. This connects directly to the war-index/sector-control mechanic already locked (16b) — skipping a front lets that region's index drift on its own rather than under player influence. Genuinely the most significant structural gap found in this pass — worth designing properly alongside the Flashpoint chains (16a), not an afterthought.
- **Reuse BattleTech's existing Flashpoint tools rather than inventing new ones.** Consecutive-deployment limits, tonnage caps, no-repair windows — these are already-built mechanical tools from the base game's Flashpoint system, meant for exactly the kind of high-intensity multi-mission sequences the historical battle chains (16a) need. Same "reflavor before invent" discipline as everything else tonight.
- **Recruiting captured/defector Zeon pilots, with a loyalty-risk mechanic.** A specific recruitment source not yet captured in the existing pilot-pool design — narratively rich (a defector who might not be fully trusted) and mechanically distinct from rookie/veteran recruitment.
- **Lore tooltips/encyclopedia entries** for MS, battles, and characters — cheap (mostly writing, no new systems needed) with high atmosphere payoff. Polish-tier, but worth remembering since it's unusually low-cost for what it adds.
- **Atmosphere re-entry damage risk** — a specific detail worth attaching to the Earth-drop mechanic already locked (part of the space/Earth theater system): dropping to Earth isn't risk-free even before combat starts.
- **Field salvage — weighted outcome table, resolved post-battle or after major story missions.**
  - Nothing
  - Good salvage (useful parts/materials)
  - Great salvage (rare, intact hero-tier unit — e.g. a Zaku III high-mobility variant, or similarly rare Gundam-lineage salvage)
  - Trap (someone gets injured — feeds directly into the existing hour-based injury/recovery system)
  - Ambush (the salvage run turns into an unplanned battle)
- **Ship storage capacity — real limits, fixing vanilla BattleTech's "unlimited Argo mech bay" problem.** Split into two distinct pools: (1) deployable MS slots (how many mechs the hangar can hold/launch) and (2) spare-parts/salvage storage (separate, probably smaller pool). Keeping these separate creates a real decision on major-battle salvage runs: bring home an intact unit (uses a full MS slot) vs. strip it for parts (less space, more flexible, but the whole unit is lost). **Real capacity numbers depend on the custom ship design, which is still a Tier 4/"last, always" item — locking in the principle and mechanic now, deferring actual numbers to that design pass.** Home-base ship upgrades are the natural way to increase both pools over the campaign.
- **Salvage exchange system at base — a real resource-allocation trade-off, not just "sell for credits."** Salvaged MS parts and ship parts can be converted at the home base into either (a) accelerated Federation MS tech development (speeding up date-gated unlocks) or (b) boosted ship repair (beyond the normal field-repair 50% cap, or faster repair time). Forces a meaningful choice on what limited salvage haul actually becomes.
- **This reinforces the whole "return to base" logistics loop already built** — ship field-repair capping at 50%, spaceport-gated Earth/orbit transitions, and now real storage limits — all pushing toward the same design goal: periodic returns to base are a real gameplay necessity, not just a menu you can ignore.
- **Minovsky particle interference as a core combat mechanic.** This is the single most important Gundam-flavor system you don't have yet. In-universe, Minovsky particles jam long-range sensors/guided weapons, which is *why* Gundam combat is close-range beam/melee brawling instead of missile spam. Mechanically this could gate sensor range, disable BattleTech's indirect-fire/LRM-style mechanics in Minovsky-saturated zones, and justify melee (beam saber) prominence. This one system does a lot of flavor work for very little new code — mostly a sensor-range/weapon-class modifier.
- **Called shots / cockpit kills.** BattleTech already has called-shot mechanics; UC pilot deaths are often brutal, specific, named-character moments (a cockpit hit killing a specific person your crew know). Worth deciding early whether you lean into this for narrative weight.

### Missing — important (drives the "misfit crew drama" feel)
- **Crew morale & relationship system.** You mentioned this in the original pitch but it dropped out of recent discussion — worth re-flagging since it's core to "ragtag misfit crew" as a premise, not just flavor text.
- **Random ship-life / away-mission events — reuse the existing system, don't build a new one.** BattleTech already ships a working ship-event framework tied to the Argo (triggers, popup structure, choice/outcome resolution). The plan here is to **reflavor and rewrite the existing event content and triggers** to fit UC (mess hall arguments, repair-bay banter, a refugee taking shelter aboard), not build a parallel new event system — same "reflavor before you invent" principle as the very first roadmap step, just easy to lose track of by the time this specific feature comes up. These are what make hour-based fatigue tracking *feel* like something rather than just a cooldown timer.
- **Mission variety beyond "kill the enemy squadron."** Escort/evac, boarding actions, infiltration, defend-the-carrier-while-it's-repairing. UC lore is full of these (colony evacuations, Operation Odessa raids, boarding actions on captured ships).

### Missing — later-era, don't build yet but plan for
- **Newtype potential.** Genuinely doesn't belong in the *first* OYW milestone — Newtype phenomena are nascent/rare even in-universe during OYW (Amuro is an outlier, not the norm). Worth stubbing as a hidden per-pilot stat now (cheap), but don't build the ability system until you're modeling later eras.
- **Faction reputation across multiple factions (Zeon remnants, AEUG, Titans, Neo Zeon).** Only Federation vs. Zeon exists in OYW. Build the reputation *framework* generically now if you want, but only OYW factions need real content yet.

### Missing — surfaced by this pass
- **Newtype awakening trigger tracking.** If awakening is driven by time, battle count, and close calls, each pilot needs a persistent tracker for these (a "close call" counter incrementing on near-death/critical-armor-loss events, a battles-survived counter, a calendar-time-in-service value). This is small but easy to forget until you're deep into building the Newtype system.
- **Named enemy aces.** A classic UC trope (Ramba Ral, the Black Tri-Stars, Char himself) — a recurring, named, mechanically-distinct enemy pilot who shows up across multiple missions and can be a genuine narrative rival. Fits naturally into the narrative-impact and mission-variety tiers you already have; flagging it explicitly since it's a low-cost, high-payoff addition (mostly narrative/AI-loadout work, not new systems).
- **Space battle rules — new mechanical system, decoupled from map assets.** Vanilla BattleTech has zero zero-G physics: no gravity means the fall/knockdown mechanic doesn't apply the same way, movement could plausibly go fully 3D (vertical axis, not just X/Y), and ground-map terrain-cover mechanics don't translate. Importantly, this ruleset can be prototyped and tested on an existing flat/open stock map as a placeholder — it doesn't need to wait on real space maps existing, so it can be built mid-project rather than blocked until the final asset phase.
- **Earth-drop / spaceport logistics — historically-grounded strategic asymmetry.** Mirrors real OYW history: dropping onto Earth from orbit was relatively easy (atmospheric reentry pods), but getting back to space required controlling one of a limited number of spaceports — a real strategic chokepoint in the actual war, not an invented mechanic. Mostly campaign-state and travel-cost rules layered on the existing basing/logistics system — no new assets needed.

### Missing — polish, genuinely last
- UI/HUD reskin (Gundam-style cockpit readouts, character portraits)
- Music/sound
- Any multiplayer consideration (skip entirely unless you have a specific reason)

## 3. Deferred — Later Mod Addition (Post-Core-Loop, Not OYW-Specific)

These aren't tied to any particular UC era the way Newtype/psycoframe are —
they're systemic additions meant to come after the core OYW loop is
working, not part of the initial milestone build order above.

### PTSD & permanent wounds (Battle Brothers-style, Thunderbolt-inspired)
- Reuses existing infrastructure rather than a new parallel system:
  - The close-call counter (already planned for Newtype awakening
    triggers) is the natural trigger source for a wound/trauma roll.
  - The existing hour-based injury/recovery system gets a third branch:
    full recovery, permanent physical wound (stat debuff, possibly with
    a prosthetic-recovery story arc later — direct Thunderbolt parallel,
    Io Fleming/Daryl Lorenz), or PTSD (mental-side debuff, potentially
    worsened by repeated close calls, mitigated but not necessarily
    fully cured by extended downtime — more true to how trauma actually
    resolves than a clean heal).
- Prosthetics as a narrative beat: a wounded pilot returning to service
  with a prosthetic is a redemption/continuation arc, not just a
  debuffed stat line.

### Weapon actuator/frame-strength requirements (Unicorn-era, later addition)
- Mechanic: certain high-tier equipment (Beam Magnum-tier) has a minimum
  required actuator/frame-strength rating to safely wield. A unit below
  that threshold can still equip the weapon — closer to the fiction than
  a hard block — but firing it risks self-damage to the wielding arm,
  matching the source material (a normal MS destroying its own arm from
  the recoil; only Unicorn-type units meet the threshold by default).
- **Ties directly into the MS-bay internal-components system already
  locked (17b):** the Joints/Actuators category is exactly what would
  need field-refit upgrading to raise a unit's rating past a weapon's
  threshold — the same salvage-exchange-funded refit path already
  designed, not a new progression system. A non-Unicorn unit earning the
  right to safely wield something that powerful becomes a real,
  hard-won milestone rather than a shop unlock.
- Correctly out of OYW scope — Unicorn-era tech (UC 0096) is well past
  the current build target. Flagged here so it isn't lost before the
  mechbay design session eventually needs it.

### Commander mortality — configurable at campaign start

**Starting character concretized:** commander begins as an **Ensign**,
age 18-20, at the outbreak of the OYW (UC 0079) — a young officer with
"natural talent" (justifies above-baseline rookie stats without breaking
the "you start as a nobody" feel), not an already-veteran pilot. Newtype
status, if any, could be either a player choice at character creation or
a hidden trait revealed through the existing awakening-trigger system
(time/battles/close calls) — genuine open design fork, not resolved yet.
This confirms the ~26-year span to the Hathaway era (UC 0105, commander
~45) discussed below, and sets a practical soft cap on how far a
full-timeline campaign would realistically extend — bounding the aging
question to a known window rather than an open-ended problem.

- Problem this solves: unlike vanilla BattleTech (abstracted commander,
  never dies, only bankruptcy ends a campaign), our commander is a
  specific story-critical pilot (Prototype Gundam). Without an explicit
  decision, they're as vulnerable as any other pilot under the systems
  above — which could end a campaign in an unsatisfying way rather than
  a dramatic one.
- Proposed as a campaign-start option, four settings, each with a distinct
  failure state rather than just "harder combat":
  - **Full** — commander follows all the same rules as any pilot
    (physical wounds, PTSD, capture, death). **Death ends the campaign.**
    This is the actual stake being opted into.
  - **Physical only** — can be wounded/killed physically, immune to
    PTSD/mental effects. **Death ends the campaign**, same as Full, just
    without the mental-trauma pathway also being live.
  - **Mental only** — can suffer PTSD/psychological effects, immune to
    physical death/wounds. **No game over from this** — instead,
    accumulated trauma past some threshold makes the commander
    permanently undeployable (benched from active piloting duty, not
    dead). A quieter, different kind of loss than the other two modes.
  - **Story mode / immortal** — fully protected, the "classic
    BattleTech" experience for players who want the strategic/narrative
    game without personal stakes on their own character
- **Timescale matters a lot here.** Within the OYW alone (roughly one
  year), this system will rarely matter — not enough time to accumulate
  meaningful trauma/attrition. It becomes genuinely significant if the
  campaign timeline is later extended toward Hathaway (UC 0079-0105) —
  a 26-year span for one character's career, long enough that this stops
  being an edge case.
- **Related future question, not for OYW scope:** if the campaign
  eventually spans the full 26-year timeline, should the commander
  physically age over that span — declining reflexes, eventual
  retirement, possibly a succession mechanic (a protégé or successor
  taking over command)? Not something to design now, but worth having on
  the radar once full-timeline scope becomes an actual build target
  rather than the original pitch's long-term ambition.
- **Open question, not yet resolved:** if "Full" or "Physical" is
  selected and the commander actually dies — confirmed as campaign-ending
  above. Still open: does that mean a hard "Game Over" screen, or some
  softer wrap-up (e.g. an epilogue reflecting how far you got)?

## 4. Proposed build order — One Year War era only

Organized in tiers. Finish a tier before starting the next; each tier should leave you with something playable, not just designed.

**Tier 0 — Pipeline (already underway)**
1. ModTek hello-world confirmed ✅ (in progress)
2. Guntank and Guncannon as reflavored starting units
3. Roster restriction (Guntank/Guncannon-only start, no Gundam-lineage unit purchasable yet)
4. Prototype Gundam as scripted commander unlock

**Tier 1 — Core loop viability**
5. Pilot roster & recruitment system (so losing a pilot for 2 weeks is a real decision, not a soft-lock)
6. Basic funds/logistics economy reskin
7. Hour-based time tracking replacing day-granularity
8. Pilot Reflex vs. Machine Speed evasion split

**Tier 2 — Gundam-flavor differentiation**
9. Minovsky interference mechanic (sensor range / weapon-class gating)
10. Called-shot / cockpit-kill emphasis tuning
11. Zaku and core Zeon roster as opposing force
12. Date-gated unlock framework (generalize the one-off Prototype Gundam event into a reusable system — GM as a later-war unlock once mass production begins, more Zaku variants, etc.)

**Tier 3 — Ragtag crew narrative depth**
13. Crew morale & relationship system
14. Random ship-life events between missions
15. Mission variety (escort/evac/boarding, not just skirmish-kill)
16. Narrative branch points tied to canon battles (the Char-survives-or-dies type moments)
16a. **Historical battle Flashpoint chains — named, concrete.** Multi-mission narrative arcs built around real OYW battles, mirroring the game's own Flashpoint content structure (Mission Control is the right tool for this, per the roadmap doc). At minimum: Battle of Loum (war's opening), Operation Odessa (GM's real combat debut, ties directly into the GM-unlock timing already established), Battle of Solomon, and A Baoa Qu (the war's climax — also where the MSN-01 Psycommu Zaku canonically appears, a natural convergence with the salvage-table hero unit). Each is a scripted mission chain with player agency over specific beats, not just a reflavored skirmish.
16c. **War theater phasing — the OYW is a space war first, then a ground war.** Structurally important, not just flavor: the opening ~2-3 months are fought **entirely in space** (Loum, the colony drop, fighting around the Sides). Zeon's Earth invasion only begins **after the Antarctic Treaty**. This has direct mechanical consequences that ripple through several already-locked systems:
- The space-deployment gate (item 17 — only jump/thruster-capable units can fight in space) means **non-thruster units are literally unusable during the opening phase**. The starting Guntank cannot deploy at all until the war reaches the ground.
- The Earth-drop/spaceport logistics system only becomes relevant at the phase transition.
- Earth-biome maps aren't needed at all for the opening campaign — only space maps (which are the cheap skybox-swap variety per item 17). Genuinely useful for build sequencing: **the first playable slice of the campaign needs zero Earth terrain work.**
- The phase transition is a natural mid-campaign inflection point — units, maps, and logistics rules all shift at once, and previously-useless roster members suddenly become relevant.
16b. **Sector/territory control — concrete mechanism: regional "war index."** Each region/sector tracks a numeric war index representing the controlling faction's military strength/presence there. Player actions — winning battles in that region, destroying supply assets/infrastructure — diminish the defending faction's index. Crossing a threshold can flip regional control or make an otherwise-historically-scripted battle outcome changeable (Loum is the clean example: historically a brutal early Zeon victory over the Federation fleet — if the player has driven Zeon's regional index down beforehand, that outcome becomes something the player can actually beat rather than a fixed cutscene). Exact formulas/thresholds explicitly deferred for later refinement — the mechanism (regional index, player-diminishable, threshold-triggered outcome changes) is what's locked in now. This was in the original pitch ("shorten/lengthen conflicts, change planetary/system control") but hadn't been turned into a concrete designed system until now — ties directly into the political-intrigue framework (Tier 4) as its mechanical backbone, and into the Flashpoint chains above as the missions that actually move the needle.
17. Space battle rules — zero-G movement/physics, PLUS the map trick that makes this cheap: reuse an existing flat/open stock map with its skybox swapped for a black backdrop with stars (not new terrain geometry — a visual swap hiding a normal flat map). Combine with a hard deployment rule: only jump/thruster-capable units can deploy to space combat at all (no thrusters = no maneuvering in zero-G, thematically correct and a real gameplay differentiator between prototype units and jump-capable ones). This moves space combat out of the "wait for the big asset phase" bucket entirely — it's buildable as soon as the ruleset itself is written.
17d. **Flight mobility tiers, and sub-flight systems as a distinct mechanic.** Beyond the jump/thruster gradient already built (Guncannon's limited booster capability, Prototype Gundam's full jump/thruster maneuverability), two more tiers worth separating clearly: (1) **true sustained atmospheric flight** — rare, high-tech, genuinely different from jump capability (sustained lift vs. short bursts), likely a later/rarer unlock rather than starting-roster material. (2) **Sub-flight systems ("undersling")** — a separate flying platform/vehicle that a non-flying MS rides or mounts onto for temporary aerial access, distinct from tier 1 because the flight capability belongs to the mount, not the MS itself. Genuinely OYW-era relevant, not later-era-only — early Federation material (Core Fighter combining with flight-support units) used exactly this concept. **Eligibility is gated by humanoid articulation, not propulsion capability** — mounting a sub-flight platform requires the MS to physically crouch/lie prone on it, which needs real biped joint articulation. This means eligibility doesn't track the jump/thruster gradient directly: **Guntank is excluded even from this workaround** — its single fixed posture (built more like a tracked vehicle with a torso than a true biped) can't assume the positions needed to mount one, reinforcing its established role as a purely ground-based, stationary fire base with no aerial access via any route. Guncannon, despite having no better propulsion than a clumsy booster, likely *can* use sub-flight systems since it's a proper biped with real crouch/prone articulation — giving even Earth-bound units aerial access in specific missions without breaking their established identity.
17a. **Campaign map representation — node graph, not a projected sphere.** The "how do you show a sphere like Earth on a flat 2D map" problem gets sidestepped rather than solved: almost every strategy game handles this with a node/connection graph (Risk-style regions and travel routes) instead of literal geography, and BattleTech's existing star-map/contract-selection UI is already exactly this kind of node graph — a strong reflavor-before-invent candidate. Under this model: Lagrange points/space colonies are natural single nodes (no projection problem to begin with — already point-like locations), and **Luna Two belongs in this cluster, not on the Moon** — it's a distinct artificial satellite/converted asteroid sitting at the Lagrange point opposite Luna's orbit (shipyard/base), not a lunar-surface site. The actual **lunar-surface nodes are Von Braun City and Granada** (worth confirming if there are others before building, not asserting from memory now). Earth gets split into a handful of macro theater-regions matching how OYW battles are already named in canon (Jaburo, Odessa, Belfast, etc.) rather than attempting real geography. This also matches the war-index sector-control mechanic already locked in, which assumes regions-with-values, not precise coordinates. **Not yet verified: the actual technical structure of BattleTech's star-map system** — needs a real file example before building, same discipline as every other schema assumption tonight. **Design note, locked pending this system's build (2026-08-20):** the player's home base is placed near **L1**, not L3 — small, easy for Zeon to overlook, and keeps resupply/refit genuinely close, matching the "home base always accessible" pillar (see basing note above) and the fast opening-beat pacing already locked in `01-ROADMAP.md` Step 3 (commander arrives → Gundam delivered ~1hr later → war begins ~1hr later — a far L3 base would fight that tempo). This also means Career mode's `CareerMode.StartingSystems` (currently generic stock Inner Sphere worlds, irrelevant to this setting) stays unlocked/untouched until this node-graph system actually exists — there's nothing real to point it at yet.
17b. **MS-bay customization (renamed from "mechbay" — MS terminology consistent with everything else in this doc) — hybrid model, not BattleTech's fully-open hardpoint system.** Gundam-type MS design doesn't map cleanly onto free hardpoint-swapping — a lot of armament is integrated into the chassis as part of its identity (a Guntank's twin cannons, a unit's internal beam saber rack), not realistically swappable. Proposed framework, three categories: (1) most **weapon** hardpoints are fixed/locked to that unit's built-in weapons, preserving each chassis's distinct identity — confirmed: Guntank/Guncannon-type units keep fully internal/fixed weapons (matches their established lore), while humanoid units like the Prototype Gundam get real hand-carried weapon flexibility (beam rifle, bazooka, shield) through a small number of "universal" hand/hip-mount slots that use BattleTech-style open swapping. (2) **Internal components** — reactor, sensors, battle computer, cockpit, joints/actuators — are their own separate swappable/upgradeable category, distinct from weapons. **Strong reflavor-before-invent candidate: BattleTech already has almost this exact system built in** — Engine rating, Gyro type, Cockpit type (including real upgrade variants like a Command Console), and Actuators (arm/leg actuators are already a removable/upgradeable component category in vanilla BT). Proposed mapping: Reactor→Engine, Joints/Actuators→Actuators (near-literal reuse), Cockpit→Cockpit type, Sensor/Battle Computer→likely existing sensor-range stats or a targeting-computer-style equipment slot (needs checking). (3) **Field-refit upgrades are the mechanism for pushing an older unit past a newer stock design**, rather than open customization doing it — extra armor, an unlocked backpack slot, a stat bump, earned through play rather than bought freely. This directly plugs into the salvage-exchange system already locked in: "accelerated Federation MS tech development" can specifically mean unlocking refit upgrades for existing units (Guntank/Guncannon becoming genuinely competitive through hard-won upgrades), giving salvage a concrete, felt payoff tied to customization rather than an abstract tech-tree speedup. **Genuinely its own sizeable system — flagged for a dedicated design session later, not fully specced tonight.** Open items for that session: exact equipment categories per weapon-mount type, tonnage/slot limits per chassis, whether refits are permanent or reversible, and — before any of this is built — pulling a real `MechDef` file (not yet seen, only `ChassisDef` so far) to confirm exactly how internal components attach and what's actually swappable in this engine.

**Scope confirmed/expanded (2026-08-23), still deferred to that same
dedicated session — do not build piecemeal before then:** the eventual
"major refit" covers at minimum (1) the mechbay→MS-bay rename actually
applied consistently everywhere it's used, not just noted here, (2) the
handheld/backpack hand-mount slots already specced above (Guncannon's
rifle+shield, Prototype Gundam's four items, and now Zaku's machine gun
+ heat hawk, all currently shipping as fixed hardpoints instead as a
deliberate stopgap — see `03-TECHNICAL-NOTES.md`), and (3) genuinely
more complex reconfiguration cases beyond simple item-swapping — Gundam
Thunderbolt's Full Armor Gundam is the reference example: a distinct,
heavier-armored/re-armed *configuration* of an existing chassis, not
just a different hand-item. Whether that's modeled as a whole separate
MechDef/ChassisDef, a BattleTech-style Omnimech-ish variant swap, or
something else entirely is genuinely undecided — flagged here so it
isn't lost, not scoped further yet.

**The dedicated design session finally happened (2026-08-30) — first
real slice built, not just designed.** Real engine research (not
guessed) found: BattleTech already has a native fixed-vs-swappable
inventory flag (`IsFixed`) that the vanilla MechLab UI already enforces
— the "hand" half of "hand/hip-mount slots" needed zero new UI code,
just correct data. The "hip" half is genuinely unsolved: no "hip" or
"back" `ChassisLocations` value exists anywhere in this engine, and
neither RogueTech nor BEX has ever added a new equip-slot category
beyond the stock 8 (both only add more hardpoints *within* existing
locations) — confirmed via direct research, not assumed. **Per the
user's own direction, hip/back stays deferred** until they find a
different reference mod that's actually solved it; Torso locations are
now earmarked for a future, separately-designed "reactor/internal
equipment" reflavor, not hip/back storage. Built this pass, Prototype
Gundam only as proof-of-concept: a real beam rifle (RightArm, swappable
— the unit previously had only a head Vulcan, contradicting the "four
items" description below) and the unit's own beam saber (LeftArm,
fixed, +85 melee), plus a Harmony filter restricting MechLab's swap
picker to only this mod's own `gundamuc_weapon`-tagged gear (technique
studied from RogueTech's `IMechLabFilter` pattern, not copied). Full
detail in `03-TECHNICAL-NOTES.md`. Not yet rolled out to the other 8
humanoid units, and not yet combat-tested in-game.
17e. **Mid-battle MS rotation — the carrier as an active combat participant.** Fully specced system letting damaged/depleted units cycle back to the carrier and be replaced mid-fight, rather than a battle being fought only by whoever launched at turn 1.
17f. **Hip/lower-back storage slot — pure data slot, not a rendered/damageable hardpoint.** Researched first (2026-08-28): checked whether any known BattleTech mod on Nexus adds a genuinely new body `Location` beyond the stock 8 (Head/Arms/Torsos/Legs) — confirmed **none do.** Every existing mod only edits hardpoint counts/types *within* those 8 locations, never adds a new one. Reason: each `Location` string corresponds to a real attachment point on that chassis's rigged 3D model (confirmed via the `HardpointDataDef` research earlier this session) — a new named location like "hip" has no attachment point on any existing rig, ours included (all in-mod MS units still use borrowed stock placeholder prefabs). **Decision: don't chase a new rendered `Location` at all.** Build this as a **pure data slot** — a carried-but-not-currently-equipped weapon, abstracted entirely from the 3D/hardpoint/damage system, similar in spirit to how reserve ammo is tracked. Two direct benefits: (1) fully buildable now, no dependency on custom 3D rigging existing first; (2) **the stored weapon can't be shot off or destroyed in combat**, since it isn't tied to a damageable `Location` — narratively clean (a genuine "safe reserve" until swapped into an active hand/hip-mount slot from 17b). **Not yet investigated:** whether `MechDef`'s schema already has any unused/repurposable field for "carried but not equipped" inventory, or whether this needs new Harmony work to add such a field — check against the now ten real `MechDef`/`VehicleDef` files already in the mod before assuming either way.

**Deployment sizes (three distinct numbers, don't conflate):**
- **Roster capacity** — how many MS the ship holds. Starts small (~6), grows through refits toward ~18 across the OYW, higher in later eras with better tech. In-fiction justification: the proto-carrier is an *original* design built on a converted battleship frame, not literally a Pegasus-class, so canon Pegasus capacity figures (6 standard) aren't binding — a battleship hull plausibly has more raw volume, and the ship is explicitly designed to expand during OYW and receive refit technology in later eras.
- **Standard deployment** — **4 MS** (our fiction calls this a squadron, navy-air-wing style, not a "lance"), matching both canon MS team size and BattleTech's own default group size under the hood (no engine fight — the engine's internal `Lance`-named fields/tags are untouched, only what we call it in fiction changes).
- **Expanded deployment** — up to **8 on-field**, unlocked via a **Command & Control refit**. An earned upgrade, not a starting capability.

**Rotation cycle (the core loop):**
- Damaged/depleted unit flies back to the carrier: **3 turns**, always under its own power
- Rearm: **1-2 turns**
- Repair: **+1-3 turns** depending on damage level. Field-repair only — never restores to 100%, and **destroyed/broken-off parts cannot be field-repaired at all**, so redeploying a badly-mauled unit is a real decision, not automatic
- Relaunch: **1 turn with a catapult**, **3 turns without**

**Catapults are the core progression gate, with a real early-game penalty:**
- **No catapult (early game):** launching takes the full 3 turns AND the MS arrives **already overheating** — it burned propellant getting there under its own power. Not just slower, actively worse condition on arrival.
- **With catapult:** 1-turn launch, arriving in good condition.
- **Cooldown: 3-5 turns per active catapult.** Additional catapults (later refits) directly increase rotation throughput — a concrete reason to invest in ship upgrades.

**Carrier position — on-map vs off-map, a real risk/reward tradeoff:**
- **Off-map (most missions):** the MS flies to a designated map edge — the side the carrier is standing off on, clearly marked so the player knows where to go. Reaching that edge starts the rotation cycle. Same rearm/repair timings, longer transit.
- **On-map:** faster — you fly to the actual ship rather than off past the map boundary. But the carrier is now exposed and takes damage, and **if the hangar section is damaged past a threshold, rotation capability is lost entirely for that battle.** Bringing the carrier in speeds up your loop significantly but puts the loop itself at risk.

**Why this system matters to the wider design:** it makes the deep attritable roster something felt during a fight rather than a mechbay number; it gives the ship's own battle survival real tactical stakes (connecting to the ship-section damage system already locked in); it uses the launch catapults already in the design doc as its mechanism; and it gives the expendable-rookie attrition model somewhere to go — losing a unit mid-battle isn't automatically "down a slot for the rest of the fight."

 Connects three systems already locked tonight: switching a unit between an Earth-optimized and space-optimized loadout (different thruster/backpack configuration, see 17b's backpack-swap category) requires actual time in the MS-bay before it's usable in the other theater — even a jump/thruster-capable unit (item 17's space-deployment gate) needs to be *configured* for space first, not just theoretically able to go. The downtime cost uses the existing hour-based time system (Tier 1) rather than inventing a new timer. Reinforces the "returning to base matters" design goal already running through ship repair caps and spaceport-gated Earth/orbit transitions — this is one more reason a trip home isn't optional busywork. **Open follow-on question, not resolved tonight:** should MS-bay refit slots be limited/queued (mirroring the ship's section-repair system, where "the crew can't fix everything at once")? Would turn refit timing into a real pre-mission scheduling decision rather than a fixed tax — worth deciding in the dedicated mechbay design session already flagged above.

17f. **Hip/back storage slot — resolved as a pure data slot, not a
rendered hardpoint (2026-08-30/31).** Real Gundam-verse melee weapons
(beam saber, heat hawk, heat rod) are consistently described in this
mod's own flavor text as racked on the hip/shoulder/backpack, not
carried in a hand — but every one of them currently mounts in a single
Arm location anyway, meaning losing that one arm deletes the weapon
outright. Confirmed via research (this mod's own engine decompile, plus
an independent Nexus search in a separate chat session reaching the
same conclusion) that **no BattleTech mod, ever, has added a body
location beyond the stock 8** (`ChassisLocations` is a hardcoded C#
enum: Head/LeftArm/RightArm/LeftTorso/CenterTorso/RightTorso/LeftLeg/
RightLeg) — a real rendered "hip" or "back" hardpoint would mean
inventing a wholly new MechLab UI category with no precedent anywhere,
a large and risky undertaking.

**Decision: sidestep the rendering problem instead of solving it.**
Mount hip/back-flavored items in **CenterTorso** — not because it's
anatomically the hip or back, but because CenterTorso destruction is
BattleTech's own coup-de-grace location: a mech cannot survive losing
its CenterTorso, so anything mounted there is functionally "unlosable
while the unit can still fight," without needing any new location,
hardpoint, or visual work at all. This also costs nothing new
mechanically: `ComponentType: "Upgrade"` items already don't need
hardpoint-category legality to mount anywhere with capacity (heat sinks
already do exactly this, e.g. every unit's `CenterTorso`
`Gear_HeatSink_Generic_Standard` entries), and melee Upgrade items
already carry an empty `PrefabIdentifier` (no visual prop is ever
rendered for them — the generic melee prefab is what actually swings,
per the already-confirmed `WeaponCategory.json` finding), so there was
never anything to render in the first place. First applied to Prototype
Gundam's beam saber (`Gear_Actuator_ProtoGundam_BeamSaber.json`, `+85`
melee, `CenterTorso`, `IsFixed: true`) — full bonus persists through
losing either arm, only actually lost if the mech itself is destroyed.
**Rolled out to all 7 other melee-carrying units (2026-09-01)** — Zaku
II/I's heat hawk, GM's/Gouf's/Dom's/Ground Type's/Ez8's melee weapons
all now mount at CenterTorso with the same `gundamuc_storage` tag,
using the already-generic Step 2 patch (no new C# needed). Caught and
fixed a real capacity problem in the process: every one of these items
was still `InventorySize: 3`, and CenterTorso only has 4 slots on every
chassis while already holding 2-3 heat sinks — moving them unmodified
would have overflowed on all 7 units. Reduced them all to
`InventorySize: 1`, matching the value already used for Gundam's own
saber.

**Renamed to "Hip Holder," and its category scope clarified (2026-09-02):**
the panel's on-screen label is "Hip Holder," not "Storage" (internal
code names unchanged — a display-only rename). **Category rule, now
explicit:** melee weapons and future backup/secondary guns belong in
Hip Holder; CenterTorso's own real widget is reserved for
cockpit/reactor/internal equipment only (Reactor Tuning already follows
this correctly — untagged `gundamuc_storage`, stays visible in
CenterTorso's own widget). Mechanically nothing changed — Hip-Holder
items still mount at CenterTorso under the hood for the same
coup-de-grace indestructibility reasoning above; only which items get
tagged into this category, and what the panel is called, changed.
**Forward-looking, not yet built:** no unit has a distinct backup/
secondary gun yet (today's items are each unit's one ranged weapon) —
when that gets built, it uses this same `gundamuc_storage` tag/pattern
already proven, not a new mechanism.

**CenterTorso's own future "internal equipment" system — confirmed
buildable with zero new engineering (2026-08-31), design pending a
user-drawn UC-flavored equipment list.** User asked whether RogueTech/
BEX's Gyro/Cockpit/Combat-Computer/ECM customization was real, since it
seemed to contradict the "Cockpit isn't inventory-equippable" finding
from earlier this session. Checked directly rather than assuming either
way: **both claims are true, not contradictory.** Vanilla BattleTech
genuinely has no equippable Cockpit/Gyro/Engine `ComponentType` — but
RogueTech's `Gear_Gyro_HeavyDuty.json` (and its many siblings under
`RogueModuleTech/Gyro/`, `.../Cockpit/`) build these as ordinary
`ComponentType: "Upgrade"` items mounted at `CenterTorso`, using plain
`StatisticEffect`/`Float_Add`/`Float_Multiply` entries — **the exact
same mechanism this mod already uses for the beam saber and shield**,
just targeting different stat names (`UnsteadyThreshold`,
`ReceivedInstabilityMultiplier`, etc. for Gyro). No new technique
needed to build this category — it's a direct, proven extension of
work already shipped twice this session.

**Directly answers the "ace custom variant" use case (a Red Comet-style
Zaku II — same chassis, measurably better stats):** confirmed
`WalkSpeed` is a real, working `statName` (found in RogueTech's
`Gear_Cockpit_Hotseat.json` and an armor-mod file) — a simple
`Float_Multiply`/`Float_Add` Upgrade item targeting it delivers "same
chassis, faster" with nothing beyond what's already proven. **One real
exception, not free:** literal Engine-*rating* swapping (changing
`TopSpeed` via a different engine core) uses `ComponentType: "HeatSink"`
plus a custom `"Custom"` JSON block only RogueTech's own compiled DLL
parses — genuine new C# work, not a JSON-only extension. Not needed for
the ace-variant goal since `WalkSpeed` alone covers it; only worth
revisiting if something later specifically needs true engine-rating
mechanics.

**Status: list drafted and both open items resolved (2026-09-01),
ready to build.** Every category below uses the confirmed technique:
`ComponentType: "Upgrade"` + `StatisticEffect` (`Float_Add`/
`Float_Multiply`), mounted at `CenterTorso`, `gundamuc_weapon`-tagged.
"Default" entries are the baseline every MS already has implicitly via
its existing ChassisDef stats. "Upgrade" entries are real buildable
items.

- **Cockpit** — Default: standard, no bonus. Upgrade: Command
  Console-equivalent for the commander/lead unit, squad-wide bonus
  while equipped.
- **Engine / Reactor** — Default: baseline chassis speed. Upgrade:
  `WalkSpeed` `Float_Multiply` — confirmed working, "same chassis,
  measurably faster" (the Red Comet/ace-variant case). True
  engine-rating swap (`TopSpeed` via a different core) needs RogueTech's
  `HeatSink`-plus-custom-JSON-block technique — genuine new C# work,
  deferred, not needed for the ace-variant goal.
- **Minovsky Dispenser — RESOLVED.** Baseline (every unit) jams two
  specific things: (1) long-range missile guidance/lock-on, and (2)
  radar-based lock-on for large beam weapons (ship-mounted mega
  particle cannons). This is the concrete mechanical description of
  the Minovsky/EW system already locked earlier in this doc — tightens
  it from "long-range sensors" (vague) to these two named effects.
- **Optics** — Default: baseline detection/visual range. Upgrade:
  Night Vision and Thermal Vision, direct port of the RogueTech
  implementations already researched for Step 6's day/night system.
- **Targeting Computer** — Default: none. Upgrade: flat accuracy/
  to-hit bonus, direct reuse of BattleTech's own real concept.
- **Gyro** — Default: standard. Upgrade: better stability/knockdown
  resistance via RogueTech's confirmed stat names (`UnsteadyThreshold`,
  `ReceivedInstabilityMultiplier`).
- **Thruster (ground)** — Default: matches the chassis's existing
  jump/booster tier (item 17's gradient). Upgrade: field-refit path to
  raise an older unit's tier (Guncannon getting better boosters without
  becoming full jump-capable) — the concrete mechanism for 17b's
  "push an old model past a newer stock design" promise.
- **Space Thruster** — Default: chassis's baseline space eligibility
  per item 17's gate (Guntank's exclusion is about humanoid
  articulation, not thruster tier, and is unaffected by this). Upgrade:
  improved space-combat performance for units with some baseline
  capability.
- **Hover Thrusters — RESOLVED, renamed from "Dom Surface Glider" and
  confirmed NOT chassis-exclusive.** Research (2026-09-01) confirmed
  the MS-06GD Zaku II High Mobility Type (Ground Use) used the same
  underlying tech — high-mobility leg thrusters enabling ground
  hovering — and is explicitly described as a predecessor to the Dom's
  hovering, not a one-off. **Naming correction, worth remembering:**
  the obvious canon term "Minovsky Craft System" is real but wrong here
  — that's specifically large-unit tech (ships/Mobile Armors) that
  doesn't see MS-scale use until far later (Hathaway's Flash era).
  "Hover Thrusters" is the accurate, shared name. Default: none — only
  hover-capable chassis (Dom, and a future Zaku II High Mobility Type
  build) have this baked into their ChassisDef movement profile, same
  as today. Open question narrowed, not fully closed: still worth
  deciding whether hover ever becomes a rare refit-unlock for a
  non-hover chassis, or stays limited to chassis explicitly designed
  for it from the start — lower priority than the resolved items above.

17g. **Internal-equipment technical baseline — referenced by the
`Equipment/` research docs before it existed here, same gap pattern as
17f; written up properly now (2026-09-01).** Two real techniques, not
one — most internal equipment uses one, armor-weight items need the
other:
- **Default technique, everything except armor-weight items:**
  `ComponentType: "Upgrade"` + `StatisticEffect`
  (`Float_Add`/`Float_Multiply` on a real confirmed `statName`),
  mounted at `CenterTorso`, `gundamuc_weapon`-tagged, `IsFixed: false`
  for a real purchasable/swappable MechLab item. Proven three times now
  (shield, beam saber, Reactor Tuning) — zero new C# needed per item.
- **Exception: armor-weight-efficiency items (Luna Titanium and future
  siblings) genuinely can't use this technique** — confirmed no
  `Tonnage`-named `statName` exists anywhere. These need a real Harmony
  patch on `BattleTech.MechStatisticsRules.CalculateTonnage` (plus a
  companion patch on `MechLabMechInfoWidget.CalculateTonnage`'s
  independent UI-display reimplementation of the same formula) and a
  hardcoded `ComponentDefID → float` lookup in
  `GundamUCArmorWeight.ArmorWeightFactors`
  (`GundamUCArrivalPatch.cs`) — full technical writeup in
  `03-TECHNICAL-NOTES.md`. Not a data-only extension like everything
  else in this category; genuine new C# per armor-weight item added
  (though the patches themselves are already built and reusable —
  adding a second armor-grade item is just a new dictionary entry, not
  new patch code).

**Tier 4 — Structural/big-ticket**
18. Proto-carrier ship swap (Argo replacement) — data/stats first, before any custom modeling
19. Rank system (replaces mercenary board)
20. Basic political-intrigue framework, OYW-scoped only
21. Earth-drop / spaceport logistics (drop to Earth relatively freely, must control a spaceport to return to orbit) — campaign-state/travel-cost rules, no new assets needed

**Tier 5 — Deferred / stub only**
22. Newtype potential — hidden stat only, no mechanics yet
23. Multi-faction reputation framework — generic scaffold, OYW content only

**Last, always**
24. 3D model replacement (unit normal/damage/destroy states)
25. Custom ship model
26. Earth-biome and Luna map reflavoring (retexture/rename existing stock maps — Luna surface content already exists in BattleTech, this is genuinely cheap and could move much earlier if desired, same bucket as the skybox swap covered in Tier 3's space battle rules)

---

## Why this order

- Tiers 0-1 get you a *playable, replayable* skirmish loop fast — this is the thing that keeps a solo project alive, since you get to actually play your own mod early.
- Tier 2 is where it stops feeling like reskinned BattleTech and starts feeling like Gundam, for relatively low implementation cost (Minovsky/evasion/date-gating are mostly data + a couple of Harmony patches, not new engines).
- Tier 3 is the highest narrative payoff but also the most open-ended — easy to scope-creep here, so it's sequenced after you already have a working, playable game to hang it on.
- Tiers 4-5 are either high-effort/high-risk (ship swap, political systems) or explicitly out-of-scope for OYW (Newtypes) — deferring them protects the project from stalling on hard problems before the fun parts exist.
