using RimWorld;
using UnityEngine;
using Verse;

namespace EB
{
	class CompProperties_PoweredFireOverlay : CompProperties_FireOverlay
	{

		public CompProperties_PoweredFireOverlay()
        {
            this.compClass = typeof(CompPoweredFireOverlay);
        }
    }

	[StaticConstructorOnStartup]
	public class CompPoweredFireOverlay : CompFireOverlay
	{
		public static readonly Graphic DarkFireGraphic = GraphicDatabase.Get<Graphic_Flicker>("Icons/Darkfire", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);

		public new CompProperties_FireOverlay Props => (CompProperties_FireOverlay)props;

		public CompPowerTrader poweredComp;
		private CompToggleGlower toggleComp;

		public override void PostDraw()
		{
			if (this.poweredComp?.PowerOn == true)
			{
				Vector3 drawPos = parent.DrawPos;
				drawPos.y += 3f / 74f;
				if (this.toggleComp.IsDarklight)
					DarkFireGraphic.Draw(drawPos, Rot4.North, parent);
				else
					FireGraphic.Draw(drawPos, Rot4.North, parent);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			this.poweredComp = this.parent.GetComp<CompPowerTrader>();
			this.toggleComp = this.parent.GetComp<CompToggleGlower>();
			this.toggleComp.RegisterParentPowerTracer(this.poweredComp);
		}
	}
}
