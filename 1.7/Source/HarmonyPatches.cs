using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using System.Text;
using Verse;

namespace EB
{
    [StaticConstructorOnStartup]
    class Main
    {
        public static Pawn ChoicesForPawn = null;

        static Main()
        {
            var harmony = new Harmony("com.electricbrazier.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            LongEventHandler.QueueLongEvent(new Action(Init), "LibraryStartup", false, null);
        }

        private static void Init()
        {
            ThingDef td = DefDatabase<ThingDef>.GetNamed("Brazier", false);
            if (DefOf.ElectricBrazier == null)
                Log.Error("ElectricBrazier is null");
            else if (td != null)
            {
                foreach (var c in td.comps)
                {
                    if (c is CompProperties_MeditationFocus m)
                    {
                        FocusStrengthOffset_Lit l = null;
                        FocusStrengthOffset_BuildingDefsLit fso = null;
                        foreach (var o in m.offsets)
                        {
                            if (o is FocusStrengthOffset_BuildingDefsLit t)
                                fso = t;
                            else if (o is FocusStrengthOffset_Lit tt)
                                l = tt;
                        }
                        if (l != null && fso != null)
                        {
                            foreach (var extra in ExtraBraziers)
                                fso.defs.Add(new MeditationFocusOffsetPerBuilding(extra, l.offset));
                        }
                        else
                        {
                            Log.Error("failed to apply ElectricBrazier to Brazier meditation");
                        }
                    }
                }
            }

            // Vanilla Royalty Support
            bool patched = false;
            foreach(var d in DefDatabase<RoyalTitleDef>.AllDefs)
            {
                if (d.bedroomRequirements != null)
                {
                    foreach (var c in d?.bedroomRequirements)
                        if (c is RoomRequirement_ThingAnyOf rr && AddElectricBrazier(rr))
                            patched = true;
                }
                if (d.throneRoomRequirements != null)
                {
                    foreach (var c in d?.throneRoomRequirements)
                        if (c is RoomRequirement_ThingAnyOf rr && AddElectricBrazier(rr))
                            patched = true;

                }
            }
            if (patched)
            {
                Log.Message("[Electric Braziers] Successfully patched Vanilla Expanded - Royaltys Patch");
            }
        }

        /// <summary>Our braziers that should count wherever a vanilla brazier counts.</summary>
        public static System.Collections.Generic.IEnumerable<ThingDef> ExtraBraziers
        {
            get
            {
                if (DefOf.ElectricBrazier != null) yield return DefOf.ElectricBrazier;
                if (DefOf.HolographicBrazier != null) yield return DefOf.HolographicBrazier;
            }
        }

        private static bool AddElectricBrazier(RoomRequirement_ThingAnyOf rr)
        {
            if (!rr.things.Contains(DefOf.Brazier))
                return false;
            foreach (var extra in ExtraBraziers)
                if (!rr.things.Contains(extra))
                    rr.things.Add(extra);
            return true;
        }
    }

    [RimWorld.DefOf]
    public static class DefOf
    {
        public static ThingDef Brazier;
        public static ThingDef ElectricBrazier;
        public static ThingDef HolographicBrazier;
    }

    [HarmonyPatch(typeof(RoomRequirement_ThingCount), "Count")]
    static class Patch_RoomRequirement_ThingCount_Count
    {
        static void Postfix(RoomRequirement_ThingCount __instance, ref int __result, Room r)
        {
            if (__instance.thingDef == DefOf.Brazier)
            {
                foreach (var extra in Main.ExtraBraziers)
                    __result += r.ThingCount(extra);
            }
        }
    }

    /*[HarmonyPatch(typeof(RoomRequirement_AllThingsAreGlowing), "Met")]
    static class Patch_RoomRequirement_ThingCount_Met
    {
        static void Postfix(RoomRequirement_AllThingsAreGlowing __instance, ref bool __result, Room r)
        {
            Log.Error($"Met start {__instance.thingDef == DefOf.Brazier} && {__result == false}");
            if (__instance.thingDef == DefOf.Brazier && __result == false)
            {
                foreach (Thing item in r.ContainedThings(DefOf.ElectricBrazier))
                {
                    Log.Error($"Met pre {__result}");
                    __result = !item.TryGetComp<CompPowerTrader>()?.PowerOn == true;
                    Log.Error($"Met post {__result}");
                }
            }
        }
    }*/
}