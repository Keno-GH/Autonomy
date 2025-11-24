using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Keno
{
    /// <summary>
    /// Pawn column worker for the pause autonomy button
    /// Allows players to temporarily or permanently pause autonomy for individual pawns
    /// </summary>
    public class PawnColumnWorker_PauseAutonomy : PawnColumnWorker
    {
        private const int Width = 22;
        private const int TopPadding = 4;

        private static Texture2D pausedIcon;
        private static Texture2D runningIcon;
        
        private static Texture2D PausedIcon
        {
            get
            {
                if (pausedIcon == null)
                {
                    pausedIcon = ContentFinder<Texture2D>.Get("UI/Icons/autonomyPaused", true);
                }
                return pausedIcon;
            }
        }
        
        private static Texture2D RunningIcon
        {
            get
            {
                if (runningIcon == null)
                {
                    runningIcon = ContentFinder<Texture2D>.Get("UI/Icons/autonomyRunning", true);
                }
                return runningIcon;
            }
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                return;
            }

            // Get the pause tracker
            var pauseTracker = Current.Game?.GetComponent<Autonomy.PawnAutonomyPauseTracker>();
            if (pauseTracker == null)
            {
                Log.WarningOnce("[Autonomy] PawnAutonomyPauseTracker component not found", 8472651);
                return;
            }

            bool isPaused = pauseTracker.IsPaused(pawn);

            // Select icon based on pause state
            Texture2D icon = isPaused ? PausedIcon : RunningIcon;

            // Draw button
            Rect buttonRect = new Rect(rect.x + (rect.width - Width) / 2f, rect.y + TopPadding, Width, Width);
            
            if (Widgets.ButtonImage(buttonRect, icon))
            {
                ShowPauseMenu(pawn, pauseTracker, isPaused);
            }

            // Add tooltip
            string tooltip = GetTooltip(pawn, pauseTracker, isPaused);
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(buttonRect, tooltip);
            }
        } 

        private void ShowPauseMenu(Pawn pawn, Autonomy.PawnAutonomyPauseTracker pauseTracker, bool isPaused)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // Show "Unpause" first if currently paused
            if (isPaused)
            {
                options.Add(new FloatMenuOption("Keno_PauseAutonomy_Unpause".Translate(), delegate
                {
                    pauseTracker.ClearPause(pawn);
                }));
            }

            // Duration options
            options.Add(new FloatMenuOption("Keno_PauseAutonomy_Pause3Hours".Translate(), delegate
            {
                pauseTracker.SetPause(pawn, Autonomy.PawnAutonomyPauseTracker.PAUSE_3_HOURS);
            }));
 
            options.Add(new FloatMenuOption("Keno_PauseAutonomy_Pause8Hours".Translate(), delegate
            {
                pauseTracker.SetPause(pawn, Autonomy.PawnAutonomyPauseTracker.PAUSE_8_HOURS);
            }));

            options.Add(new FloatMenuOption("Keno_PauseAutonomy_Pause24Hours".Translate(), delegate
            {
                pauseTracker.SetPause(pawn, Autonomy.PawnAutonomyPauseTracker.PAUSE_24_HOURS);
            }));

            options.Add(new FloatMenuOption("Keno_PauseAutonomy_Pause3Days".Translate(), delegate
            {
                pauseTracker.SetPause(pawn, Autonomy.PawnAutonomyPauseTracker.PAUSE_3_DAYS);
            }));

            options.Add(new FloatMenuOption("Keno_PauseAutonomy_PauseForever".Translate(), delegate
            {
                pauseTracker.SetPause(pawn, Autonomy.PawnAutonomyPauseTracker.PAUSE_FOREVER);
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private string GetTooltip(Pawn pawn, Autonomy.PawnAutonomyPauseTracker pauseTracker, bool isPaused)
        {
            if (isPaused)
            {
                string remainingTime = pauseTracker.GetRemainingTimeString(pawn);
                if (!remainingTime.NullOrEmpty())
                {
                    return "Keno_PauseAutonomy_TooltipPaused".Translate(remainingTime);
                }
            }

            return "Keno_PauseAutonomy_Tooltip".Translate();
        }

        public override int GetMinCellHeight(Pawn pawn)
        {
            return Mathf.Max(base.GetMinCellHeight(pawn), Width + TopPadding);
        }

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), Width);
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        private int GetValueToCompare(Pawn pawn)
        {
            var pauseTracker = Current.Game?.GetComponent<Autonomy.PawnAutonomyPauseTracker>();
            if (pauseTracker == null)
            {
                return 0;
            }

            // Sort paused pawns first (return 1), unpaused last (return 0)
            return pauseTracker.IsPaused(pawn) ? 1 : 0;
        }
    }
}
