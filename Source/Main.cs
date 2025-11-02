using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Verse.Noise;
using Verse.Grammar;
using RimWorld;
using RimWorld.Planet;

using System.Reflection;
using HarmonyLib;

namespace Autonomy
{
    public class AutonomyMapComponent : MapComponent
    {
        public AutonomyMapComponent(Map map) : base(map){}
        public override void FinalizeInit()
        {
            Messages.Message("Autonomy loaded successfully", null, MessageTypeDefOf.PositiveEvent);
        }
    }

    [StaticConstructorOnStartup]
    public static class Start
    {
        static Start()
        {
            Log.Message("Autonomy mod loaded successfully!");

            Harmony harmony = new Harmony("keno.autonomy");
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
        }
    }

}
