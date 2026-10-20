using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EB
{
    /// <summary>
    /// Drives the hologram shader. The clock only advances, and flicker is only enabled,
    /// while the game is unpaused at normal (1x) speed. At any other speed the hologram holds still.
    /// Values are pushed as shader globals (same mechanism the game uses for _GameSeconds).
    /// </summary>
    public static class HoloClock
    {
        private static readonly int TimeId = Shader.PropertyToID("_EBHoloTime");
        private static readonly int FlickerId = Shader.PropertyToID("_EBHoloFlicker");

        private static int lastFrame = -1;
        private static float time;

        public static float Time => time;
        public static bool Running { get; private set; }

        public static void Update()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame == lastFrame)
                return;
            lastFrame = frame;

            TickManager tm = Find.TickManager;
            Running = tm != null && !tm.Paused && tm.CurTimeSpeed == TimeSpeed.Normal;
            if (Running)
                time += Mathf.Min(UnityEngine.Time.unscaledDeltaTime, 0.1f);

            // Keep the value small so float precision in the shader stays good on long sessions.
            if (time > 3600f)
                time -= 3600f;

            Shader.SetGlobalFloat(TimeId, time);
            Shader.SetGlobalFloat(FlickerId, Running ? 1f : 0f);
        }
    }

    public class CompProperties_HoloBrazier : CompProperties
    {
        public string shaderDef = "ElectricBrazier_Hologram";

        public string bodyTexPath;
        public float bodySize = 1f;
        public Color bodyColor = new Color(0.55f, 0.85f, 1f, 0.55f);

        public List<string> flameTexPaths = new List<string>();
        public float flameSize = 0.83f;
        public Vector3 flameOffset = new Vector3(0f, 0f, 0.18f);
        public float flameFramesPerSecond = 6f;
        public Color flameColor = new Color(0.65f, 0.9f, 1f, 0.9f);
        public Color flameColorDarklight = new Color(0.35f, 0.95f, 0.95f, 0.9f);

        public CompProperties_HoloBrazier()
        {
            compClass = typeof(CompHoloBrazier);
        }
    }

    /// <summary>Draws the holographic brazier body and flame above the projector disc while powered.</summary>
    public class CompHoloBrazier : ThingComp
    {
        private CompPowerTrader power;
        private CompToggleGlower toggle;

        private Material bodyMat;
        private Material[] flameMats;
        private Material[] flameMatsDark;
        private bool matsResolved;

        public CompProperties_HoloBrazier Props => (CompProperties_HoloBrazier)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            power = parent.GetComp<CompPowerTrader>();
            toggle = parent.GetComp<CompToggleGlower>();
            toggle?.RegisterParentPowerTracer(power);
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

            HoloClock.Update();
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
                // Frame advances on the hologram clock, so it freezes when paused or fast-forwarded.
                int offset = Mathf.Abs(parent.thingIDNumber * 7919) % flameMats.Length;
                int frame = ((int)(HoloClock.Time * Props.flameFramesPerSecond) + offset) % flameMats.Length;
                Material[] set = (toggle != null && toggle.IsDarklight) ? flameMatsDark : flameMats;

                Vector3 fpos = pos + Props.flameOffset;
                fpos.y = pos.y + 0.0135f; // just above the body
                Matrix4x4 m = Matrix4x4.TRS(fpos, Quaternion.identity, new Vector3(Props.flameSize, 1f, Props.flameSize));
                Graphics.DrawMesh(MeshPool.plane10, m, set[frame], 0);
            }
        }
    }
}
