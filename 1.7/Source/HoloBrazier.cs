using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EB
{
    public class CompProperties_HoloBrazier : CompProperties
    {
        public string shaderDef = "ElectricBrazier_Hologram";

        public string bodyTexPath;
        public float bodySize = 1f;
        public Color bodyColor = new Color(0.55f, 0.85f, 1f, 0.55f);

        public List<string> flameTexPaths = new List<string>();
        public float flameSize = 0.83f;
        public Vector3 flameOffset = new Vector3(0f, 0f, 0.18f);
        // Relative odds of each flame frame being picked on a long tick (A : B : C).
        // 6 : 2 : 1  ->  A = 2/3, B = 2/3 of the remaining 1/3 (2/9), C = the rest (1/9).
        public List<float> flameFrameWeights = new List<float> { 6f, 2f, 1f };
        public Color flameColor = new Color(0.65f, 0.9f, 1f, 0.9f);
        public Color flameColorDarklight = new Color(0.35f, 0.95f, 0.95f, 0.9f);

        public CompProperties_HoloBrazier()
        {
            compClass = typeof(CompHoloBrazier);
        }
    }

    /// <summary>
    /// Draws the holographic brazier body and flame above the projector disc while powered.
    /// The flame frame is re-rolled once per long tick (needs tickerType Long); drawing just reuses it.
    /// </summary>
    public class CompHoloBrazier : ThingComp
    {
        private CompPowerTrader power;
        private CompToggleGlower toggle;

        private Material bodyMat;
        private Material[] flameMats;
        private Material[] flameMatsDark;
        private bool matsResolved;
        private int curFrame;

        public CompProperties_HoloBrazier Props => (CompProperties_HoloBrazier)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            power = parent.GetComp<CompPowerTrader>();
            toggle = parent.GetComp<CompToggleGlower>();
            toggle?.RegisterParentPowerTracer(power);
        }

        public override void CompTickLong()
        {
            base.CompTickLong();
            curFrame = RollFrame();
        }

        private int RollFrame()
        {
            int n = Props.flameTexPaths.Count;
            if (n <= 1)
                return 0;

            List<float> w = Props.flameFrameWeights;
            float total = 0f;
            for (int i = 0; i < n; i++)
                total += (w != null && i < w.Count) ? w[i] : 0f;
            if (total <= 0f)
                return 0;

            float roll = Rand.Value * total;
            for (int i = 0; i < n; i++)
            {
                roll -= (w != null && i < w.Count) ? w[i] : 0f;
                if (roll < 0f)
                    return i;
            }
            return 0;
        }

        private void ResolveMaterials()
        {
            matsResolved = true;
            ShaderDef def = DefDatabase<ShaderDef>.GetNamedSilentFail(Props.shaderDef);
            Shader shader = def != null ? def.Shader : null;
            if (shader == null)
            {
                Log.ErrorOnce($"[Electric Braziers] Hologram shader '{Props.shaderDef}' not found; using TransparentPostLight.", 0x45421);
                shader = ShaderDatabase.TransparentPostLight;
            }

            if (!Props.bodyTexPath.NullOrEmpty())
                bodyMat = MaterialPool.MatFrom(Props.bodyTexPath, shader, Props.bodyColor);

            int n = Props.flameTexPaths.Count;
            flameMats = new Material[n];
            flameMatsDark = new Material[n];
            for (int i = 0; i < n; i++)
            {
                flameMats[i] = MaterialPool.MatFrom(Props.flameTexPaths[i], shader, Props.flameColor);
                flameMatsDark[i] = MaterialPool.MatFrom(Props.flameTexPaths[i], shader, Props.flameColorDarklight);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (power == null || !power.PowerOn)
                return;

            if (!matsResolved)
                ResolveMaterials();

            Vector3 pos = parent.DrawPos;
            pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();

            if (bodyMat != null)
            {
                Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(Props.bodySize, 1f, Props.bodySize));
                Graphics.DrawMesh(MeshPool.plane10, m, bodyMat, 0);
            }

            if (flameMats != null && flameMats.Length > 0)
            {
                int frame = curFrame < flameMats.Length ? curFrame : 0;
                Material[] set = (toggle != null && toggle.IsDarklight) ? flameMatsDark : flameMats;

                Vector3 fpos = pos + Props.flameOffset;
                fpos.y = pos.y + 0.0135f; // just above the body
                Matrix4x4 m = Matrix4x4.TRS(fpos, Quaternion.identity, new Vector3(Props.flameSize, 1f, Props.flameSize));
                Graphics.DrawMesh(MeshPool.plane10, m, set[frame], 0);
            }
        }
    }
}
