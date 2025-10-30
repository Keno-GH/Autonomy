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
    [DefOf]
    public class AutonomyDefOf
    {
        public static LetterDef success_letter;
    }

    public class AutonomyMapComponent : MapComponent
    {
        public AutonomyMapComponent(Map map) : base(map){}
        public override void FinalizeInit()
        {
            Messages.Message("Autonomy loaded successfully", null, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(new TaggedString("Autonomy"), new TaggedString("Autonomy mod has been loaded successfully!"), AutonomyDefOf.success_letter, "", 0);
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

    [HarmonyPatch(typeof(LetterStack), "ReceiveLetter")]
    [HarmonyPatch(new Type[] {typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(string), typeof(int), typeof(bool)})]
    public static class LetterTextChange
    {
        public static bool Prefix(ref TaggedString text)
        {
            text += new TaggedString(" with harmony");
            return true;
        }
    }

}
