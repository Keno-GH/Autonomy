using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// Harmony patch to add Autonomy tab to the History window's tab system
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_History))]
    public static class MainTabWindow_History_Patch
    {
        private static InfoGiverWindow infoGiverWindow;
        
        // Track our tab state directly instead of relying on reflection
        private static bool autonomyTabSelected = false;
    // Track the built-in tab value that was active when the Autonomy tab was selected.
    // This lets us detect if the player later switches to a built-in tab and we should
    // clear our custom selection.
    private static int previousBuiltInCurTab = int.MinValue;

        // Patch PreOpen to add our tab to the tab list
        [HarmonyPatch("PreOpen")]
        [HarmonyPostfix]
        public static void PreOpen_Postfix(MainTabWindow_History __instance)
        {
            try
            {
                autonomyTabSelected = false; // Reset our tab state when window opens
                AddAutonomyTab(__instance);
                // Move the built-in search bar down so it doesn't overlap our custom tab.
                try
                {
                    var searchBarField = typeof(MainTabWindow_History).GetField("SearchBarOffset",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    if (searchBarField != null)
                    {
                        var old = (Vector2)searchBarField.GetValue(null);
                        // Increase Y so the quick-search box sits lower (below tabs)
                        Vector2 newVal = new Vector2(old.x, 52f);
                        searchBarField.SetValue(null, newVal);
                        Log.Message($"[Autonomy] Adjusted MainTabWindow_History.SearchBarOffset to {newVal}");
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning($"[Autonomy] Failed to adjust SearchBarOffset: {e.Message}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[Autonomy] Error adding Autonomy tab: {e.Message}");
            }
        }

        // Patch DoWindowContents to detect tab changes and optionally draw our custom UI.
        [HarmonyPatch("DoWindowContents")]
        [HarmonyPrefix]
        public static bool DoWindowContents_Prefix(MainTabWindow_History __instance, Rect rect)
        {
            try
            {
                var curTabField = typeof(MainTabWindow_History).GetField("curTab",
                    BindingFlags.NonPublic | BindingFlags.Static);

                int currentTabInt = int.MinValue;
                if (curTabField != null)
                {
                    var currentTabValue = curTabField.GetValue(null);
                    currentTabInt = (currentTabValue != null) ? System.Convert.ToInt32(currentTabValue) : int.MinValue;
                }

                // If our tab is active, draw the tabs row (so tabs stay visible) and then
                // render only our Autonomy content, skipping the original content drawing.
                if (autonomyTabSelected)
                {
                    // If the player switched to a built-in tab since selecting Autonomy,
                    // clear our selection and allow the original drawing to proceed.
                    if (previousBuiltInCurTab != int.MinValue && currentTabInt != previousBuiltInCurTab)
                    {
                        autonomyTabSelected = false;
                        previousBuiltInCurTab = int.MinValue;
                        return true; // let the original method draw the newly selected built-in tab
                    }

                    // Draw the tabs row ourselves so the tabs remain visible.
                    Rect tabsRect = rect;
                    tabsRect.yMin += 45f;
                    var tabsField = typeof(MainTabWindow_History).GetField("tabs",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (tabsField?.GetValue(__instance) is List<TabRecord> tabs)
                    {
                        // Build a temporary copy of TabRecord objects so we can control
                        // which one is visually selected without mutating the originals.
                        List<TabRecord> tmp = new List<TabRecord>(tabs.Count);
                        foreach (var t in tabs)
                        {
                            // Determine what the selected state should be for the copy.
                            bool sel;
                            if (autonomyTabSelected)
                            {
                                sel = t.label == "Autonomy";
                            }
                            else
                            {
                                sel = t.Selected;
                            }

                            // Recreate the TabRecord using the bool-selected constructor so
                            // TabDrawer will use the field rather than any getter.
                            tmp.Add(new TabRecord(t.label, t.clickedAction, sel));
                        }

                        TabDrawer.DrawTabs(tabsRect, tmp);
                    }

                    // Now render our Autonomy content in the same content area the original
                    // would have used.
                    HandleAutonomyTab(__instance, rect);

                    return false; // skip the original DoWindowContents (we handled drawing)
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[Autonomy] Error in DoWindowContents_Prefix: {e.Message}");
            }

            return true; // run original DoWindowContents as normal
        }

        // Note: we handle rendering of the Autonomy content in the Prefix when our
        // tab is selected (so we can draw the tabs row then render our content). We
        // don't use a Postfix for drawing to avoid duplicating content or leaving the
        // original content visible underneath.

        private static void AddAutonomyTab(MainTabWindow_History historyWindow)
        {
            // Get the tabs list using reflection
            var tabsField = typeof(MainTabWindow_History).GetField("tabs", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (tabsField?.GetValue(historyWindow) is List<TabRecord> tabs)
            {
                // Check if our tab is already added to avoid duplicates
                bool alreadyAdded = tabs.Any(tab => tab.label == "Autonomy");
                    if (!alreadyAdded)
                    {
                        Log.Message("[Autonomy] Adding Autonomy tab to History window");
                        // Add our Autonomy tab to the existing tabs
                        tabs.Add(new TabRecord("Autonomy", delegate
                        {
                            Log.Message("[Autonomy] Autonomy tab clicked - setting autonomyTabSelected = true");
                            // Record the built-in curTab value at the moment of selection so we can
                            // detect later if the player switched to a built-in tab.
                            try
                            {
                                var curTabField = typeof(MainTabWindow_History).GetField("curTab",
                                    BindingFlags.NonPublic | BindingFlags.Static);
                                if (curTabField != null)
                                {
                                    var curVal = curTabField.GetValue(null);
                                    previousBuiltInCurTab = (curVal != null) ? System.Convert.ToInt32(curVal) : int.MinValue;
                                }
                            }
                            catch
                            {
                                previousBuiltInCurTab = int.MinValue;
                            }

                            autonomyTabSelected = true;
                            // Don't try to manipulate the original tab system at all
                        }, () => autonomyTabSelected));
                    }
            }
            else
            {
                Log.Warning("[Autonomy] Could not find tabs field in MainTabWindow_History");
            }
        }

        private static void HandleAutonomyTab(MainTabWindow_History historyWindow, Rect rect)
        {
            // Check if our Autonomy tab is selected
            if (autonomyTabSelected)
            {
                Log.Message("[Autonomy] Rendering Autonomy tab content");
                // Create content area like other tabs do
                Rect contentRect = rect;
                contentRect.yMin += 45f; // Same offset as other tabs
                contentRect.yMin += 17f; // Additional offset like Statistics tab
                
                DoAutonomyTabContent(contentRect);
            }
        }

        private static void DoAutonomyTabContent(Rect rect)
        {
            Log.Message($"[Autonomy] DoAutonomyTabContent called with rect: {rect}");
            Widgets.BeginGroup(rect);

            // Use margin-based layout so elements adapt to different window sizes
            float margin = 12f;
            float innerWidth = rect.width - (margin * 2f);
            float y = margin;

            // Remove the window title and description (we're embedding into History window)
            // Place the action button at the bottom-left of the content area.
            float buttonHeight = 36f;
            Rect buttonRect = new Rect(margin, rect.height - margin - buttonHeight, Mathf.Min(220f, innerWidth * 0.6f), buttonHeight);
            if (Widgets.ButtonText(buttonRect, "Open InfoGiver InfoCards"))
            {
                OpenInfoGiverWindow();
            }

            // Draw quick stats area using the space above the bottom button
            Rect statsRect = new Rect(margin, y, innerWidth, (rect.height - margin - buttonHeight) - y);
            DrawQuickStats(statsRect);

            // Reset GUI state
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            Widgets.EndGroup();
        }

        private static void DrawQuickStats(Rect rect)
        {
            var currentMap = Find.CurrentMap;
            if (currentMap == null)
            {
                Widgets.Label(rect, "No active map selected");
                return;
            }

            var manager = currentMap.GetComponent<InfoGiverManager>();
            if (manager == null)
            {
                Widgets.Label(rect, "InfoGiverManager not found on current map");
                return;
            }

            Text.Font = GameFont.Small;

            float margin = 6f;
            float x = rect.x + margin;
            float y = rect.y + margin;
            float innerWidth = rect.width - margin * 2f;

            // Section header
            GUI.color = Color.cyan;
            Widgets.Label(new Rect(x, y, innerWidth, 22f), "System Overview");
            GUI.color = Color.white;
            y += 24f;

            // Counts
            var allInfoGivers = DefDatabase<InfoGiverDef>.AllDefs.ToList();
            var urgentInfoGivers = allInfoGivers.Where(ig => ig.isUrgent).Count();
            var normalInfoGivers = allInfoGivers.Count - urgentInfoGivers;

            Widgets.Label(new Rect(x, y, innerWidth, 20f), $"Total InfoGivers: {allInfoGivers.Count}");
            y += 20f;

            GUI.color = Color.yellow;
            Widgets.Label(new Rect(x, y, innerWidth, 20f), $"Urgent (400 tick): {urgentInfoGivers}");
            GUI.color = Color.white;
            y += 20f;

            Widgets.Label(new Rect(x, y, innerWidth, 20f), $"Normal (2000 tick): {normalInfoGivers}");
            y += 24f;

            var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs.Count();
            Widgets.Label(new Rect(x, y, innerWidth, 20f), $"Priority Givers: {priorityGivers}");
            y += 28f;

            // Recent values
            GUI.color = Color.cyan;
            Widgets.Label(new Rect(x, y, innerWidth, 22f), "Recent InfoGiver Values");
            GUI.color = Color.white;
            y += 26f;

            var results = manager.GetAllResults();
            if (results.Any())
            {
                float leftColX = x;
                float rightColX = x + innerWidth * 0.5f + 8f;
                float colWidth = innerWidth * 0.5f - 8f;
                float leftY = y;
                float rightY = y;
                int i = 0;
                foreach (var kvp in results.OrderBy(r => r.Key))
                {
                    var infoDef = DefDatabase<InfoGiverDef>.GetNamedSilentFail(kvp.Key);
                    if (infoDef == null) continue;

                    bool left = (i % 2 == 0);
                    float drawY = left ? leftY : rightY;
                    float labelW = colWidth * 0.65f;
                    float valueW = colWidth - labelW;

                    Rect labelRect = new Rect(left ? leftColX : rightColX, drawY, labelW, 20f);
                    Rect valueRect = new Rect((left ? leftColX : rightColX) + labelW, drawY, valueW, 20f);
                    Widgets.Label(labelRect, infoDef.label ?? kvp.Key);

                    GUI.color = infoDef.isUrgent ? Color.yellow : Color.white;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(valueRect, kvp.Value.ToString("F2"));
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;

                    if (left) leftY += 22f; else rightY += 22f;
                    i++;
                }
            }
            else
            {
                Widgets.Label(new Rect(x, y, innerWidth, 20f), "No InfoGiver results available yet");
            }
        }

        private static void OpenInfoGiverWindow()
        {
            if (infoGiverWindow == null || infoGiverWindow.IsOpen == false)
            {
                infoGiverWindow = new InfoGiverWindow();
                Find.WindowStack.Add(infoGiverWindow);
            }
            else
            {
                infoGiverWindow.Close();
            }
        }
    }
}