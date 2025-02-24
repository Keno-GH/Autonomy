using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Autonomy
{
    public class FreeWillWindow : Window
    {
        private string title;
        private string mapInfo;
        private int tickCounter = 0;
        private const int updateInterval = 1000;

        public FreeWillWindow()
        {
            this.doCloseButton = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;

            // Load the title from the language key
            title = "AutonomyWindowTitle".Translate();

            // Initialize map info
            UpdateMapInfo();
        }

        // Increase the window size
        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), title);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 40f, inRect.width, inRect.height - 40f), mapInfo);
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            tickCounter++;
            if (tickCounter >= updateInterval)
            {
                UpdateMapInfo();
                tickCounter = 0;
            }
        }

        private void UpdateMapInfo()
        {
            var map = Find.CurrentMap;
            if (map == null)
            {
                mapInfo = "AutonomyNoMap".Translate();
                return;
            }

            var priorityGivers = new List<PriorityGiver>();
            foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefs)
            {
                var extension = workTypeDef.GetModExtension<PriorityGiverExtension>();
                if (extension != null)
                {
                    priorityGivers.AddRange(extension.priorityGivers);
                }
            }

            Dictionary<string, float> info = InfoProvider.GetMapInfo(map, priorityGivers);
            List<string> translatedInfo = new List<string>();

            foreach (var kvp in info)
            {
                switch (kvp.Key)
                {
                    case "pawnCount":
                        translatedInfo.Add("AutonomyPawnCount".Translate(kvp.Value));
                        break;
                    case "colonistCount":
                        translatedInfo.Add("AutonomyColonistCount".Translate(kvp.Value));
                        break;
                    case "petCount":
                        translatedInfo.Add("AutonomyPetCount".Translate(kvp.Value));
                        break;
                    case "enemyCount":
                        translatedInfo.Add("AutonomyEnemyCount".Translate(kvp.Value));
                        break;
                    case "filthInHome":
                        translatedInfo.Add("AutonomyFilthInHome".Translate(kvp.Value));
                        break;
                    case "noMap":
                        translatedInfo.Add("AutonomyNoMap".Translate());
                        break;
                    default:
                        translatedInfo.Add($"{kvp.Key}: {kvp.Value}");
                        break;
                }
            }

            mapInfo = string.Join("\n", translatedInfo);
        }
    }
}
