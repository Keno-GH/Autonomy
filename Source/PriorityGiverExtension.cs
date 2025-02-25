using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    public class PriorityGiverExtension : DefModExtension
    {
        public List<PriorityGiver> priorityGivers;
    }

    public class PriorityGiver
    {
        public string condition;
        public string priority;
        public string type;
        public string skill;
        public string stat;
        public string typeMultiplier;
        public string description;
        public string workPreferenceScoreRange;
        public string validRange;
        public bool useAverage;
        public List<RangeData> rangeDatas;
    }

    public class RangeData
    {
        public string validRange;
        public string priority;
        public string description;
    }

    public static class PriorityGiverUtility
    {
        private static Dictionary<Pawn, Dictionary<WorkTypeDef, (int priority, List<string> descriptions)>> pawnWorkPriorities = new Dictionary<Pawn, Dictionary<WorkTypeDef, (int priority, List<string> descriptions)>>();

        public static void SetWorkPriorities(Pawn pawn, Dictionary<string, float> mapInfo)
        {
            if (pawn.workSettings == null || !pawn.workSettings.EverWork)
                return;

            Dictionary<WorkTypeDef, int> workPriorities = new Dictionary<WorkTypeDef, int>();
            var workDrivePreferences = WorkDriveCalculator.CalculateWorkDrivePreferences(pawn);
            var workTypePriorities = new Dictionary<WorkTypeDef, (int priority, List<string> descriptions)>();
            Dictionary<string, float> pawnInfo = InfoProvider.GetPawnInfo(pawn);

            foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (pawn.WorkTypeIsDisabled(workTypeDef))
                    continue;
                var (priority, descriptions) = PriorityCalculator.GetPriority(workTypeDef, pawn.Map, pawn, workDrivePreferences, pawnInfo, mapInfo);
                workPriorities[workTypeDef] = priority;
                workTypePriorities[workTypeDef] = (priority, descriptions);
            }

            pawnWorkPriorities[pawn] = workTypePriorities;

            if (workPriorities.Count == 0)
                return;

            int minPriority = workPriorities.Values.Min();
            int maxPriority = workPriorities.Values.Max();
            int range = maxPriority - minPriority;
            int step = range / 5;

            foreach (var kvp in workPriorities) // Set the work priorities based on the priority range
            {
                WorkTypeDef workTypeDef = kvp.Key;
                int priority = kvp.Value;
                if (priority >= minPriority && priority < minPriority + step)
                {
                    if (priority < 0) {
                        pawn.workSettings.SetPriority(workTypeDef, 0); // Only disable lowest priority if the priority is negative
                    }
                    else {
                        pawn.workSettings.SetPriority(workTypeDef, 4);
                    }
                        
                }
                else if (priority >= minPriority + step && priority < minPriority + 2 * step)
                {
                    pawn.workSettings.SetPriority(workTypeDef, 4);
                }
                else if (priority >= minPriority + 2 * step && priority < minPriority + 3 * step)
                {
                    pawn.workSettings.SetPriority(workTypeDef, 3);
                }
                else if (priority >= minPriority + 3 * step && priority < minPriority + 4 * step)
                {
                    pawn.workSettings.SetPriority(workTypeDef, 2);
                }
                else
                {
                    pawn.workSettings.SetPriority(workTypeDef, 1);
                }
            }
        }

        public static (int priority, List<string> descriptions) GetSavedPriority(Pawn pawn, WorkTypeDef workTypeDef)
        {
            if (pawnWorkPriorities.TryGetValue(pawn, out var workTypePriorities) && workTypePriorities.TryGetValue(workTypeDef, out var priorityData))
            {
                return priorityData;
            }
            return (0, new List<string>());
        }
    }
}
