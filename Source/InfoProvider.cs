using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    public static class InfoProvider
    {
        public static Dictionary<string, float> GetMapInfo(Map map, List<PriorityGiver> priorityGivers)
        {
            Dictionary<string, float> mapInfo = new Dictionary<string, float>();
            if (map == null)
            {
                mapInfo["noMap"] = 0;
            }
            else
            {
                int pawnCount = map.mapPawns.AllPawns.Count;
                int colonistCount = map.mapPawns.FreeColonists.Count;
                int petCount = map.mapPawns.SpawnedColonyAnimals.Count;
                int enemyCount = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer)).Count();
                int filthInHome = map.listerFilthInHomeArea.FilthInHomeArea.Count;
                int thingsDeteriorating = map.listerHaulables.ThingsPotentiallyNeedingHauling().Count(t => t.def.useHitPoints && t.IsOutside() && !t.IsForbidden(Faction.OfPlayer) && t.def.CanEverDeteriorate);
                int refuelableThingsNeedingRefuel = map.listerThings.ThingsInGroup(ThingRequestGroup.Refuelable).Count(t => t.TryGetComp<CompRefuelable>() is CompRefuelable compRefuelable && compRefuelable.ShouldAutoRefuelNowIgnoringFuelPct && compRefuelable.Fuel < compRefuelable.Props.fuelCapacity * 0.25f);
                int colonistsNeedingTending = map.mapPawns.FreeColonists.Count(p => p.health.hediffSet.HasTendableHediff());
                float colonistsBloodLoss = map.mapPawns.FreeColonists.Sum(p => p.health.hediffSet.BleedRateTotal);
                int animalsNeedingTending = map.mapPawns.SpawnedColonyAnimals.Count(p => p.health.hediffSet.HasTendableHediff());
                float animalsBloodLoss = map.mapPawns.SpawnedColonyAnimals.Sum(p => p.health.hediffSet.BleedRateTotal);
                int corpsesNeedingBurial = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Count(t => !t.IsForbidden(Faction.OfPlayer) && (t.GetRotStage() == RotStage.Fresh || t.GetRotStage() == RotStage.Rotting || t.GetRotStage() == RotStage.Dessicated) && t.def.race.Humanlike);
                float colonistsFoodLevelAverage = map.mapPawns.FreeColonists.Average(p => p.needs.food.CurLevelPercentage);

                mapInfo["pawnCount"] = pawnCount;
                mapInfo["colonistCount"] = colonistCount;
                mapInfo["petCount"] = petCount;
                mapInfo["enemyCount"] = enemyCount;
                mapInfo["filthInHome"] = filthInHome;
                mapInfo["thingsDeteriorating"] = thingsDeteriorating;
                mapInfo["refuelableThingsNeedingRefuel"] = refuelableThingsNeedingRefuel;
                mapInfo["colonistsNeedingTending"] = colonistsNeedingTending;
                mapInfo["colonistsBloodLoss"] = colonistsBloodLoss;
                mapInfo["animalsNeedingTending"] = animalsNeedingTending;
                mapInfo["animalsBloodLoss"] = animalsBloodLoss;
                mapInfo["corpsesNeedingBurial"] = corpsesNeedingBurial;
                mapInfo["colonistsFoodLevelAverage"] = colonistsFoodLevelAverage;

                var statDefsToCheck = priorityGivers
                    .Where(g => !string.IsNullOrEmpty(g.stat))
                    .Select(g => StatDef.Named(g.stat))
                    .Distinct();

                foreach (StatDef statDef in statDefsToCheck)
                {
                    float sum = 0f;
                    int count = 0;
                    Pawn bestColonist = null;
                    float maxStatValue = float.MinValue;

                    foreach (Pawn pawn in map.mapPawns.FreeColonists)
                    {
                        float statValue = pawn.GetStatValue(statDef, false);
                        if (statValue <= 0f)
                            continue;

                        if (pawn.InBed())
                        {
                            continue; // Skip colonists who are in bed to account for injuries and varying schedules
                        }

                        sum += statValue;
                        count++;

                        if (statValue > maxStatValue)
                        {
                            maxStatValue = statValue;
                            bestColonist = pawn;
                        }
                    }
                    mapInfo[$"bestAt_{statDef.defName}"] = maxStatValue;
                    mapInfo[$"average_{statDef.defName}"] = count > 0 ? sum / count : 0;
                }
            }
            return mapInfo;
        }

        public static Dictionary<string, float> GetPawnInfo(Pawn pawn)
        {
            Dictionary<string, float> pawnInfo = new Dictionary<string, float>();
            if (pawn == null)
            {
                pawnInfo["noPawn"] = 0;
            }
            else
            {
                float cleanliness = CleanlinessUtility.CalculateCleanliness(pawn);
                pawnInfo["cleanlinessSurroundingMe"] = cleanliness;

                List<TraitDef> traitsThatNullifyObservedLayingCorpse = ThoughtDef.Named("ObservedLayingCorpse").nullifyingTraits;
                List<HediffDef> hediffsThatNullifyObservedLayingCorpse = ThoughtDef.Named("ObservedLayingCorpse").nullifyingHediffs;
                List<PreceptDef> preceptsThatNullifyObservedLayingCorpse = ThoughtDef.Named("ObservedLayingCorpse").nullifyingPrecepts;

                bool caresAboutDeaths = true;

                // Check traits
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (traitsThatNullifyObservedLayingCorpse.Contains(trait.def))
                    {
                        caresAboutDeaths = false;
                        break;
                    }
                }

                // Check hediffs if Anomaly DLC is enabled
                if (caresAboutDeaths && ModsConfig.AnomalyActive)
                {
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediffsThatNullifyObservedLayingCorpse.Contains(hediff.def))
                        {
                            caresAboutDeaths = false;
                            break;
                        }
                    }
                }

                // Check precepts if Ideology DLC is enabled
                if (caresAboutDeaths && ModsConfig.IdeologyActive)
                {
                    foreach (Precept precept in pawn.Ideo?.PreceptsListForReading)
                    {
                        if (preceptsThatNullifyObservedLayingCorpse.Contains(precept.def))
                        {
                            caresAboutDeaths = false;
                            break;
                        }
                    }
                }

                pawnInfo["ignoresDeath"] = caresAboutDeaths ? 0 : 1;

                int injuriesCount = pawn.health.hediffSet.GetHediffsTendable().Count();
                float bleedingRate = pawn.health.hediffSet.BleedRateTotal;
                bool needsTending = pawn.health.hediffSet.HasTendableHediff();
                var immunizableHeddifs = pawn.health.hediffSet.hediffs
                    .Select(h => h as HediffWithComps)
                    .Where(h => h != null && h.comps.Any(c => c is HediffComp_Immunizable));
                float immunityGainSpeed = immunizableHeddifs.Sum(h => pawn.health.immunity.GetImmunityRecord(h.def).ImmunityChangePerTick(pawn, true, h) * 60000f);
                float severityGainSpeed = pawn.health.hediffSet
                    .hediffs
                    .Select(h => h as HediffWithComps)
                    .Where(h => h != null && h.comps.Any(c => c is HediffComp_Immunizable))
                    .Select(h => h.TryGetComp<HediffComp_Immunizable>())
                    .Where(c => c != null)
                    .Sum(c => c.Props.severityPerDayNotImmune);
                float severityTendedSpeed = pawn.health.hediffSet
                    .hediffs
                    .Select(h => h as HediffWithComps)
                    .Where(h => h != null && h.comps.Any(c => c is HediffComp_TendDuration))
                    .Select(h => h.TryGetComp<HediffComp_TendDuration>())
                    .Where(c => c != null)
                    .Sum(c => c.TProps.severityPerDayTended * c.tendQuality);
                
                pawnInfo["injuriesCount"] = injuriesCount;
                pawnInfo["bleedingRate"] = bleedingRate;
                pawnInfo["needsTending"] = needsTending ? 1 : 0;
                pawnInfo["immunityGainSpeed"] = immunityGainSpeed;
                pawnInfo["severityGainSpeed"] = severityGainSpeed;
                pawnInfo["severityTendedSpeed"] = severityTendedSpeed;

                float foodLevel = pawn.needs.food.CurLevelPercentage;
                pawnInfo["foodLevel"] = foodLevel;

                // Log.Message($"Pawn {pawn.Name} has {injuriesCount} injuries, bleeding rate: {bleedingRate}, needs tending: {needsTending}, immunity gain speed: {immunityGainSpeed}, severity gain speed: {severityGainSpeed}, severity tended speed: {severityTendedSpeed}, true severity gained: {severityGainSpeed + severityTendedSpeed}, immunity rate - true severity gained: {immunityGainSpeed - (severityGainSpeed + severityTendedSpeed)}. Pawn Immunity Stat Value: {pawn.GetStatValue(StatDefOf.ImmunityGainSpeed, applyPostProcess: true)}");

                foreach (var skill in pawn.skills.skills)
                {
                    pawnInfo[skill.def.defName] = skill.Level;
                    pawnInfo[$"{skill.def.defName}_passion"] = (float)skill.passion;
                }
            }
            return pawnInfo;
        }
    }
}
