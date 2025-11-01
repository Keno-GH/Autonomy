using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// Standalone main tab for the Autonomy system
    /// </summary>
    public class MainTabWindow_Autonomy : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private InfoGiverWindow infoGiverWindow;

        public override Vector2 RequestedTabSize => new Vector2(800f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            Widgets.Label(titleRect, "Autonomy System Monitor");
            Text.Font = GameFont.Small;

            Rect contentRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - titleRect.height - 10f);
            
            // Draw description
            Rect descRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 60f);
            Widgets.Label(descRect, "Monitor InfoGivers and Priority systems. View current values, configurations, and relationships between different autonomous systems.");
            
            // Draw InfoGiver button
            Rect buttonRect = new Rect(contentRect.x + 20f, descRect.yMax + 20f, 200f, 35f);
            if (Widgets.ButtonText(buttonRect, "Open InfoGiver Monitor"))
            {
                OpenInfoGiverWindow();
            }
            
            // Draw quick stats
            DrawQuickStats(new Rect(contentRect.x, buttonRect.yMax + 20f, contentRect.width, contentRect.height - buttonRect.yMax - 20f));
        }

        private void DrawQuickStats(Rect rect)
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
            
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, GetQuickStatsHeight());
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            Text.Font = GameFont.Small;
            
            // Quick stats section
            float curY = 0f;
            Rect titleRect = new Rect(0f, curY, viewRect.width, 25f);
            GUI.color = Color.cyan;
            Widgets.Label(titleRect, "System Overview");
            GUI.color = Color.white;
            curY += 30f;
            
            // Count InfoGivers
            var allInfoGivers = DefDatabase<InfoGiverDef>.AllDefs.ToList();
            var urgentInfoGivers = allInfoGivers.Where(ig => ig.isUrgent).Count();
            var normalInfoGivers = allInfoGivers.Count - urgentInfoGivers;
            
            Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f), 
                $"Total InfoGivers: {allInfoGivers.Count}");
            curY += 22f;
            
            GUI.color = Color.yellow;
            Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f), 
                $"Urgent (400 tick): {urgentInfoGivers}");
            GUI.color = Color.white;
            curY += 22f;
            
            Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f), 
                $"Normal (2000 tick): {normalInfoGivers}");
            curY += 22f;
            
            // Count Priority Givers
            var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs.Count();
            Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f), 
                $"Priority Givers: {priorityGivers}");
            curY += 30f;

            // Current InfoGiver Values
            GUI.color = Color.cyan;
            Widgets.Label(new Rect(0f, curY, viewRect.width, 25f), "Current InfoGiver Values");
            GUI.color = Color.white;
            curY += 30f;

            var results = manager.GetAllResults();
            if (results.Any())
            {
                foreach (var kvp in results.OrderBy(r => r.Key))
                {
                    var infoDef = DefDatabase<InfoGiverDef>.GetNamedSilentFail(kvp.Key);
                    if (infoDef != null)
                    {
                        Color valueColor = infoDef.isUrgent ? Color.yellow : Color.white;
                        
                        Rect labelRect = new Rect(20f, curY, viewRect.width * 0.7f, 20f);
                        Rect valueRect = new Rect(viewRect.width * 0.7f, curY, viewRect.width * 0.3f, 20f);
                        
                        Widgets.Label(labelRect, infoDef.label ?? kvp.Key);
                        
                        GUI.color = valueColor;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(valueRect, kvp.Value.ToString("F2"));
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = Color.white;
                        
                        curY += 22f;
                    }
                }
            }
            else
            {
                Widgets.Label(new Rect(20f, curY, viewRect.width - 20f, 20f), "No InfoGiver results available yet");
            }

            Widgets.EndScrollView();
        }

        private float GetQuickStatsHeight()
        {
            float baseHeight = 200f; // For headers and basic stats
            
            var currentMap = Find.CurrentMap;
            if (currentMap != null)
            {
                var manager = currentMap.GetComponent<InfoGiverManager>();
                if (manager != null)
                {
                    var results = manager.GetAllResults();
                    baseHeight += results.Count * 22f; // 22f per InfoGiver result
                }
            }
            
            return Math.Max(baseHeight, 400f);
        }

        private void OpenInfoGiverWindow()
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