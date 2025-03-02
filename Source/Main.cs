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
    public class LetterDefinition
    {
        public static LetterDef success_letter;
    }

    public class MyMapComponent : MapComponent
    {
        private int tickCounter = 0;
        private const int tickInterval = 1000; // Adjust this value to change the frequency

        public MyMapComponent(Map map) : base(map){}
        public override void FinalizeInit()
        {
            Messages.Message("Success", null, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(new TaggedString("Success"), new TaggedString("Success message"), LetterDefinition.success_letter, "", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            tickCounter++;
            if (tickCounter >= tickInterval)
            {
                Log.Message("Hello world every " + tickInterval + " ticks!");

                // Collect the pawns in a separate list to avoid modifying the collection while iterating
                List<Pawn> workingColonists = map.mapPawns.FreeColonists
                .Where(p => p.workSettings != null && p.workSettings.EverWork && p.ageTracker.CurLifeStage != null && p.ageTracker.CurLifeStage.defName != "HumanlikeBaby")
                .ToList();

                Log.Message("Colonists: " + workingColonists.Count);
                
                var priorityGivers = DefDatabase<WorkTypeDef>.AllDefs.SelectMany(w => w.GetModExtension<PriorityGiverExtension>()?.priorityGivers ?? new List<PriorityGiver>()).ToList();
                Dictionary<string, float> mapInfo = InfoProvider.GetMapInfo(map, priorityGivers, workingColonists);

                foreach (var pawn in workingColonists)
                {
                    PriorityGiverUtility.SetWorkPriorities(pawn, mapInfo);
                }

                tickCounter = 0;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class Start
    {
        static Start()
        {
            // Log.Message("Mod template loaded successfully!");

            Harmony harmony = new Harmony("Template");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Work), "DoWindowContents")]
    public static class WorkTabPatch
    {
        public static void Postfix(Rect rect, MainTabWindow_Work __instance)
        {
            Rect buttonRect = new Rect(rect.width - 110f, 0f, 100f, 30f);
            if (Widgets.ButtonText(buttonRect, "Free Will Info"))
            {
                Find.WindowStack.Add(new FreeWillWindow());
            }
        }
    }

    public class WorkTabWithButton : MainTabWindow_Work
    {
        public override void DoWindowContents(Rect rect)
        {
            base.DoWindowContents(rect);

            Rect buttonRect = new Rect(rect.width - 110f, 0f, 100f, 30f);
            if (Widgets.ButtonText(buttonRect, "Autonomy Info"))
            {
                Find.WindowStack.Add(new FreeWillWindow());
            }
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
