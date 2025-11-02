using HarmonyLib;
using RimWorld;
using Verse;

namespace Autonomy
{
    /// <summary>
    /// Ensures Manual Priorities is enabled by default for proper autonomous priority management
    /// Allows 5 priority levels (1,2,3,4,disabled) instead of just (enabled,disabled)
    /// </summary>
    [HarmonyPatch]
    public static class ManualPrioritiesPatch
    {
        /// <summary>
        /// Force manual priorities to be enabled when a new game starts
        /// </summary>
        [HarmonyPatch(typeof(GameInitData), "PrepForMapGen")]
        [HarmonyPostfix]
        public static void PrepForMapGen_Postfix()
        {
            Current.Game.playSettings.useWorkPriorities = true;
            Log.Message("[Autonomy] Manual Priorities enabled for autonomous work management");
        }

        /// <summary>
        /// Also ensure it's enabled when loading existing games
        /// </summary>
        [HarmonyPatch(typeof(Game), "LoadGame")]
        [HarmonyPostfix]
        public static void LoadGame_Postfix()
        {
            if (Current.Game?.playSettings != null)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                Log.Message("[Autonomy] Manual Priorities ensured enabled on game load");
            }
        }
    }
}