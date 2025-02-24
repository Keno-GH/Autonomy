using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    public static class CleanlinessUtility
    {
        public static float CalculateCleanliness(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return 0f;
            }

            Room room = pawn.GetRoom();
            float cleanliness;
            if (room != null && !room.PsychologicallyOutdoors)
            {
                cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
            }
            else
            {
                cleanliness = CalculateOutdoorCleanliness(pawn.Position, pawn.Map);
            }

            Log.Message($"Calculated cleanliness for pawn {pawn.Name}: {cleanliness}");
            return cleanliness;
        }

        private static float CalculateOutdoorCleanliness(IntVec3 position, Map map)
        {
            List<IntVec3> surroundingCells = GenRadial.RadialCellsAround(position, 5f, true).ToList();
            float totalCleanliness = 0f;
            int cellCount = 0;

            foreach (var cell in surroundingCells)
            {
                if (cell.InBounds(map) && !cell.Fogged(map))
                {
                    totalCleanliness += BeautyUtility.CellBeauty(cell, map);
                    cellCount++;
                }
            }

            return cellCount > 0 ? totalCleanliness / cellCount : 0f;
        }
    }
}
