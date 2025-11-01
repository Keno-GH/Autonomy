using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Autonomy
{
    /// <summary>
    /// InfoCard-style window for viewing InfoGiver values and details
    /// </summary>
    public class InfoGiverWindow : Window
    {
        private Vector2 scrollPositionLeft = Vector2.zero;
        private Vector2 scrollPositionRight = Vector2.zero;
        private InfoGiverDef selectedInfoGiver = null;
        private Dictionary<string, float> currentResults;
        private InfoGiverManager manager;

        private const float WINDOW_WIDTH = 800f;
        private const float WINDOW_HEIGHT = 600f;
        private const float LEFT_COLUMN_WIDTH = 350f;
        private const float RIGHT_COLUMN_WIDTH = 400f;
        private const float COLUMN_SPACING = 10f;
    // Make rows compact like vanilla info cards but with extra padding for readability
    private const float ROW_HEIGHT = 30f;
        private const float SECTION_SPACING = 20f;

        public override Vector2 InitialSize => new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

        public InfoGiverWindow()
        {
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            forcePause = false;
            absorbInputAroundWindow = false;
            
            // Get current map's InfoGiverManager
            var currentMap = Find.CurrentMap;
            if (currentMap != null)
            {
                manager = currentMap.GetComponent<InfoGiverManager>();
                if (manager != null)
                {
                    currentResults = manager.GetAllResults();
                }
            }
            
            if (currentResults == null)
            {
                currentResults = new Dictionary<string, float>();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Reserve some space at the bottom so the window close controls don't overlap content
            float bottomReserve = 55f;
            Rect leftColumn = new Rect(0f, 0f, LEFT_COLUMN_WIDTH, inRect.height - bottomReserve);
            Rect rightColumn = new Rect(LEFT_COLUMN_WIDTH + COLUMN_SPACING, 0f, RIGHT_COLUMN_WIDTH, inRect.height - bottomReserve);

            // Draw a subtle separator line between columns (darker than white)
            Color prevColor = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f);
            Widgets.DrawLineVertical(LEFT_COLUMN_WIDTH + COLUMN_SPACING / 2f, 0f, inRect.height - bottomReserve);
            GUI.color = prevColor;

            DoLeftColumn(leftColumn);
            DoRightColumn(rightColumn);
        }

        private void DoLeftColumn(Rect rect)
        {
            // Bigger title area with short description
            float titleHeight = 36f;
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, titleHeight);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "InfoGivers");
            Text.Font = GameFont.Small;

            float descHeight = Text.CalcHeight("InfoGivers provide measured values used by priority systems and AI.", rect.width - 10f);
            Rect descRect = new Rect(rect.x + 4f, titleRect.yMax + 4f, rect.width - 8f, descHeight);
            Widgets.Label(descRect, "InfoGivers provide measured values used by priority systems and AI.");

            Rect listRect = new Rect(rect.x, descRect.yMax + 6f, rect.width, rect.height - titleHeight - descHeight - 10f);
            // leave extra width for scrollbar
            Rect viewRect = new Rect(0f, 0f, listRect.width - 22f, GetInfoGiverListHeight());

            Widgets.BeginScrollView(listRect, ref scrollPositionLeft, viewRect);

            float curY = 0f;
            var infoGivers = DefDatabase<InfoGiverDef>.AllDefs.OrderBy(ig => ig.label).ToList();

            foreach (var infoGiver in infoGivers)
            {
                Rect rowRect = new Rect(0f, curY, viewRect.width, ROW_HEIGHT);
                
                // Highlight selected row
                if (selectedInfoGiver == infoGiver)
                {
                    Widgets.DrawHighlight(rowRect);
                }

                // Get current value
                float currentValue = currentResults.TryGetValue(infoGiver.defName, out float value) ? value : 0f;
                
                // Draw label and value
                // Keep label away from scrollbar by using viewRect width and fixed padding
                float labelW = viewRect.width * 0.68f;
                Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y, labelW - 6f, rowRect.height);
                Rect valueRect = new Rect(rowRect.x + labelW, rowRect.y, viewRect.width - labelW, rowRect.height);

                // Color code urgent InfoGivers
                Color originalColor = GUI.color;
                if (infoGiver.isUrgent)
                {
                    GUI.color = Color.yellow;
                }

                // Vertically center labels for better readability
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, infoGiver.label);
                GUI.color = originalColor;

                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(valueRect, currentValue.ToString("F2"));
                Text.Anchor = TextAnchor.UpperLeft;

                // Handle click
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedInfoGiver = infoGiver;
                    scrollPositionRight = Vector2.zero; // Reset right scroll when switching
                }

                curY += ROW_HEIGHT;
            }

            Widgets.EndScrollView();
        }

        private void DoRightColumn(Rect rect)
        {
            if (selectedInfoGiver == null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "Select an InfoGiver to view details");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                return;
            }

            float titleHeight = 40f;
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, titleHeight);
            Text.Font = GameFont.Medium;

            // Color code urgent InfoGivers in title too
            Color originalColor = GUI.color;
            if (selectedInfoGiver.isUrgent)
            {
                GUI.color = Color.yellow;
            }

            Widgets.Label(titleRect, selectedInfoGiver.label);
            GUI.color = originalColor;
            Text.Font = GameFont.Small;

            // Small description beneath the title if available, otherwise a hint
            float titleDescY = titleRect.yMax + 4f;
            string shortDesc = !selectedInfoGiver.description.NullOrEmpty() ? selectedInfoGiver.description : "Small details and configuration for the selected InfoGiver.";
            float titleDescHeight = Text.CalcHeight(shortDesc, rect.width - 10f);
            Rect titleDescRect = new Rect(rect.x + 4f, titleDescY, rect.width - 8f, titleDescHeight);
            Widgets.Label(titleDescRect, shortDesc);

            Rect contentRect = new Rect(rect.x, titleDescRect.yMax + 6f, rect.width, rect.height - titleDescRect.yMax - rect.y - 6f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 18f, GetDetailsHeight());

            Widgets.BeginScrollView(contentRect, ref scrollPositionRight, viewRect);

            float curY = 0f;
            curY = DrawInfoGiverDetails(viewRect, curY);

            Widgets.EndScrollView();
        }

        private float DrawInfoGiverDetails(Rect viewRect, float startY)
        {

            float curY = startY;

            // Current Value
            curY = DrawDetailSection(viewRect, curY, "Current Value", () =>
            {
                float currentValue = currentResults.TryGetValue(selectedInfoGiver.defName, out float value) ? value : 0f;
                return new List<string> { currentValue.ToString("F2") };
            });

            // Basic Information
            curY = DrawDetailSection(viewRect, curY, "Basic Information", () =>
            {
                var info = new List<string>();
                info.Add($"Source Type: {selectedInfoGiver.sourceType}");
                info.Add($"Calculation: {selectedInfoGiver.calculation}");
                info.Add($"Update Frequency: {(selectedInfoGiver.isUrgent ? "Urgent (400 ticks)" : "Normal (2000 ticks)")}");
                return info;
            });

            // Description
            if (!selectedInfoGiver.description.NullOrEmpty())
            {
                curY = DrawDetailSection(viewRect, curY, "Description", () =>
                {
                    return new List<string> { selectedInfoGiver.description };
                });
            }

            // Configuration Details
            curY = DrawDetailSection(viewRect, curY, "Configuration", () =>
            {
                var config = new List<string>();
                
                if (!selectedInfoGiver.targetStat.NullOrEmpty())
                    config.Add($"Target Stat: {selectedInfoGiver.targetStat}");
                if (!selectedInfoGiver.targetNeed.NullOrEmpty())
                    config.Add($"Target Need: {selectedInfoGiver.targetNeed}");
                if (!selectedInfoGiver.weatherProperty.NullOrEmpty())
                    config.Add($"Weather Property: {selectedInfoGiver.weatherProperty}");
                if (selectedInfoGiver.targetItems.Any())
                    config.Add($"Target Items: {string.Join(", ", selectedInfoGiver.targetItems)}");
                if (selectedInfoGiver.targetCategories.Any())
                    config.Add($"Target Categories: {string.Join(", ", selectedInfoGiver.targetCategories)}");
                if (selectedInfoGiver.conditions.Any())
                    config.Add($"Conditions: {selectedInfoGiver.conditions.Count} configured");
                
                if (!config.Any())
                    config.Add("No special configuration");
                
                return config;
            });

            // Priority Givers that use this InfoGiver
            curY = DrawDetailSection(viewRect, curY, "Used By Priority Givers", () =>
            {
                var callers = new List<string>();
                var priorityGivers = DefDatabase<PriorityGiverDef>.AllDefs;
                
                foreach (var pg in priorityGivers)
                {
                    foreach (var condition in pg.conditions)
                    {
                        if (condition.infoDefName == selectedInfoGiver.defName)
                        {
                            callers.Add(pg.label ?? pg.defName);
                            break;
                        }
                    }
                }
                
                if (!callers.Any())
                    callers.Add("Not used by any Priority Givers");
                
                return callers;
            });

            return curY;
        }

        private float DrawDetailSection(Rect viewRect, float startY, string title, Func<List<string>> getContent)
        {
            float curY = startY;

            // Section title
            Text.Font = GameFont.Small;
            GUI.color = Color.cyan;
            Widgets.Label(new Rect(0f, curY, viewRect.width, ROW_HEIGHT), title);
            GUI.color = Color.white;
            curY += ROW_HEIGHT;

            // Section content
            var content = getContent();
            foreach (var line in content)
            {
                float lineHeight = Text.CalcHeight(line, viewRect.width - 14f);
                Widgets.Label(new Rect(10f, curY, viewRect.width - 14f, lineHeight), line);
                curY += lineHeight + 4f;
            }

            curY += SECTION_SPACING;
            return curY;
        }

        private float GetInfoGiverListHeight()
        {
            return DefDatabase<InfoGiverDef>.AllDefs.Count() * ROW_HEIGHT;
        }

        private float GetDetailsHeight()
        {
            if (selectedInfoGiver == null) return 100f;
            
            // Estimate height needed for all sections
            float estimatedHeight = 0f;
            estimatedHeight += ROW_HEIGHT * 10; // Basic sections
            estimatedHeight += SECTION_SPACING * 6; // Section spacing
            
            // Add height for description if present
            if (!selectedInfoGiver.description.NullOrEmpty())
            {
                estimatedHeight += Text.CalcHeight(selectedInfoGiver.description, RIGHT_COLUMN_WIDTH - 26f);
            }
            
            return Math.Max(estimatedHeight, 400f);
        }
    }
}