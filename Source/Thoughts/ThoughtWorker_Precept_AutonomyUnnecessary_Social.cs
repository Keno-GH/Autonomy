using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// Social ThoughtWorker for pawns with the "unnecessary" autonomy precept.
    /// Gives a -5 opinion of other pawns who have autonomy enabled (not paused).
    /// "They shouldn't force people to manage themselves."
    /// 
    /// Does not apply when observing slaves (as they are not considered free people).
    /// </summary>
    public class ThoughtWorker_Precept_AutonomyUnnecessary_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            // Don't apply opinion about slaves
            if (otherPawn.IsSlave)
            {
                return ThoughtState.Inactive;
            }
            
            // Don't apply opinion about prisoners
            if (otherPawn.IsPrisoner)
            {
                return ThoughtState.Inactive;
            }

            // Check if the other pawn has autonomy enabled (not paused)
            var tracker = Current.Game?.GetComponent<PawnAutonomyPauseTracker>();
            
            // If no tracker or pawn is not paused, they have "wanted" autonomy
            if (tracker == null || !tracker.IsPaused(otherPawn))
            {
                return ThoughtState.ActiveDefault;
            }

            return ThoughtState.Inactive;
        }
    }
}
