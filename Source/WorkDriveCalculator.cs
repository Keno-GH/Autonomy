using System.Collections.Generic;
using System.Linq;
using RimWorld;
using System;
using Verse;

namespace Autonomy
{
    public static class WorkDriveCalculator
    {
        public static Dictionary<string, int> CalculateWorkDrivePreferences(Pawn pawn)
        {
            var (preferences, _) = CalculateWorkDrivePreferencesWithInfluences(pawn);
            return preferences;
        }

        public static (Dictionary<string, int> preferences, Dictionary<string, List<string>> influences) CalculateWorkDrivePreferencesWithInfluences(Pawn pawn)
        {
            var preferences = new Dictionary<string, int>();
            var influences = new Dictionary<string, List<string>>();

            foreach (var axisDef in DefDatabase<WorkdrivePreferenceAxisDef>.AllDefs)
            {
                preferences[axisDef.defName] = 0;
                influences[axisDef.defName] = new List<string>();
            }

            // Process Traits
            if (pawn.story?.traits != null)
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    var extension = trait.def.GetModExtension<WorkDriveGiverExtension>();
                    if (extension != null)
                    {
                        string sourceDetail = $"Has trait: {trait.LabelCap}";
                        List<WorkDriveGiver> giversToApply = new List<WorkDriveGiver>();

                        if (extension.degreeDatas != null && extension.degreeDatas.Any(dd => dd.degree == trait.Degree))
                        {
                            // Use degree-specific givers if present and matching
                            giversToApply.AddRange(extension.degreeDatas.First(dd => dd.degree == trait.Degree).workDriveGivers);
                        }
                        else if (extension.workDriveGivers != null)
                        {
                            // Fallback to general givers if no degree-specific ones match or exist
                            giversToApply.AddRange(extension.workDriveGivers);
                        }
                        
                        ApplyGivers(giversToApply, preferences, influences, sourceDetail);
                    }
                }
            }

            // Process Genes
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var extension = gene.def.GetModExtension<WorkDriveGiverExtension>();
                    if (extension != null && extension.workDriveGivers != null)
                    {
                        string sourceDetail = $"Has gene: {gene.LabelCap}";
                        ApplyGivers(extension.workDriveGivers, preferences, influences, sourceDetail);
                    }
                }
            }

            // Process Ideology Precepts
            if (pawn.Ideo != null)
            {
                foreach (var precept in pawn.Ideo.PreceptsListForReading)
                {
                    var extension = precept.def.GetModExtension<WorkDriveGiverExtension>();
                    if (extension != null && extension.workDriveGivers != null)
                    {
                        string sourceDetail = $"Ideology precept: {precept.LabelCap}";
                        ApplyGivers(extension.workDriveGivers, preferences, influences, sourceDetail);
                    }
                }
            }
            
            // Placeholder for Backstory check - requires similar logic if backstories can have WorkDriveGiverExtension

            var clampedPreferences = new Dictionary<string, int>();
            foreach (var pref in preferences)
            {
                clampedPreferences[pref.Key] = Math.Max(-10, Math.Min(10, pref.Value));
            }

            return (clampedPreferences, influences);
        }

        private static void ApplyGivers(List<WorkDriveGiver> givers, Dictionary<string, int> preferences, Dictionary<string, List<string>> influences, string sourceDetail)
        {
            if (givers == null) return;

            foreach (var giver in givers)
            {
                if (preferences.ContainsKey(giver.workDrivePreferenceAxis))
                {
                    preferences[giver.workDrivePreferenceAxis] += giver.value;
                    if (!influences[giver.workDrivePreferenceAxis].Contains(sourceDetail))
                    {
                        influences[giver.workDrivePreferenceAxis].Add(sourceDetail);
                    }
                }
            }
        }
    }
}
