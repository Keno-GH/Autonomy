using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy.Workers
{
    public class SkillAffinityWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            var pawnInfo = context.PawnInfo;

            if (!pawnInfo.TryGetValue(giver.skill, out float skillLevel)) return 0;

            foreach (var rangeData in giver.rangeDatas)
            {
                float minSkillLevel = float.Parse(rangeData.validRange.Split('~')[0]);
                float maxSkillLevel = float.Parse(rangeData.validRange.Split('~')[1]);
                if (skillLevel < minSkillLevel || skillLevel > maxSkillLevel) continue;

                int minPriority = int.Parse(rangeData.priority.Split('~')[0]);
                int maxPriority = int.Parse(rangeData.priority.Split('~')[1]);
                float skillRatio = (skillLevel - minSkillLevel) / (maxSkillLevel - minSkillLevel);
                int skillPriority = (int)(minPriority + (skillRatio * (maxPriority - minPriority)));
                int calculatedPriority = skillPriority < minPriority ? minPriority : skillPriority > maxPriority ? maxPriority : skillPriority;

                return calculatedPriority;
            }

            return 0;
        }
    }
}
