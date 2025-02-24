using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Autonomy
{
    [HarmonyPatch(typeof(WidgetsWork), "TipForPawnWorker")]
    public static class WorkTypeDefTooltipPatch
    {
        public static void Postfix(ref string __result, Pawn p, WorkTypeDef wDef, bool incapableBecauseOfCapacities)
        {
            if (p == null || wDef == null)
            {
                return;
            }

            var (priority, descriptions) = PriorityGiverUtility.GetSavedPriority(p, wDef);
            if (priority != 0 && descriptions.Count > 0)
            {
                StringBuilder sb = new StringBuilder(__result);
                sb.AppendLine();
                sb.AppendLine("Priority Givers:");

                foreach (var description in descriptions)
                {
                    sb.AppendLine($"- {description}");
                }

                __result = sb.ToString();
            }
        }
    }
}
