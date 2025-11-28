using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// ThoughtWorker for when a pawn has their autonomy paused.
    /// Stages are based on the duration of the pause:
    /// Stage 0: Less than 8 hours (-3)
    /// Stage 1: 8-24 hours (-6)
    /// Stage 2: 1-3 days (-10)
    /// Stage 3: 3+ days (-15)
    /// 
    /// Does not apply to prisoners, slaves, or masochists.
    /// If Ideology is active, this is handled by precepts instead.
    /// </summary>
    public class ThoughtWorker_AutonomyPaused : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Skip if Ideology is active - thoughts are handled by precepts instead
            if (ModsConfig.IdeologyActive && p.Ideo != null)
            {
                return ThoughtState.Inactive;
            }

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
