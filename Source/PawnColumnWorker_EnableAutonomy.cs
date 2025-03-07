using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Autonomy
{
    public class PawnColumnWorker_EnableAutonomy : PawnColumnWorker_Checkbox
    {
        protected override bool GetValue(Pawn pawn)
        {
            var comp = pawn.GetComp<CompAutonomy>();
            if (comp == null)
            {
                comp = new CompAutonomy();
                pawn.AllComps.Add(comp);
            }
            return comp.Enabled;
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            var comp = pawn.GetComp<CompAutonomy>();
            if (comp == null)
            {
                comp = new CompAutonomy();
                pawn.AllComps.Add(comp);
            }
            comp.Enabled = value;
        }

        protected override string GetTip(Pawn pawn)
        {
            return "Enable or disable autonomy for this pawn.";
        }
    }

    public class CompAutonomy : ThingComp
    {
        public bool Enabled = true;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref Enabled, "Enabled", true);
        }
    }

    public class CompProperties_Autonomy : CompProperties
    {
        public CompProperties_Autonomy()
        {
            this.compClass = typeof(CompAutonomy);
        }
    }
}
