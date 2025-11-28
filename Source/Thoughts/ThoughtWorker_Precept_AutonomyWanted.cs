using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// ThoughtWorker for pawns with the "wanted" autonomy precept.
    /// Gives a mood debuff when autonomy is paused, similar to ThoughtWorker_AutonomyPaused
    /// but specifically for ideology pawns with this precept.
    /// 
    /// Stages:
    /// Stage 0: Less than 8 hours (-3)
    /// Stage 1: 8-24 hours (-6)
    /// Stage 2: 1-3 days (-10)
    /// Stage 3: 3+ days (-15)
    /// 
    /// Does not apply to prisoners, slaves, or masochists.
    /// </summary>
    public class ThoughtWorker_Precept_AutonomyWanted : ThoughtWorker_Precept
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

            // Skip for masochists
            if (p.story?.traits?.HasTrait(TraitDefOf.Masochist) == true)
            {
                return ThoughtState.Inactive;
            }

            // Check if pawn has autonomy paused
            var tracker = Current.Game?.GetComponent<PawnAutonomyPauseTracker>();
            if (tracker == null || !tracker.IsPaused(p))
            {
                return ThoughtState.Inactive;
            }

            // Calculate stage based on pause duration using shared method
            int stage = tracker.GetPauseThoughtStage(p);
            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
