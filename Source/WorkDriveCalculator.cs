using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Autonomy
{
    public static class WorkDriveCalculator
    {
        public static Dictionary<string, int> CalculateWorkDrivePreferences(Pawn pawn)
        {
            Dictionary<string, int> workDrivePreferences = new Dictionary<string, int>();

            // Initialize all workDrivePreferences to 0
            IEnumerable<WorkdrivePreferenceAxisDef> allWorkDriveAxes = DefDatabase<WorkdrivePreferenceAxisDef>.AllDefsListForReading;
            foreach (WorkdrivePreferenceAxisDef axis in allWorkDriveAxes)
            {
                workDrivePreferences[axis.defName] = 0;
            }

            foreach (var trait in pawn.story.traits.allTraits)
            {
                var extension = trait.def.GetModExtension<WorkDriveGiverExtension>();
                if (extension != null)
                {
                    // Handle base workDriveGivers
                    if (extension.workDriveGivers != null)
                    {
                        foreach (var giver in extension.workDriveGivers)
                        {
                            if (!workDrivePreferences.ContainsKey(giver.workDrivePreferenceAxis))
                            {
                                Log.Warning($"Work drive preference axis '{giver.workDrivePreferenceAxis}' not found for trait '{trait.def.defName}'. Current work drive preference axes are: {string.Join(", ", workDrivePreferences.Keys)}");
                                continue;
                            }
                            workDrivePreferences[giver.workDrivePreferenceAxis] += giver.value;
                        }
                    }

                    // Handle degree data for spectrum traits
                    if (extension.degreeDatas != null)
                    {
                        foreach (var degreeData in extension.degreeDatas)
                        {
                            if (degreeData.degree != trait.Degree)
                            {
                                continue;
                            }
                            foreach (var giver in degreeData.workDriveGivers)
                            {
                                workDrivePreferences[giver.workDrivePreferenceAxis] += giver.value;
                            }
                        }
                    }
                }
            }

            // Ensure no work drive axis has a value greater than 10 or less than -10
            foreach (string key in workDrivePreferences.Keys.ToList())
            {
                if (workDrivePreferences[key] > 10)
                {
                    workDrivePreferences[key] = 10;
                }
                else if (workDrivePreferences[key] < -10)
                {
                    workDrivePreferences[key] = -10;
                }
            }

            // Debug print the work drive results
            /* Log.Message($"Work drive preferences for {pawn.Name}:");
            foreach (var kvp in workDrivePreferences)
            {
                Log.Message($"{kvp.Key}: {kvp.Value}");
            } */

            return workDrivePreferences;
        }
    }
}
