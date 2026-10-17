using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EB
{
    [StaticConstructorOnStartup]
    static class IconUtil
    {
        public static Texture2D Normal;
        public static Texture2D Dark;

        static IconUtil()
        {
            Normal = ContentFinder<Texture2D>.Get("Icons/fire", true);
            Dark = ContentFinder<Texture2D>.Get("Icons/Darkfire/DarkfireA", true);
        }
    }

    public class CompProperties_ToggleGlower : CompProperties_Glower
    {
        public CompProperties_ToggleGlower()
        {
            base.compClass = typeof(CompToggleGlower);
        }
    }

    class CompToggleGlower : CompGlower
    {
        public bool IsDarklight = false;
        private CompPowerTrader parentPowerTrader;
        public void RegisterParentPowerTracer(CompPowerTrader parentComp)
        {
            parentPowerTrader = parentComp;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;
            if (ModsConfig.IdeologyActive)
            {
                Texture2D icon;

                if (IsDarklight)
                    icon = IconUtil.Dark;
                else
                    icon = IconUtil.Normal;

                yield return new Command_Action
                {
                    action = delegate {
                        IsDarklight = !IsDarklight;
                        SetLightColor();
                        if (parentPowerTrader?.PowerOn == true)
                        {
                            base.parent.Map.glowGrid.DeRegisterGlower(this);
                            base.parent.Map.glowGrid.RegisterGlower(this);
                        }
                        base.parent.Map.mapDrawer.MapMeshDirty(base.parent.Position, MapMeshFlagDefOf.Things);
                    },
                    defaultLabel = "EB.ToggleGlowColor".Translate(),
                    defaultDesc = "EB.ToggleGlowColorDesc".Translate(),
                    icon = icon
                };
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SetLightColor();

        }

        private void SetLightColor()
        {
            if (IsDarklight)
                base.Props.glowColor = new ColorInt(78, 226, 229, 0);
            else
                base.Props.glowColor = new ColorInt(252, 187, 113, 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref IsDarklight, "isDarklight");
        }
    }
}
