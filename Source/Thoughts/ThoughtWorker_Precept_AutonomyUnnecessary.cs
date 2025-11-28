using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// ThoughtWorker for pawns with the "unnecessary" autonomy precept.
    /// Gives a +1 mood buff when autonomy is paused.
    /// "I prefer to let others decide what I am best suited for."
    /// 
    /// Does not apply to prisoners or slaves.
    /// </summary>
    public class ThoughtWorker_Precept_AutonomyUnnecessary : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            // Skip for prisoners
            if (p.IsPrisoner)
            {
                return ThoughtState.Inactive;
            }

            // Skip for slaves
            if (p.IsSlave)
            {
                return ThoughtState.Inactive;
            }

            // Check if pawn has autonomy paused - they get a mood buff when paused
            var tracker = Current.Game?.GetComponent<PawnAutonomyPauseTracker>();
            if (tracker == null || !tracker.IsPaused(p))
            {
                return ThoughtState.Inactive;
            }

            return ThoughtState.ActiveDefault;
        }
    }
}
