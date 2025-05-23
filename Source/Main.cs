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
            base.FinalizeInit();
            Messages.Message("Success", null, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(new TaggedString("Success"), new TaggedString("Success message"), LetterDefinition.success_letter, "", 0);

            // Enable manual priorities
            Find.PlaySettings.useWorkPriorities = true;

            InitializeAutonomyCompForAllPawns();
        }

        private void InitializeAutonomyCompForAllPawns()
        {
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                if (pawn.workSettings != null && pawn.workSettings.EverWork)
                {
                    AddAutonomyCompIfMissing(pawn);
                }
            }
        }

        private void AddAutonomyCompIfMissing(Pawn pawn)
        {
            var comp = pawn.GetComp<CompAutonomy>();
            if (comp == null)
            {
                comp = new CompAutonomy();
                pawn.AllComps.Add(comp);
                comp.Enabled = true; // Initialize with the comp enabled
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            tickCounter++;
            if (tickCounter >= tickInterval)
            {
                Log.Message("Hello world every " + tickInterval + " ticks!");

                if (map == null || map.mapPawns == null)
                {
                    Log.Error("Map or mapPawns is null.");
                    return;
                }

                // Collect the pawns in a separate list to avoid modifying the collection while iterating
                List<Pawn> workingColonists = map.mapPawns.FreeColonists
                .Where(p => p.workSettings != null && p.workSettings.EverWork && p.ageTracker.CurLifeStage != null && p.ageTracker.CurLifeStage.defName != "HumanlikeBaby")
                .ToList();

                Log.Message("Colonists: " + workingColonists.Count);
                
                var priorityGivers = DefDatabase<WorkTypeDef>.AllDefs.SelectMany(w => w.GetModExtension<PriorityGiverExtension>()?.priorityGivers ?? new List<PriorityGiver>()).ToList();
                Dictionary<string, float> mapInfo = InfoProvider.GetMapInfo(map, priorityGivers, workingColonists);

                foreach (var pawn in workingColonists)
                {
                    var compAutonomy = pawn.GetComp<CompAutonomy>();
                    if (compAutonomy != null && compAutonomy.Enabled)
                    {
                        PriorityGiverUtility.SetWorkPriorities(pawn, mapInfo);
                    }
                    else
                    {
                        Log.Message("Autonomy disabled for working pawn" + pawn.Name);
                    }
                }

                tickCounter = 0;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public static class Pawn_SpawnSetup_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.workSettings != null && __instance.workSettings.EverWork)
            {
                var comp = __instance.GetComp<CompAutonomy>();
                if (comp == null)
                {
                    comp = new CompAutonomy();
                    __instance.AllComps.Add(comp);
                    comp.Enabled = true; // Initialize with the comp enabled
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class Start
    {
        static Start()
        {
            // Log.Message("Mod template loaded successfully!");

            Harmony harmony = new Harmony("Autonomy");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Register workers
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_BaseType", new Autonomy.Workers.BaseTypeWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_ComparedStats", new Autonomy.Workers.ComparedStatsWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_Immunity", new Autonomy.Workers.ImmunityWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_InfoRange", new Autonomy.Workers.InfoRangeWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_Passion", new Autonomy.Workers.PassionWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_SkillAffinity", new Autonomy.Workers.SkillAffinityWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_SurroundingsFilthy", new Autonomy.Workers.SurroundingsFilthyWorker());
            PriorityWorkerRegistry.RegisterWorker("PriorityWorker_Boredom", new Autonomy.Workers.BoredomWorker()); // Added BoredomWorker registration
            PriorityWorkerRegistry.RegisterWorker("Autonomy.Workers.IdeologyOpposedWorkWorker", new Autonomy.Workers.IdeologyOpposedWorkWorker()); // Added IdeologyOpposedWorkWorker registration
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

    // Patch for adding a custom stat to the pawn's info card (Stats tab)
    [HarmonyPatch(typeof(RimWorld.StatsReportUtility), "StatsToDraw", new Type[] { typeof(Verse.Thing) })]
    public static class StatsReportUtility_StatsToDraw_Patch
    {
        public static void Postfix(ref IEnumerable<StatDrawEntry> __result, Thing thing)
        {
            if (thing is Pawn pawn && pawn.IsColonist)
            {
                List<StatDrawEntry> entries = new List<StatDrawEntry>(__result);

                // Calculate work drive preferences and their influences
                var (workDrives, workDriveInfluences) = WorkDriveCalculator.CalculateWorkDrivePreferencesWithInfluences(pawn);
                StringBuilder workDrivesValueString = new StringBuilder();
                StringBuilder workDrivesReportString = new StringBuilder();

                workDrivesReportString.AppendLine("The pawn's work drives and the factors influencing them (traits, genes, ideology, etc.).");
                workDrivesReportString.AppendLine(); // Add a blank line for spacing

                foreach (var kvp in workDrives)
                {
                    WorkdrivePreferenceAxisDef axisDef = DefDatabase<WorkdrivePreferenceAxisDef>.GetNamed(kvp.Key, false);
                    string keyLabel = axisDef != null ? axisDef.label : kvp.Key;
                    string valueLabel;

                    if (axisDef != null && axisDef.workDrivesStepLabels != null && axisDef.workDrivesStepLabels.Count == 4)
                    {
                        int value = kvp.Value;
                        if (value >= -10 && value <= -4) valueLabel = axisDef.workDrivesStepLabels[0];
                        else if (value >= -3 && value <= 0) valueLabel = axisDef.workDrivesStepLabels[1];
                        else if (value >= 1 && value <= 4) valueLabel = axisDef.workDrivesStepLabels[2];
                        else if (value >= 5 && value <= 10) valueLabel = axisDef.workDrivesStepLabels[3];
                        else valueLabel = value.ToString();
                    }
                    else
                    {
                        valueLabel = kvp.Value.ToString();
                    }
                    workDrivesValueString.AppendLine(valueLabel); // This will be the concise value shown on the left
                    workDrivesReportString.AppendLine($"{keyLabel}: {valueLabel}");

                    // Append influences for this work drive axis
                    if (workDriveInfluences.TryGetValue(kvp.Key, out List<string> influences) && influences.Any())
                    {
                        workDrivesReportString.AppendLine("  Has this work drive because:");
                        foreach (var influence in influences)
                        {
                            workDrivesReportString.AppendLine($"  - {influence}");
                        }
                    }
                    else
                    {
                        workDrivesReportString.AppendLine("  Influences: None specific.");
                    }
                    workDrivesReportString.AppendLine(); // Add a blank line after each work drive for readability
                }

                StatDrawEntry workDriveEntry = new StatDrawEntry(
                    StatCategoryDefOf.Basics, 
                    "Work Drives",      
                    workDrivesValueString.ToString().TrimEnd(),      
                    workDrivesReportString.ToString().TrimEnd(), 
                    9998,                     
                    null,                     // overrideReportTitle
                    null,                     // hyperlinks
                    false                     // forceUnfinalizedMode
                );
                entries.Add(workDriveEntry);

                __result = entries;
            }
        }
    }
}
