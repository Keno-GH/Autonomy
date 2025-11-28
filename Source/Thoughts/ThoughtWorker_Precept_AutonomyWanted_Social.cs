using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// Social ThoughtWorker for pawns with the "wanted" autonomy precept.
    /// Gives a -5 opinion of other pawns who have autonomy disabled.
    /// "They shouldn't let others decide for them."
    /// 
    /// Does not apply when observing slaves (as they are not considered free people).
    /// </summary>
    public class ThoughtWorker_Precept_AutonomyWanted_Social : ThoughtWorker_Precept_Social
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

            // Check if the other pawn has autonomy paused
            var tracker = Current.Game?.GetComponent<PawnAutonomyPauseTracker>();
            if (tracker == null || !tracker.IsPaused(otherPawn))
            {
                return ThoughtState.Inactive;
            }

            return ThoughtState.ActiveDefault;
        }
    }
}
