using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GundamUCArrivalPatch
{
    public static class Main
    {
        public static void Init()
        {
            new Harmony("com.gundamuc.units.arrivalpatch").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // SimGameState.FlashpointDayPassed runs on every OnDayPassed tick, including
    // bulk fast-forward ticks (timeLapse > 0). Everything else that could have
    // scheduled/checked our Gundam-arrival event (UpdateMilestones, the
    // interruptQueue.QueueEventTest random-event roll) only runs when
    // timeLapse == 0, i.e. never during a held fast-forward. FlashpointDayPassed
    // is the same hook RogueTech's DisableHMLootbox.dll uses to intercept the
    // Heavy Metal DLC's starting-crate popup, which is why that popup (and the
    // flashpoint-expiration warning) fire correctly under fast-forward while
    // JSON-only milestones/events never do.
    [HarmonyPatch(typeof(SimGameState), "FlashpointDayPassed")]
    public static class SimGameState_FlashpointDayPassed_Patch
    {
        private const string GrantedStatName = "GundamUC_ArrivalGranted";
        private const string ArrivalEventId = "event_gundamuc_gundamArrival";
        private const int ArrivalDay = 1;
        private const string HeavyMetalSeenStatName = "HasSeenHeavyMetalLootPopup";

        // Preempt the Heavy Metal DLC's starting-mech-crate popup by marking
        // it "already seen" before the original method's own check runs —
        // the same technique RogueTech's DisableHMLootbox.dll uses against
        // this exact method. The Gundam arrival event now replaces this
        // narrative beat (same day-1 slot), so both firing would be redundant
        // and thematically wrong for this mod.
        public static void Prefix(SimGameState __instance)
        {
            if (__instance.SimGameMode == SimGameState.SimGameType.CAREER
                && !__instance.CompanyStats.ContainsStatistic(HeavyMetalSeenStatName))
            {
                __instance.CompanyStats.AddStatistic(HeavyMetalSeenStatName, 1);
            }
        }

        public static void Postfix(SimGameState __instance)
        {
            if (__instance.SimGameMode != SimGameState.SimGameType.CAREER)
            {
                return;
            }

            if (__instance.CompanyStats.ContainsStatistic(GrantedStatName))
            {
                return;
            }

            if (__instance.DaysPassed < ArrivalDay)
            {
                return;
            }

            __instance.CompanyStats.AddStatistic(GrantedStatName, 1);

            if (!__instance.DataManager.SimGameEventDefs.Exists(ArrivalEventId))
            {
                return;
            }

            SimGameEventDef eventDef = __instance.DataManager.SimGameEventDefs.Get(ArrivalEventId);
            SimGameEventTracker tracker = new SimGameEventTracker();
            // A bare `new SimGameEventTracker()` never sets its private `sim`
            // field, which the option-selection callback (OnOptionSelected ->
            // ApplyResultsToObject) dereferences unconditionally. Without this,
            // clicking the event's option throws a NullReferenceException
            // inside the UI click handler and the popup never advances.
            // InitForcedEvent is exactly what SimGameState.AddSpecialEvent
            // calls for a real ForceEvents-triggered event; mirror it here.
            tracker.InitForcedEvent(eventDef.Scope, 0, 0, 100, eventDef, null, __instance);
            __instance.OnEventTriggered(eventDef, eventDef.Scope, tracker);
        }
    }

    // Date-gated unit unlock, first real example: RGM-79 GM enters
    // production/becomes purchasable once the campaign clock crosses
    // Operation Odessa (6 November UC 0079 = 311 days from this mod's
    // CampaignStartDate of 0078-12-30 — see dev-reference/01-One-Year-War/
    // MS/RGM-79_GM.md for the date research). Reuses the same
    // FlashpointDayPassed hook as the Gundam arrival, for the same reason
    // (runs unconditionally every day tick, including under bulk
    // fast-forward). Deliberately a second hardcoded block, not a
    // generalized framework — matches this mod's established
    // don't-build-ahead-of-need pattern; generalize once a 3rd/4th
    // date-gated unit actually needs it.
    //
    // Unlike the Gundam (an instant scripted gift), GM is meant to become
    // purchasable/hireable, matching its canon mass-production identity.
    // mechdef_gm_RGM-79.json ships with "Purchasable": false and is
    // already listed by ID in itemCollection_systemStores_Mechs_common_
    // Medium.csv (a real, reachable stock shop pool — confirmed via
    // direct research that Purchasable:true alone does nothing; a MechDef
    // must also be explicitly listed by ID in a collection CSV a shop
    // chain actually reaches). This patch flips Purchasable to true on
    // the already-loaded MechDef once the date threshold passes, so the
    // existing unmodified shop-refresh cycle can start rolling it in on
    // its own — no shop-generation code needed.
    //
    // DescriptionDef.Purchasable is `{ get; private set; }` (confirmed via
    // decompile) — can't be set directly from outside the class, hence
    // Harmony's Traverse reflection helper below.
    [HarmonyPatch(typeof(SimGameState), "FlashpointDayPassed")]
    public static class SimGameState_FlashpointDayPassed_GMUnlock_Patch
    {
        private const string UnlockedStatName = "GundamUC_GMUnlocked";
        private const string GMMechDefId = "mechdef_gm_RGM-79";
        private const int GMUnlockDay = 311;

        public static void Postfix(SimGameState __instance)
        {
            if (__instance.SimGameMode != SimGameState.SimGameType.CAREER)
            {
                return;
            }

            if (__instance.CompanyStats.ContainsStatistic(UnlockedStatName))
            {
                return;
            }

            if (__instance.DaysPassed < GMUnlockDay)
            {
                return;
            }

            __instance.CompanyStats.AddStatistic(UnlockedStatName, 1);

            if (!__instance.DataManager.MechDefs.Exists(GMMechDefId))
            {
                return;
            }

            MechDef gmMechDef = __instance.DataManager.MechDefs.Get(GMMechDefId);
            Traverse.Create(gmMechDef.Description).Property("Purchasable").SetValue(true);
        }
    }

    // Dev/testing convenience: hide every stock BattleTech 'Mech from the
    // Skirmish mechbay so only this mod's own units show up, making it fast
    // to find whichever custom unit is actually being tested instead of
    // scrolling past ~100 stock 'Mechs.
    //
    // First attempt at this patched BattleTech.UI.LanceConfigurator, which
    // was wrong — confirmed by decompile that LanceConfiguratorPanel (which
    // owns a LanceConfigurator instance) references SimGameState/Contract/
    // Barracks, meaning that whole class is Career mode's real mission-prep
    // lance screen, not Skirmish. That patch had zero effect on Skirmish
    // (different code path entirely) and, worse, would have silently
    // filtered stock 'Mechs out of Career's own real mission-prep screen —
    // an unintended side effect on actual gameplay, not just a dev
    // convenience. Removed.
    //
    // The real Skirmish class is BattleTech.UI.SkirmishMechBayPanel
    // (confirmed via decompile — SetData/RequestResources/RefreshMechList).
    // RequestResources() bulk-loads every MechDef in the game (stock and
    // modded alike land in the same `stockMechs` list — "stock" here just
    // means "not built via the game's own in-UI Custom Mech editor",
    // unrelated to our mod) via an async LoadRequest callback, then
    // RefreshMechList() combines stockMechs + customMechs into allMechs and
    // hands it to the mechbay widget. A Prefix on RefreshMechList filtering
    // the private `stockMechs` field in place is the right point — it runs
    // after the async load completes (RefreshMechList is only ever called
    // from the load-complete callback) and before allMechs/the widget get
    // built from it. Every MechDef this mod ships carries a shared
    // "unit_gundamuc" MechTag specifically for this (see
    // 03-TECHNICAL-NOTES.md) — any new unit built later needs this tag too,
    // or it won't show up here.
    [HarmonyPatch(typeof(SkirmishMechBayPanel), "RefreshMechList")]
    public static class SkirmishMechBayPanel_RefreshMechList_Patch
    {
        private const string GundamUCTag = "unit_gundamuc";

        public static void Prefix(ref List<MechDef> ___stockMechs)
        {
            if (___stockMechs != null)
            {
                ___stockMechs = ___stockMechs
                    .Where(m => m?.MechTags != null && m.MechTags.Contains(GundamUCTag))
                    .ToList();
            }
        }
    }

    // Hand-equip system, first slice: restrict the MechLab Customize screen's
    // item picker to only this mod's own weapon/upgrade pool. Every
    // gear-swappable slot this mod ships (currently: Prototype Gundam's
    // RightArm beam rifle, IsFixed:false — see mechdef_protogundam_RX-78-1.json)
    // relies on this so the player can't drag in a stock BattleTech weapon
    // instead. Vanilla MechLab already reads a per-item IsFixed flag
    // (BaseComponentRef.IsFixed, confirmed via decompile) and blocks
    // drag/removal on it automatically — that part needed no patch at all.
    // This patch only handles the other half: what's legal to equip INTO an
    // open slot.
    //
    // Technique lifted from RogueTech's CustomComponents.dll (studied, not
    // copied — see 03-TECHNICAL-NOTES.md's rule on reference-mod use), which
    // does the same kind of item-pool restriction via an IMechLabFilter
    // interface + a postfix on this exact method. Confirmed via decompile
    // that BattleTech.UI.MechLabInventoryWidget.ApplyFiltering is a real
    // vanilla method (not just something RogueTech's own DLL defines):
    // iterates public field `localInventory`, and for each item, resolves a
    // MechComponentDef via `item.controller.weaponDef`,
    // `item.controller.componentDef`, or (a second, non-controller code path)
    // `item.weaponDef` directly. Only Weapon/Upgrade-type items are gated —
    // ammo/heat sinks/jump jets/mech parts are left alone entirely, since
    // this mod reuses stock ammo IDs directly (Ammo_AmmunitionBox_Generic_MG,
    // Ammo_AC5) and those were never going to carry a "gundamuc_weapon" tag
    // in the first place. Fails open on anything it can't confidently
    // resolve to a def, rather than risk hiding a legitimate inventory row.
    [HarmonyPatch(typeof(MechLabInventoryWidget), "ApplyFiltering")]
    public static class MechLabInventoryWidget_ApplyFiltering_Patch
    {
        private const string GundamUCWeaponTag = "gundamuc_weapon";

        public static void Postfix(MechLabInventoryWidget __instance)
        {
            if (__instance.localInventory == null)
            {
                return;
            }

            foreach (InventoryItemElement_NotListView item in __instance.localInventory)
            {
                if (item == null || item.gameObject == null || !item.gameObject.activeSelf)
                {
                    continue;
                }

                MechComponentDef def = null;
                if (item.controller != null)
                {
                    def = item.controller.weaponDef != null
                        ? (MechComponentDef)item.controller.weaponDef
                        : item.controller.componentDef;
                }
                else if (item.weaponDef != null)
                {
                    def = item.weaponDef;
                }

                if (def == null)
                {
                    continue;
                }

                if (def.ComponentType != ComponentType.Weapon && def.ComponentType != ComponentType.Upgrade)
                {
                    continue;
                }

                if (def.ComponentTags == null || !def.ComponentTags.Contains(GundamUCWeaponTag))
                {
                    item.gameObject.SetActive(false);
                }
            }
        }
    }

    // MechLab "Storage" panel — Step 1, take 3 (2026-08-31). Take 1 (cloned
    // centerTorsoWidget, hand-computed RectTransform offset) coincided with
    // a "the whole Career UI is gone" report — reverted immediately, later
    // confirmed via ModTek.log to be unrelated (zero exceptions, this
    // patch's hook hadn't even fired yet; the screenshot was BattleTech's
    // own normal new-Career establishing shot). Take 2 (zero offset —
    // identity copy of centerTorsoWidget's own position, per the user's
    // suggestion to remove positioning math as a variable) loaded safely
    // but visually overlapped centerTorsoWidget, "look[ed] so wrong" per
    // direct user feedback in-game. **Real, visual feedback from the live
    // layout > anything guessable from decompile alone** — the user
    // identified actual open screen space near the LeftArm widget (not
    // visible/inferable from a static field dump). This take clones
    // leftArmWidget instead of centerTorsoWidget and offsets upward by the
    // source widget's own height + a margin, landing in that open space
    // above it rather than overlapping anything. Still a best-effort guess
    // at the exact margin (no live-scene access) — expect another visual
    // round-trip if it's not quite right yet, same as every step so far.
    // Reparenting technique unchanged from take 2 (explicit
    // `SetParent(parent, false)`, `LayoutRebuilder.MarkLayoutForRebuild` as
    // cheap insurance) — that part loaded cleanly, no reason to touch it.
    // Still no item population, no drag/drop — that's Step 2, gated on
    // this actually looking right and behaving safely in-game first.
    // Shared between the InitWidgets and LoadMech patches below. Storing the
    // created widget directly, keyed by panel instance, rather than trying to
    // re-find it later by name/hierarchy — take 4's bug (below) was exactly
    // that kind of re-derivation going wrong. Same state-tracking-dictionary
    // pattern already studied from RogueTech's MechLabFixStateTracker earlier
    // this session, not invented fresh.
    public static class GundamUCHipHolder
    {
        public static readonly Dictionary<MechLabPanel, MechLabLocationWidget> Widgets =
            new Dictionary<MechLabPanel, MechLabLocationWidget>();
    }

    [HarmonyPatch(typeof(MechLabPanel), "InitWidgets")]
    public static class MechLabPanel_InitWidgets_StoragePanel_Patch
    {
        public static void Postfix(MechLabPanel __instance)
        {
            if (__instance.leftArmWidget == null)
            {
                return;
            }

            // Real root cause of the "Handheld shows a different mech's
            // gear" report (2026-09-11): InitWidgets() isn't called once per
            // panel — it's called from MechLabPanel.SetData(), which fires
            // again on every mech switch within the same MechLab session, so
            // this Postfix runs fresh each time too. Each run created a
            // brand-new clone via raw UnityEngine.Object.Instantiate (never
            // pooled — ruled out the RewardsPopup-style shared-pool
            // explanation directly via decompile) and overwrote the tracking
            // dictionary entry, but never destroyed the PREVIOUS mech's
            // widget GameObject first. Both old and new clones get
            // positioned at identical screen coordinates (computed the same
            // way every time, relative to leftArmWidget), so the stale one
            // could end up rendering on top depending on sibling order —
            // showing whichever mech was viewed before instead of the one
            // actually loaded now. The earlier ClearInventory() fix
            // (LoadMech patch) was real but insufficient: it cleared data,
            // not the duplicate GameObject actually causing the visual
            // leak.
            //
            // 2026-09-11, third correction: cleaning up the old widget
            // BEFORE creating the new one (both attempts so far) caused two
            // different failures in turn — first an occasional total
            // absence of the panel (a Destroy() failure could abort the rest
            // of the method before this mech's own widget ever got created),
            // second the panel existing and correctly holding this mech's
            // items (confirmed: tonnage included them, they were correctly
            // gone from their real source widgets) but not being visually
            // rendered — most likely a sibling-order issue, an old
            // not-yet-destroyed clone sharing the exact same computed screen
            // position as the new one and ending up drawn on top. Reordered
            // so cleanup can never interfere with creating or displaying
            // today's widget: build, position, and register the new one
            // FIRST, explicitly force it to the front of the render order,
            // and only then destroy whatever this panel had before.
            GundamUCHipHolder.Widgets.TryGetValue(__instance, out MechLabLocationWidget oldStorageWidget);

            // Sizing attempts against an arm-sized clone (sizeDelta shrink,
            // then a RectMask2D, then LayoutElement.ignoreLayout) all had
            // zero visible effect across three separate takes — user's own
            // idea, tried here instead: Prototype Gundam's LeftLeg has 4
            // InventorySlots in its ChassisDef vs. LeftArm's 8, so its widget
            // PREFAB may simply be built smaller to begin with, sidestepping
            // the whole "shrink a bigger one" problem rather than fighting
            // whatever kept overriding a manual resize. Clone source and
            // position anchor are deliberately different now: clone
            // leftLegWidget (for its naturally smaller proportions), but
            // still position relative to leftArmWidget (the open screen
            // space this panel is meant to occupy, already confirmed correct
            // across the earlier takes).
            GameObject sizeSourceGO = __instance.leftLegWidget.gameObject;
            GameObject positionAnchorGO = __instance.leftArmWidget.gameObject;
            Transform sharedParent = positionAnchorGO.transform.parent;

            GameObject storageGO = UnityEngine.Object.Instantiate(sizeSourceGO);
            storageGO.name = "GundamUC_StorageWidget";
            storageGO.transform.SetParent(sharedParent, false);

            // Forces this to the front of the render order regardless of
            // whatever else is still parented under sharedParent at this
            // exact moment (including a not-yet-destroyed previous clone) —
            // SetParent alone doesn't guarantee last-sibling placement.
            storageGO.transform.SetAsLastSibling();

            MechLabLocationWidget storageWidget = storageGO.GetComponent<MechLabLocationWidget>();
            if (storageWidget == null)
            {
                UnityEngine.Object.Destroy(storageGO);
                return;
            }

            RectTransform positionAnchorRect = positionAnchorGO.transform as RectTransform;
            RectTransform storageRect = storageGO.transform as RectTransform;
            if (positionAnchorRect != null && storageRect != null)
            {
                // Real root cause, finally confirmed via logged data
                // (2026-09-11): this used to be `rect.height + 200f`.
                // leftArmWidget.rect.height reads as a small/near-zero,
                // not-yet-laid-out value on the very first InitWidgets call
                // ever made in a MechLab session (landing the panel at
                // anchoredPos.y=112, confirmed visible), but reads its true,
                // much larger settled height on every call after that
                // (anchoredPos.y=482 every time, confirmed present, active,
                // correctly populated, and NOT visible — pushed off-screen).
                // Nothing else differed between the working and broken
                // cases in the logs: same active state, same sibling index,
                // same item counts. Dropping the unstable rect.height term
                // entirely fixes the actual bug instead of the sibling-order/
                // exception theories tried (and disproven) before this.
                Vector2 aboveOffset = new Vector2(0f, 200f);
                storageRect.anchoredPosition = positionAnchorRect.anchoredPosition + aboveOffset;

                // Kept from earlier takes as harmless insurance even though
                // neither turned out to be the actual blocker for the old
                // arm-sized clone: RectMask2D clips any child content to the
                // panel's own rect regardless of the children's own anchor/
                // size settings; LayoutElement.ignoreLayout tells any parent
                // LayoutGroup (if `sharedParent` has one managing the 8 real
                // widgets' uniform sizing) to skip this child entirely so it
                // isn't silently reset after being placed.
                storageGO.AddComponent<RectMask2D>();
                LayoutElement layoutElement = storageGO.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
            }

            storageWidget.Init(__instance);
            GundamUCHipHolder.Widgets[__instance] = storageWidget;

            // Cleanup runs LAST, deliberately — see the long comment at the
            // top of this method. This mech's own widget is already fully
            // built, positioned, front-of-render-order, and registered by
            // this point, so a failure here can only leave a stray
            // duplicate GameObject behind, never block this mech's panel.
            if (oldStorageWidget != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(oldStorageWidget.gameObject);
                }
                catch (Exception)
                {
                    // Best-effort cleanup only.
                }
            }

            // Phase 1 of the Handheld redesign (2026-09-03): make this widget
            // a REAL drop target, not just a display. Confirmed via decompile
            // that `ValidateAdd` (the real gate `OnMechLabDrop` calls) throws
            // on a null `loadout` — this clone only ever got Init(), never
            // SetData(). Fix: both `loadout` and the hardpoint-count fields
            // below are PUBLIC (confirmed from the class field dump), so this
            // is plain field assignment, no Traverse/reflection needed for
            // any of it except `maxSlots` (confirmed private).
            //
            // `loadout.Location: CenterTorso` is deliberate, not arbitrary —
            // matches the same coup-de-grace backing every Handheld item's
            // `AllowedLocations` now targets, and is what
            // `RefreshMechComponentData` writes back onto the real
            // MechComponentRef on a successful drop (confirmed via decompile:
            // `mechComponent.ComponentRef.SetData(loadout.Location, ...)`).
            //
            // Changed from CenterTorso to RightArm (2026-09-03), per user
            // direction: Handheld is conceptually an extension of the arm,
            // not a replacement for it — the REAL MountedLocation a dropped
            // item ends up with needs to stay an actual arm so a future
            // custom 3D model attaches in the right place (can't swap which
            // location a model renders from after the fact). This still
            // avoids whatever's actually broken in the real arm widgets' own
            // drop-handling (unconfirmed root cause, see 03-TECHNICAL-NOTES.md)
            // — Handheld's fake widget bypasses that code path entirely while
            // still writing anatomically-correct data. Trade-off accepted
            // deliberately: this reintroduces normal "lose that arm, lose the
            // weapon" behavior until Phase 2 (weapon stays usable with either
            // arm intact) gets built — not a regression, just the interim
            // state before that separate mechanic exists.
            storageWidget.loadout = new LocationLoadoutDef(
                ChassisLocations.RightArm,
                CurrentInternalStructure: 50f);

            // ValidateAdd also gates Weapon-type drops on category-specific
            // hardpoint counts (currentBallisticCount vs
            // totalBallisticHardpoints, etc. — confirmed via decompile, not
            // just the slot-capacity check). Tightened 2026-09-11 per direct
            // user report/design correction: this used to be generous enough
            // (2 Ballistic + 2 Energy + 1 Missile + 1 Support = up to 6
            // ranged items) to let the player load far more ranged weapons
            // into Handheld than "one hand" should ever hold. Beam Rifle no
            // longer routes through this panel at all (removed
            // gundamuc_storage — it's the primary arm weapon, displays on
            // its own real arm widget like any normal weapon now that the
            // bugs that originally justified rerouting it are fixed), so
            // Energy/Missile/Support have nothing left to hold — zeroed.
            // Ballistic capped to exactly 1 (today's only ranged backup,
            // Bazooka) — a real, deliberately narrow cap, not headroom for
            // hypothetical future items; widen only when a specific new
            // ranged Handheld item actually needs it.
            storageWidget.totalBallisticHardpoints = 1;
            storageWidget.totalEnergyHardpoints = 0;
            storageWidget.totalMissileHardpoints = 0;
            storageWidget.totalSmallHardpoints = 0;

            // Sized to fit exactly one melee item (Beam Saber, 1 slot) plus
            // one ranged item (Bazooka, 3 slots) — not a round number chosen
            // for headroom.
            Traverse traverse = Traverse.Create(storageWidget);
            traverse.Field("maxSlots").SetValue(4);

            // Labelled "Handheld" per user direction (2026-09-03, superseding
            // the earlier "Hip Holder" rename) — this panel (internal names
            // still say "Storage" throughout this file, unchanged to avoid a
            // pure-rename diff) is the visual home for anything a hand
            // carries: melee weapons and now real ranged weapons too
            // (Phase 1). CenterTorso stays reserved for cockpit/reactor/
            // internal equipment only — items shown here still mount at
            // CenterTorso under the hood for the coup-de-grace
            // indestructibility reasoning (02-FEATURE-LIST.md 17f).
            traverse.Field("locationName").GetValue<LocalizableText>()?.SetText("Handheld");

            // The individual named-field hides below (armorBar/rearArmorBar/
            // structureText/hardpoints) visibly did NOT take effect in-game —
            // the "120/120" armor slider and its +/- buttons still rendered.
            // Root cause not confirmed (possibly a field-name/type mismatch on
            // the cloned instance, possibly something re-enabling them after
            // Init runs). Rather than keep guessing at exactly which field is
            // wrong, added a more robust sweep below: walk every descendant of
            // the panel EXCEPT the inventoryParent subtree (so real/future
            // item content is never touched, even if an item's own name
            // happens to contain one of these keywords) and hide anything
            // whose GameObject name suggests it's armor/structure/hardpoint
            // UI. Keep the explicit per-field hides too — harmless if
            // redundant with the sweep, still correct wherever they do work.
            SetActiveIfNotNull(traverse.Field("armorBar").GetValue<LanceStat>()?.gameObject, false);
            SetActiveIfNotNull(traverse.Field("rearArmorBar").GetValue<LanceStat>()?.gameObject, false);
            SetActiveIfNotNull(traverse.Field("structureText").GetValue<LocalizableText>()?.gameObject, false);
            SetActiveIfNotNull(traverse.Field("damagedOverlay").GetValue<GameObject>(), false);
            SetActiveIfNotNull(traverse.Field("destroyedOverlay").GetValue<GameObject>(), false);
            SetActiveIfNotNull(traverse.Field("HighlightFrame").GetValue<GameObject>(), false);

            MechLabHardpointElement[] hardpoints = traverse.Field("hardpoints").GetValue<MechLabHardpointElement[]>();
            if (hardpoints != null)
            {
                foreach (MechLabHardpointElement hardpoint in hardpoints)
                {
                    SetActiveIfNotNull(hardpoint == null ? null : hardpoint.gameObject, false);
                }
            }

            Transform inventoryParentTransform = traverse.Field("inventoryParent").GetValue<Transform>();
            string[] hideKeywords = { "armor", "structure", "hardpoint", "quirk", "damaged", "destroyed" };
            HideDescendantsByNameContains(storageGO.transform, inventoryParentTransform, hideKeywords);

            // Close the gap left by the now-hidden header/armor/hardpoint rows
            // by pulling the item list up toward the "Storage" title instead
            // of leaving that space empty.
            if (inventoryParentTransform != null)
            {
                RectTransform inventoryRect = inventoryParentTransform as RectTransform;
                if (inventoryRect != null)
                {
                    Vector2 pos = inventoryRect.anchoredPosition;
                    pos.y = -40f;
                    inventoryRect.anchoredPosition = pos;
                }
            }

            if (sharedParent != null)
            {
                RectTransform parentRect = sharedParent as RectTransform;
                if (parentRect != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                }
            }
        }

        private static void SetActiveIfNotNull(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static void HideDescendantsByNameContains(Transform root, Transform exclude, string[] keywords)
        {
            foreach (Transform child in root)
            {
                if (child == exclude)
                {
                    continue;
                }

                string nameLower = child.name.ToLowerInvariant();
                bool matched = false;
                foreach (string keyword in keywords)
                {
                    if (nameLower.Contains(keyword))
                    {
                        child.gameObject.SetActive(false);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    HideDescendantsByNameContains(child, exclude, keywords);
                }
            }
        }
    }

    // MechLab "Storage" panel — Step 2 (real items, 2026-08-31). Step 1
    // only proved the panel itself renders correctly; this is what actually
    // moves gundamuc_storage-tagged items into it. Real items still mount
    // at CenterTorso in the MechDef (per 02-FEATURE-LIST.md 17f — its
    // destruction is BattleTech's coup-de-grace location, so anything
    // mounted there is functionally unlosable while the unit can still
    // fight) — this patch only changes which WIDGET they're displayed and
    // interacted through, not where they mechanically live.
    //
    // Confirmed via decompile exactly where to hook: MechLabPanel.LoadMech
    // is what actually calls SetData on all 8 real widgets with the mech's
    // real per-location data (InitWidgets, patched above for Step 1, never
    // does this — Init() and SetData() are genuinely separate, as already
    // established). A Postfix here runs after every real widget has been
    // freshly populated from activeMechDef.Inventory.
    //
    // Critical detail found before writing this (would have shipped a
    // subtly-broken version otherwise): MechLabItemSlotElement tracks its
    // own logical owner via a private `dropParent` field (type
    // IMechLabDropTarget, which MechLabLocationWidget implements) —
    // completely independent of the Unity Transform hierarchy. Simply
    // reparenting the item's GameObject would move it visually into
    // Storage while every drag/remove/tooltip interaction kept routing back
    // to CenterTorso's widget underneath. The item's own public
    // `SetData(componentRef, mountedLocation, dataManager, dropParent)`
    // rebinds that logical owner correctly — call that AND reparent the
    // Transform, not just one or the other.
    //
    // Also fixed 2026-09-02, same investigation as the usedSlots fix below:
    // originally looked up the Hip Holder widget via
    // `centerTorsoWidget.transform.parent.Find("GundamUC_StorageWidget")` —
    // but the widget was actually parented under `leftArmWidget`'s parent in
    // the InitWidgets patch above, not CenterTorso's. If those two widgets
    // don't share a direct parent in the real UI hierarchy (plausible if the
    // layout groups widgets into columns — never actually confirmed either
    // way), that lookup silently finds nothing and this entire patch becomes
    // a no-op: matches the exact reported symptom (Hip Holder renders and is
    // positioned correctly, but nothing ever moves into it). Rather than
    // guess at the right shared-parent assumption a second time, removed the
    // re-derivation entirely — `GundamUCHipHolder.Widgets` (declared above,
    // set once in InitWidgets) is looked up directly by panel instance
    // instead.
    [HarmonyPatch(typeof(MechLabPanel), "LoadMech")]
    public static class MechLabPanel_LoadMech_StorageItems_Patch
    {
        private const string GundamUCStorageTag = "gundamuc_storage";

        public static void Postfix(MechLabPanel __instance)
        {
            if (__instance.centerTorsoWidget == null)
            {
                return;
            }

            if (!GundamUCHipHolder.Widgets.TryGetValue(__instance, out MechLabLocationWidget storageWidget)
                || storageWidget == null)
            {
                return;
            }

            // Bug found 2026-09-11 (direct user report: every Guncannon
            // showed the one Bazooka that actually belonged to their
            // Gundam): the fabricated Handheld widget lives for the life of
            // the MechLabPanel instance, not per-mech — switching to a
            // different mech in the same MechLab session re-runs LoadMech
            // on the SAME storageWidget, but the sweep below only ever adds
            // to its localInventory, never clears stale entries left over
            // from whichever mech was viewed before. ClearInventory() is a
            // real, public vanilla method (confirmed via decompile) that
            // properly unparents+pools each item's GameObject and empties
            // localInventory — exactly what every REAL location widget gets
            // automatically on a mech switch, that this fabricated one never
            // did. Must run unconditionally, before the hands gate below,
            // so a hands-less mech doesn't silently carry stale entries
            // forward to the next hands-capable mech either.
            storageWidget.ClearInventory();

            // Eligibility gate (2026-09-03): a Guntank — or any future
            // Mobile Armor — has no hands and shouldn't be able to see, let
            // alone use, the Handheld panel at all. Reuses
            // `unit_humanoidHands`, a ChassisTag this mod already added to
            // 9 of its 10 chassis specifically for this and 17d's
            // sub-flight system (03-TECHNICAL-NOTES.md) — built ahead of
            // need, first real use. Simple visibility gate, not deep
            // validation: a hand-less mech never sees the panel, so
            // there's nothing to drop into by construction.
            const string HumanoidHandsTag = "unit_humanoidHands";
            bool hasHands = __instance.activeMechDef != null
                && __instance.activeMechDef.Chassis != null
                && __instance.activeMechDef.Chassis.ChassisTags != null
                && __instance.activeMechDef.Chassis.ChassisTags.Contains(HumanoidHandsTag);
            storageWidget.gameObject.SetActive(hasHands);
            if (!hasHands)
            {
                return;
            }

            Transform storageInventoryParent = Traverse.Create(storageWidget).Field("inventoryParent").GetValue<Transform>();
            if (storageInventoryParent == null)
            {
                return;
            }

            Traverse storageTraverse = Traverse.Create(storageWidget);
            List<MechLabItemSlotElement> storageInventory =
                storageTraverse.Field("localInventory").GetValue<List<MechLabItemSlotElement>>();

            // Sweeps LeftArm and RightArm now, not just CenterTorso (2026-09-03) —
            // Handheld items are real-arm-backed again (see the loadout.Location
            // change above), so that's genuinely where they show up in each
            // real widget's own display and need relocating from. CenterTorso
            // kept in the sweep too, harmless if nothing gundamuc_storage-tagged
            // ever lands there again — cheap robustness, not dead code chasing
            // a scenario that can't happen.
            SweepWidgetForHandheldItems(__instance.leftArmWidget, storageWidget, storageInventory, storageInventoryParent, __instance.dataManager);
            SweepWidgetForHandheldItems(__instance.rightArmWidget, storageWidget, storageInventory, storageInventoryParent, __instance.dataManager);
            SweepWidgetForHandheldItems(__instance.centerTorsoWidget, storageWidget, storageInventory, storageInventoryParent, __instance.dataManager);
        }

        private static void SweepWidgetForHandheldItems(
            MechLabLocationWidget sourceWidget,
            MechLabLocationWidget storageWidget,
            List<MechLabItemSlotElement> storageInventory,
            Transform storageInventoryParent,
            DataManager dataManager)
        {
            if (sourceWidget == null)
            {
                return;
            }

            Traverse sourceTraverse = Traverse.Create(sourceWidget);
            List<MechLabItemSlotElement> sourceInventory =
                sourceTraverse.Field("localInventory").GetValue<List<MechLabItemSlotElement>>();
            if (sourceInventory == null)
            {
                return;
            }

            // First pass: only IDENTIFY matching items — never mutate the
            // list while enumerating it (throws InvalidOperationException).
            List<MechLabItemSlotElement> itemsToMove = new List<MechLabItemSlotElement>();
            foreach (MechLabItemSlotElement item in sourceInventory)
            {
                if (item == null || item.ComponentRef == null || item.ComponentRef.Def == null)
                {
                    continue;
                }

                if (item.ComponentRef.Def.ComponentTags == null
                    || !item.ComponentRef.Def.ComponentTags.Contains(GundamUCStorageTag))
                {
                    continue;
                }

                itemsToMove.Add(item);
            }

            if (itemsToMove.Count == 0)
            {
                return;
            }

            // The bug this fixes: earlier version only reparented the
            // GameObject and rebound drag/drop ownership, but never removed
            // the item from the source widget's own `localInventory` list —
            // so that widget still considered the item present (confirmed by
            // direct user report). Now genuinely removed from the source
            // widget's bookkeeping, `usedSlots` corrected to match, and added
            // to Handheld's own `localInventory` too (needed for that panel
            // to correctly support dragging the item back out later, not
            // just displaying it).
            int sourceUsedSlots = sourceTraverse.Field("usedSlots").GetValue<int>();

            foreach (MechLabItemSlotElement item in itemsToMove)
            {
                ChassisLocations realLocation = item.ComponentRef.MountedLocation;
                sourceInventory.Remove(item);
                sourceUsedSlots -= item.ComponentRef.Def.InventorySize;

                item.SetData(item.ComponentRef, realLocation, dataManager, storageWidget);
                item.gameObject.transform.SetParent(storageInventoryParent, false);
                storageInventory?.Add(item);
            }

            sourceTraverse.Field("usedSlots").SetValue(sourceUsedSlots < 0 ? 0 : sourceUsedSlots);

            // Bug found 2026-09-05: usedSlots isn't the only bookkeeping
            // ValidateAdd checks — currentEnergyCount/currentBallisticCount/
            // currentMissileCount/currentSmallCount (private, confirmed via
            // decompile) track per-category hardpoint occupancy separately
            // and were never corrected here. Removing a weapon from
            // localInventory without recomputing these left the real arm
            // widget believing its hardpoint was still occupied, silently
            // rejecting any attempt to drop that same weapon (or another of
            // its category) back onto the arm. RefreshHardpointData() (public)
            // recomputes all four counts straight from localInventory — the
            // same method the widget calls after any normal add/remove.
            sourceWidget.RefreshHardpointData();
        }
    }

    // Quick cosmetic fix, not the full Section B sub-day-granularity rework:
    // SGTimePlayPause.SetDay(int daysPassed) computes "Week {0}  Day {1}" from
    // pure elapsed-days-since-Career-start math (daysPassed/7+1, daysPassed%7+1)
    // — it never reads CampaignStartDate/CurrentDate at all, which is why
    // changing the campaign start year had no effect on this label. This
    // Postfix overwrites the label with the real in-fiction calendar date
    // instead. Expected to be replaced, not extended, once the full sub-day
    // time system (hour-of-day, night missions) gets built — that rework will
    // need to touch this same class anyway.
    [HarmonyPatch(typeof(SGTimePlayPause), "SetDay")]
    public static class SGTimePlayPause_SetDay_Patch
    {
        public static void Postfix(LocalizableText ___timePassedText, SimGameState ___simState)
        {
            if (___timePassedText == null || ___simState == null)
            {
                return;
            }

            DateTime currentDate = ___simState.CurrentDate;
            // Force invariant (English) culture — without it, month names follow
            // the Mono runtime's ambient OS locale, which isn't necessarily
            // English even though the rest of the game's UI is.
            ___timePassedText.SetText(currentDate.ToString(
                "d MMMM, 'UC' yyyy", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    // Armor-weight-efficiency items (Luna Titanium Purity Grade and future
    // siblings) — researched before building (2026-09-01), not assumed.
    // Confirmed neither Tonnage nor real armor points are targetable via the
    // plain Upgrade+StatisticEffect mechanism this mod's other items all use
    // (grepped stock data and all of RogueTech — zero hits for any such
    // statName). Real armor-weight discounts (RogueTech's Ferro-Fibrous
    // items) work through a completely different mechanism: a Harmony patch
    // on the actual vanilla tonnage-math method, not a data-driven stat
    // effect. Confirmed via decompile that the vanilla math has exactly two
    // independent implementations that both need correcting, not one:
    // `BattleTech.MechStatisticsRules.CalculateTonnage` (the real one —
    // `MechValidationRules.ValidateMechTonnage` calls this directly, so
    // correcting it here is also what fixes overweight/underweight
    // validation) and a second, fully independent reimplementation of the
    // identical formula inside `MechLabMechInfoWidget.CalculateTonnage`
    // (private, UI-display-only — doesn't call the rules method at all, so
    // leaving it unpatched would show the player a stale/wrong tonnage
    // total even though the real validation was already correct).
    //
    // Deliberately NOT using RogueTech's own "Custom" JSON-block pattern —
    // that requires a whole generic JSON-extension-parsing framework
    // (CustomComponents.dll) this mod has no need for. A plain hardcoded
    // lookup keyed by ComponentDefID is simpler and matches this project's
    // own established practice of a small hardcoded block over a
    // generalized framework until a 3rd/4th item actually needs one (see
    // the GM date-gate patch above for the same reasoning).
    //
    // Both patches are Postfixes, not skip-original replacements — matches
    // every other patch in this file, and is provably safe here: the
    // original method already adds the FULL (undiscounted) armor tonnage
    // into its result using the exact same `ARMOR_PER_TENTH_TON` formula
    // this patch also uses, so subtracting that same amount back out and
    // re-adding the discounted version can never drift from vanilla's own
    // math even if the underlying constant ever changes.
    public static class GundamUCArmorWeight
    {
        // Component IDs mapped to a tonnage multiplier for the armor
        // portion of the mech's weight only (1.0 = no discount). Add new
        // armor-grade items here as they're built.
        public static readonly Dictionary<string, float> ArmorWeightFactors = new Dictionary<string, float>
        {
            { "Gear_Armor_LunaTitanium_Improved", 0.90f },
        };

        public static float GetArmorWeightFactor(MechDef mechDef)
        {
            float best = 1f;
            if (mechDef == null || mechDef.Inventory == null)
            {
                return best;
            }

            foreach (MechComponentRef componentRef in mechDef.Inventory)
            {
                if (componentRef == null || componentRef.ComponentDefID == null)
                {
                    continue;
                }

                if (ArmorWeightFactors.TryGetValue(componentRef.ComponentDefID, out float factor) && factor < best)
                {
                    best = factor;
                }
            }

            return best;
        }

        public static float SumAssignedArmor(MechDef mechDef)
        {
            float total = 0f;
            total += mechDef.Head.AssignedArmor;
            total += mechDef.CenterTorso.AssignedArmor;
            total += mechDef.CenterTorso.AssignedRearArmor;
            total += mechDef.LeftTorso.AssignedArmor;
            total += mechDef.LeftTorso.AssignedRearArmor;
            total += mechDef.RightTorso.AssignedArmor;
            total += mechDef.RightTorso.AssignedRearArmor;
            total += mechDef.LeftArm.AssignedArmor;
            total += mechDef.RightArm.AssignedArmor;
            total += mechDef.LeftLeg.AssignedArmor;
            total += mechDef.RightLeg.AssignedArmor;
            return total;
        }
    }

    [HarmonyPatch(typeof(MechStatisticsRules), "CalculateTonnage")]
    public static class MechStatisticsRules_CalculateTonnage_ArmorWeight_Patch
    {
        public static void Postfix(MechDef mechDef, ref float currentValue, ref float maxValue)
        {
            float armorFactor = GundamUCArmorWeight.GetArmorWeightFactor(mechDef);
            if (armorFactor >= 1f)
            {
                return;
            }

            float armorPoints = GundamUCArmorWeight.SumAssignedArmor(mechDef);
            float armorTonnageAtStandardWeight = armorPoints
                / (UnityGameInstance.BattleTechGame.MechStatisticsConstants.ARMOR_PER_TENTH_TON * 10f);
            currentValue -= armorTonnageAtStandardWeight;
            currentValue += armorTonnageAtStandardWeight * armorFactor;
        }
    }

    // Companion patch, UI-display-only — see the block comment above for why
    // this second, fully independent method also needs correcting.
    [HarmonyPatch(typeof(MechLabMechInfoWidget), "CalculateTonnage")]
    public static class MechLabMechInfoWidget_CalculateTonnage_ArmorWeight_Patch
    {
        public static void Postfix(MechLabMechInfoWidget __instance)
        {
            Traverse traverse = Traverse.Create(__instance);
            MechLabPanel mechLab = traverse.Field("mechLab").GetValue<MechLabPanel>();
            if (mechLab == null || mechLab.activeMechDef == null)
            {
                return;
            }

            float armorFactor = GundamUCArmorWeight.GetArmorWeightFactor(mechLab.activeMechDef);
            if (armorFactor >= 1f)
            {
                return;
            }

            float armorPoints = GundamUCArmorWeight.SumAssignedArmor(mechLab.activeMechDef);
            float armorTonnageAtStandardWeight = armorPoints
                / (UnityGameInstance.BattleTechGame.MechStatisticsConstants.ARMOR_PER_TENTH_TON * 10f);
            float correctedTonnage = __instance.currentTonnage - armorTonnageAtStandardWeight
                + (armorTonnageAtStandardWeight * armorFactor);
            __instance.currentTonnage = correctedTonnage;

            float maxTonnage = mechLab.activeMechDef.Chassis.Tonnage;
            float remaining = maxTonnage - correctedTonnage;
            UIColor totalColor = (remaining < 0f) ? UIColor.Red : UIColor.WhiteHalf;
            UIColor remainingColor = (remaining < 0f) ? UIColor.Red : ((remaining <= 5f) ? UIColor.Gold : UIColor.White);

            LocalizableText totalTonnageText = traverse.Field("totalTonnage").GetValue<LocalizableText>();
            UIColorRefTracker totalTonnageColor = traverse.Field("totalTonnageColor").GetValue<UIColorRefTracker>();
            LocalizableText remainingTonnageText = traverse.Field("remainingTonnage").GetValue<LocalizableText>();
            UIColorRefTracker remainingTonnageColor = traverse.Field("remainingTonnageColor").GetValue<UIColorRefTracker>();

            totalTonnageText?.SetText("{0:0.##} / {1}", correctedTonnage, maxTonnage);
            totalTonnageColor?.SetUIColor(totalColor);

            float absRemaining = Mathf.Abs(remaining);
            if (remaining < 0f)
            {
                remainingTonnageText?.SetText("{0:0.##} ton{1} overweight", absRemaining, (absRemaining == 1f) ? "" : "s");
            }
            else
            {
                remainingTonnageText?.SetText("{0:0.##} ton{1} remaining", absRemaining, (absRemaining == 1f) ? "" : "s");
            }
            remainingTonnageColor?.SetUIColor(remainingColor);
        }
    }

    // Vanilla bug: RewardsPopup.ClearItems() pools each reward-preview widget's
    // GameObject directly (dm.PoolGameObject) instead of via
    // ListElementController_BASE_NotListView.Pool(), which is the only path that
    // resets SetClickable(true)/SetDraggable(true) before pooling. Because
    // CreateItemEntry() locks reward previews (SetClickable(false)) and the
    // MechBay inventory sidebar draws widgets from that same shared pool, an item
    // received via a flashpoint/story reward can come back permanently
    // grey/disabled and undraggable in MechLab (still sellable, since SimGame
    // ownership is unaffected). Fix at the source: unlock every reward widget
    // before ClearItems' own pooling call runs.
    [HarmonyPatch(typeof(RewardsPopup), "ClearItems")]
    public static class RewardsPopup_ClearItems_UnlockPatch
    {
        public static void Prefix(RewardsPopup __instance)
        {
            List<ListElementController_BASE_NotListView> controllers =
                Traverse.Create(__instance).Field("AllSalvageControllers")
                    .GetValue<List<ListElementController_BASE_NotListView>>();
            if (controllers == null)
            {
                return;
            }

            foreach (ListElementController_BASE_NotListView controller in controllers)
            {
                if (controller?.ItemWidget == null)
                {
                    continue;
                }
                controller.ItemWidget.SetClickable(true);
                controller.ItemWidget.SetDraggable(true);
            }
        }
    }

    // Root fix for the same bug (2026-09-05): the ClearItems patch above turned
    // out insufficient on its own — RewardsPopup.OnClose() (fired when the
    // player clicks "Complete") never calls ClearItems() at all, it only pools
    // the popup panel itself. So a reward item's own preview widget, locked by
    // CreateItemEntry's SetItemButtonClickable(false), can sit locked and
    // orphaned indefinitely, never cycling back through ClearItems to get
    // unlocked. Confirmed via decompile: InventoryItemElement_NotListView.SetData
    // (both overloads — the MechComponentRef one MechLab's inventory sidebar
    // calls, and the ListElementController one) unconditionally calls
    // SetDraggable(true) when binding a widget to real data, but neither one
    // calls the matching SetClickable(true) — the only method that clears
    // HBSButtonBase.IsLocked. That's the actual gap: no consumer of this shared
    // widget pool can ever inherit a clean "clickable" state except by luck.
    // Patching both SetData overloads directly closes the whole bug class,
    // regardless of which prior use poisoned the pooled instance.
    [HarmonyPatch(typeof(InventoryItemElement_NotListView))]
    public static class InventoryItemElement_NotListView_SetData_UnlockPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(InventoryItemElement_NotListView)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "SetData");
        }

        public static void Postfix(InventoryItemElement_NotListView __instance)
        {
            __instance.SetClickable(true);
        }
    }

    // Combat mechanic (2026-09-10): Prototype Gundam carries multiple
    // hand-held weapons (Beam Rifle, Hyper Bazooka, and — via the mech's own
    // built-in melee weapon, boosted by the Beam Saber Upgrade item — a
    // melee option) but only one can be firing from the hand at a time, and
    // none of them work with both arms destroyed. User's design: the
    // arm-mounted weapon is active by default at battle start; committing to
    // a melee attack automatically claims the hand (no manual step) as long
    // as an arm survives; switching between the two ranged options needs an
    // explicit player click. Confirmed via decompile: RogueTech's
    // CustomAmmoCategories mod already does near-identical mutual-exclusion
    // via a Postfix on Weapon.HasAmmo — vanilla's own
    // `CanFire = !IsDisabled && HasAmmo` already reads that everywhere it
    // matters, combat logic and HUD alike, so patching it here is the same
    // proven, minimal hook rather than RogueTech's much heavier
    // ActivatableComponent framework (confirmed absent from vanilla
    // entirely — not a lightweight precedent, deliberately not used).
    public static class GundamUCHandWeapon
    {
        public const string RequiresHandTag = "gundamuc_requireshand";
        public const string MeleeSlotValue = "__GundamUC_Melee__";
        private const string ActiveHandSlotStat = "GundamUC_ActiveHandSlot";

        public static bool RequiresHand(MechComponentDef def)
        {
            return def != null && def.ComponentTags != null && def.ComponentTags.Contains(RequiresHandTag);
        }

        public static bool HasFunctionalArm(Mech mech)
        {
            return !mech.IsLocationDestroyed(ChassisLocations.LeftArm)
                || !mech.IsLocationDestroyed(ChassisLocations.RightArm);
        }

        // Gates the melee auto-switch so it's a no-op for every unit that
        // isn't carrying this mod's hand-exclusive gear (checked against
        // allComponents, not Weapons, since the Beam Saber is a
        // ComponentType.Upgrade, not a Weapon).
        public static bool HasAnyHandItem(Mech mech)
        {
            if (mech?.allComponents == null)
            {
                return false;
            }
            foreach (MechComponent component in mech.allComponents)
            {
                if (component?.componentDef != null && RequiresHand(component.componentDef))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetActiveHandSlot(Mech mech)
        {
            StatCollection stats = mech.StatCollection;
            if (stats.ContainsStatistic(ActiveHandSlotStat))
            {
                return stats.GetStatistic(ActiveHandSlotStat).Value<string>();
            }

            // Lazy default, first query only: whichever hand-required
            // Weapon (not the melee Upgrade) is currently mounted on a real
            // arm — "the weapon equipped to the arm is the default one when
            // starting the battle," per the user's own design.
            string defaultSlot = MeleeSlotValue;
            if (mech.Weapons != null)
            {
                foreach (Weapon weapon in mech.Weapons)
                {
                    if (weapon != null
                        && RequiresHand(weapon.componentDef)
                        && ((ChassisLocations)weapon.Location == ChassisLocations.LeftArm
                            || (ChassisLocations)weapon.Location == ChassisLocations.RightArm))
                    {
                        defaultSlot = weapon.defId;
                        break;
                    }
                }
            }

            stats.AddStatistic(ActiveHandSlotStat, defaultSlot);
            return defaultSlot;
        }

        public static void SetActiveHandSlot(Mech mech, string value)
        {
            StatCollection stats = mech.StatCollection;
            if (stats.ContainsStatistic(ActiveHandSlotStat))
            {
                stats.GetStatistic(ActiveHandSlotStat).SetValue(value);
            }
            else
            {
                stats.AddStatistic(ActiveHandSlotStat, value);
            }

            // Keep the HUD's own "included in this fire order" flag from
            // showing a just-benched hand-weapon as on — it can't actually
            // fire anymore (HasAmmo below now says no for it), but leaving
            // IsEnabled stale would still highlight it.
            if (mech.Weapons == null)
            {
                return;
            }
            foreach (Weapon weapon in mech.Weapons)
            {
                if (weapon != null && RequiresHand(weapon.componentDef) && weapon.defId != value && weapon.IsEnabled)
                {
                    weapon.DisableWeapon();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Weapon), "HasAmmo", MethodType.Getter)]
    public static class Weapon_HasAmmo_HandExclusivity_Patch
    {
        public static void Postfix(Weapon __instance, ref bool __result)
        {
            if (!__result || !GundamUCHandWeapon.RequiresHand(__instance.componentDef))
            {
                return;
            }

            if (!(__instance.parent is Mech mech))
            {
                return;
            }

            if (!GundamUCHandWeapon.HasFunctionalArm(mech))
            {
                __result = false;
                return;
            }

            if (GundamUCHandWeapon.GetActiveHandSlot(mech) != __instance.defId)
            {
                __result = false;
            }
        }
    }

    // Melee doesn't need its own HasAmmo gate (the mech's built-in
    // MeleeWeapon isn't tagged gundamuc_requireshand — only the Beam Saber
    // Upgrade item that boosts it is), and vanilla's own
    // MeleeRules.GetValidMeleeAttackTypes already blocks melee entirely once
    // the mech's designated punching arm is destroyed. This patch only
    // needs to claim the hand slot the instant a melee attack is committed,
    // so the two ranged weapons become correctly blocked for that round via
    // the patch above.
    [HarmonyPatch(typeof(MechMeleeSequence), "OnAdded")]
    public static class MechMeleeSequence_OnAdded_HandSwitch_Patch
    {
        public static void Postfix(MechMeleeSequence __instance)
        {
            Mech mech = __instance.OwningMech;
            if (mech == null || !GundamUCHandWeapon.HasAnyHandItem(mech))
            {
                return;
            }

            if (GundamUCHandWeapon.HasFunctionalArm(mech))
            {
                GundamUCHandWeapon.SetActiveHandSlot(mech, GundamUCHandWeapon.MeleeSlotValue);
            }
        }
    }

    // Manual toggle between the two ranged hand weapons (Beam Rifle vs.
    // Bazooka): reuses the combat HUD's existing per-weapon-slot click
    // handler rather than building any new UI. Runs as a Prefix (not
    // skip-original) so the original method's own enable/disable-for-firing
    // logic still runs immediately after, now seeing the updated active
    // slot — clicking a benched hand-weapon first switches which one is
    // active, then falls straight into vanilla's own "enable this weapon"
    // branch, which succeeds because HasAmmo now agrees.
    [HarmonyPatch(typeof(CombatHUDWeaponSlot), "OnPointerUp")]
    public static class CombatHUDWeaponSlot_OnPointerUp_HandToggle_Patch
    {
        public static void Prefix(CombatHUDWeaponSlot __instance, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left
                || __instance.weaponSlotType == CombatHUDWeaponSlot.WeaponSlotType.Melee
                || __instance.weaponSlotType == CombatHUDWeaponSlot.WeaponSlotType.DFA)
            {
                return;
            }

            Weapon weapon = __instance.DisplayedWeapon;
            if (weapon == null || !GundamUCHandWeapon.RequiresHand(weapon.componentDef))
            {
                return;
            }

            if (!(weapon.parent is Mech mech))
            {
                return;
            }

            if (GundamUCHandWeapon.GetActiveHandSlot(mech) == weapon.defId)
            {
                return;
            }

            GundamUCHandWeapon.SetActiveHandSlot(mech, weapon.defId);
        }
    }
}
