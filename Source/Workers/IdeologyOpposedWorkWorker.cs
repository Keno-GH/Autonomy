using RimWorld;
using Verse;
using System.Globalization; // Required for int.TryParse with InvariantCulture
using System.Linq; // Required for .Contains()

namespace Autonomy.Workers
{
    public class IdeologyOpposedWorkWorker : IPriorityWorker
    {
        public int CalculatePriority(PriorityGiver giver, PriorityCalculationContext context)
        {
            Pawn pawn = context.Pawn;
            WorkTypeDef workTypeDef = context.WorkTypeDef; // Correctly accessed now

            if (pawn == null || workTypeDef == null || pawn.Ideo == null)
            {
                return 0; // No pawn, worktype, or ideology, so no effect
            }

            // Iterate through the pawn's ideology precepts
            foreach (Precept precept in pawn.Ideo.PreceptsListForReading)
            {
                // Check if the precept has disapproved work types and if the current workTypeDef is among them
                if (precept.def.opposedWorkTypes != null && precept.def.opposedWorkTypes.Contains(workTypeDef)) // Corrected to opposedWorkTypes
                {
                    // Try to parse the priority from the giver's priority field
                    // It's good practice to use InvariantCulture for parsing internal data
                    if (int.TryParse(giver.priority, NumberStyles.Integer, CultureInfo.InvariantCulture, out int priorityValue))
                    {
                        return priorityValue;
                    }
                    // Fallback to a default strong negative priority if parsing fails or priority is not specified
                    // This ensures a strong negative impact even if configuration is minimal.
                    return -100; 
                }
            }

            return 0; // Work type is not opposed by any precept, so no effect on priority
        }
    }
}
