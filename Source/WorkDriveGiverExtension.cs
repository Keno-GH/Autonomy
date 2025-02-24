using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Autonomy
{
    public class WorkDriveGiverExtension : DefModExtension
    {
        public List<WorkDriveGiver> workDriveGivers;
        public List<WorkDriveGiverDegreeData> degreeDatas;
    }

    public class WorkDriveGiver
    {
        public string workDrivePreferenceAxis;
        public int value;
    }

    public class WorkDriveGiverDegreeData
    {
        public int degree;
        public List<WorkDriveGiver> workDriveGivers;
    }
}
