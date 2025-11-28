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

            // Calculate stage based on pause duration
            int stage = GetPauseStage(p, tracker);
            return ThoughtState.ActiveAtStage(stage);
        }

        private int GetPauseStage(Pawn p, PawnAutonomyPauseTracker tracker)
        {
            // Forever pause = max stage (3+ days equivalent)
            if (tracker.IsForeverPause(p))
            {
                return 3;
            }

            // Get actual paused duration in ticks
            int pausedTicks = tracker.GetPausedDurationTicks(p);
            
            // Stage 0: Less than 8 hours (-3 mood)
            // Stage 1: 8-24 hours (-6 mood)
            // Stage 2: 1-3 days (-10 mood)
            // Stage 3: 3+ days or forever (-15 mood)
            
            if (pausedTicks >= PawnAutonomyPauseTracker.PAUSE_3_DAYS)
            {
                return 3;
            }
            else if (pausedTicks >= PawnAutonomyPauseTracker.PAUSE_24_HOURS)
            {
                return 2;
            }
            else if (pausedTicks >= PawnAutonomyPauseTracker.PAUSE_8_HOURS)
            {
                return 1;
            }
            
            return 0;
        }
    }
}
