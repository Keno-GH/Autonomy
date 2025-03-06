using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Autonomy
{
    public static class InfoProvider
    {
        public static Dictionary<string, float> GetMapInfo(Map map, List<PriorityGiver> priorityGivers, List<Pawn> workingColonists)
        {
            Dictionary<string, float> mapInfo = new Dictionary<string, float>();
            if (map == null)
            {
                mapInfo["noMap"] = 0;
            }
            else
            {
                // Basic information
                int pawnCount = map.mapPawns.AllPawns.Count;
                int colonistCount = map.mapPawns.FreeColonists.Count;
                int petCount = map.mapPawns.SpawnedColonyAnimals.Count;
                int enemyCount = map.mapPawns.AllHumanlikeSpawned.Where(p => p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer)).Count();
                int firesInHomeArea = map.listerThings.ThingsInGroup(ThingRequestGroup.Fire).Count;

                mapInfo["pawnCount"] = pawnCount;
                mapInfo["colonistCount"] = colonistCount;
                mapInfo["petCount"] = petCount;
                mapInfo["enemyCount"] = enemyCount;
                mapInfo["firesInHomeArea"] = firesInHomeArea;

                // Things information
                var filthList = map.listerFilthInHomeArea.FilthInHomeArea;
                var haulableThings = map.listerHaulables.ThingsPotentiallyNeedingHauling();
                var refuelableThingsList = map.listerThings.ThingsInGroup(ThingRequestGroup.Refuelable);
                var corpseThingsList = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                var allCountedAmounts = map.resourceCounter.AllCountedAmounts;

                int filthInHome = filthList.Count;

                int thingsDeteriorating = haulableThings.Count(t =>
                {
                    bool isDeteriorating = t.def.useHitPoints &&
                                        !t.IsForbidden(Faction.OfPlayer) &&
                                        t.def.CanEverDeteriorate &&
                                        t.GetStatValue(StatDefOf.DeteriorationRate, true) > 0f;
                    return isDeteriorating;
                });

                int refuelableThingsNeedingRefuel = refuelableThingsList.Count(t =>
                    t.TryGetComp<CompRefuelable>() is CompRefuelable compRefuelable &&
                    compRefuelable.ShouldAutoRefuelNowIgnoringFuelPct &&
                    compRefuelable.Fuel < compRefuelable.Props.fuelCapacity * 0.25f);

                int corpsesNeedingBurial = corpseThingsList.Count(t =>
                    !t.IsForbidden(Faction.OfPlayer) &&
                    (t.GetRotStage() == RotStage.Fresh || t.GetRotStage() == RotStage.Rotting || t.GetRotStage() == RotStage.Dessicated) &&
                    t.def.race.Humanlike);

                int vegetablesInHome = map.resourceCounter.GetCountIn(ThingCategoryDefOf.PlantFoodRaw);
                int meatInHome = map.resourceCounter.GetCountIn(ThingCategoryDefOf.MeatRaw);
                int medicineCount = map.resourceCounter.GetCountIn(ThingCategoryDefOf.Medicine);

                int vegetablesInHomePerColonist = colonistCount > 0 ? vegetablesInHome / colonistCount : 0;
                int meatInHomePerColonist = colonistCount > 0 ? meatInHome / colonistCount : 0;

                int socialDrugsInHome = allCountedAmounts
                    .Where(kvp => kvp.Key.IsIngestible && kvp.Key.ingestible.drugCategory == DrugCategory.Social)
                    .Sum(kvp => kvp.Value);

                int preparedMealsInHome = allCountedAmounts
                    .Where(kvp => kvp.Key.IsIngestible &&
                                (kvp.Key.ingestible.preferability == FoodPreferability.MealAwful ||
                                kvp.Key.ingestible.preferability == FoodPreferability.MealSimple ||
                                kvp.Key.ingestible.preferability == FoodPreferability.MealFine ||
                                kvp.Key.ingestible.preferability == FoodPreferability.MealLavish))
                    .Sum(kvp => kvp.Value);

                int preparedMealsInHomePerColonist = colonistCount > 0 ? preparedMealsInHome / colonistCount : 0;
                int medicineInHomePerColonist = colonistCount > 0 ? medicineCount / colonistCount : 0;

                mapInfo["filthInHome"] = filthInHome;
                mapInfo["thingsDeteriorating"] = thingsDeteriorating;
                mapInfo["refuelableThingsNeedingRefuel"] = refuelableThingsNeedingRefuel;
                mapInfo["corpsesNeedingBurial"] = corpsesNeedingBurial;
                mapInfo["vegetablesInHome"] = vegetablesInHome;
                mapInfo["vegetablesInHomePerColonist"] = vegetablesInHomePerColonist;
                mapInfo["meatInHome"] = meatInHome;
                mapInfo["meatInHomePerColonist"] = meatInHomePerColonist;
                mapInfo["socialDrugsInHome"] = socialDrugsInHome;
                mapInfo["preparedMealsInHomePerColonist"] = preparedMealsInHomePerColonist;
                mapInfo["medicineInHomePerColonist"] = medicineInHomePerColonist;

                // Health information
                var freeColonists = map.mapPawns.FreeColonists.ToList();
                var spawnedColonyAnimals = map.mapPawns.SpawnedColonyAnimals.ToList();

                int colonistsNeedingTending = 0;
                float colonistsBloodLoss = 0f;
                int colonistsNeedRescuing = 0;

                foreach (var colonist in freeColonists)
                {
                    if (colonist.health.hediffSet.HasTendableHediff())
                        colonistsNeedingTending++;
                    
                    colonistsBloodLoss += colonist.health.hediffSet.BleedRateTotal;
                    
                    if (colonist.CurJob != null && colonist.CurJob.def == JobDefOf.Wait_Downed)
                        colonistsNeedRescuing++;
                }

                int animalsNeedingTending = 0;
                float animalsBloodLoss = 0f;

                foreach (var animal in spawnedColonyAnimals)
                {
                    if (animal.health.hediffSet.HasTendableHediff())
                        animalsNeedingTending++;
                    
                    animalsBloodLoss += animal.health.hediffSet.BleedRateTotal;
                }

                mapInfo["colonistsNeedingTending"] = colonistsNeedingTending;
                mapInfo["colonistsBloodLoss"] = colonistsBloodLoss;
                mapInfo["animalsNeedingTending"] = animalsNeedingTending;
                mapInfo["animalsBloodLoss"] = animalsBloodLoss;
                mapInfo["colonistsNeedRescuing"] = colonistsNeedRescuing;

                // Needs information
                float colonistsFoodLevelAverage = map.mapPawns.FreeColonists.Average(p => p.needs.food.CurLevelPercentage);
                float colonistsChemicalNeedLevelAverage = map.mapPawns.FreeColonists
                    .Where(p => p.story.traits.GetTrait(TraitDefOf.DrugDesire) != null)
                    .Select(p => p.needs.TryGetNeed<Need_Chemical_Any>().CurLevelPercentage)
                    .DefaultIfEmpty(1f)
                    .Average();
                var colonistRecoveringInBed = map.mapPawns.FreeColonists.Where(p => p.CurJob.def == JobDefOf.LayDown && p.CurJob.jobGiver.Isnt<JobGiver_GetRest>()).ToList();
                
                float colonistsFoodLevelTotal = 0f;
                float colonistsChemicalNeedLevelTotal = 0f;
                foreach (var colonist in colonistRecoveringInBed)
                {
                    if (colonist.needs.food != null)
                        colonistsFoodLevelTotal += colonist.needs.food.CurLevelPercentage;
                    if (colonist.needs.joy != null)
                        colonistsChemicalNeedLevelTotal += colonist.needs.joy.CurLevelPercentage;
                }
                float patientsAverageFoodLevel = colonistRecoveringInBed.Any() ? colonistsFoodLevelTotal / colonistRecoveringInBed.Count : 1f;
                float patientsAverageRecreation = colonistRecoveringInBed.Any() ? colonistsChemicalNeedLevelTotal / colonistRecoveringInBed.Count : 1f;

                mapInfo["colonistsFoodLevelAverage"] = colonistsFoodLevelAverage;
                mapInfo["colonistsChemicalNeedLevelAverage"] = colonistsChemicalNeedLevelAverage;
                mapInfo["patientsAverageFoodLevel"] = patientsAverageFoodLevel;
                mapInfo["patientsAverageRecreation"] = patientsAverageRecreation;

                // Children information
                float childrenFoodLevelAverage = 1f;
                int childrenWantingTeacher = 0;
                int childrenInColony = 0;
                var children = map.mapPawns.FreeColonists.Where(p => p.ageTracker.CurLifeStage.defName == "HumanlikeChild" || p.ageTracker.CurLifeStage.defName == "HumanlikePreTeenager" ).ToList();
                var babies = map.mapPawns.FreeColonists.Where(p => p.ageTracker.CurLifeStage.defName == "HumanlikeBaby").ToList();
                if (children.Any())
                {
                    childrenWantingTeacher = children.Count(p => p.learning.ActiveLearningDesires.Any(d => d.defName == "Lessontaking"));
                    childrenInColony = children.Count;
                }
                if (babies.Any())
                {
                    childrenFoodLevelAverage = babies.Average(p => p.needs.food.CurLevelPercentage);
                    childrenInColony += babies.Count;
                }

                mapInfo["childrenFoodLevelAverage"] = childrenFoodLevelAverage;
                mapInfo["childrenWantingTeacher"] = childrenWantingTeacher;
                mapInfo["childrenInColony"] = childrenInColony;

                var statDefsToCheck = priorityGivers
                    .Where(g => !string.IsNullOrEmpty(g.stat))
                    .Select(g => 
                    {
                        var statDef = DefDatabase<StatDef>.GetNamed(g.stat, errorOnFail: false);
                        if (statDef == null)
                        {
                            throw new KeyNotFoundException($"StatDef named '{g.stat}' not found.");
                        }
                        return statDef;
                    })
                    .Distinct();

                foreach (StatDef statDef in statDefsToCheck)
                {

                    float sum = 0f;
                    int count = 0;
                    Pawn bestColonist = null;
                    float maxStatValue = float.MinValue;
                    float statValue = 0f;

                    foreach (Pawn pawn in workingColonists)
                    {
                        try 
                        {
                            statValue = pawn.GetStatValue(statDef, true);
                        }
                        catch
                        {
                            Log.Warning($"Pawn {pawn.Name} does not have stat {statDef.defName}.");
                            continue;
                        }

                        if (pawn.InBed())
                            continue; // Skip colonists who are in bed to account for injuries and varying schedules

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
                    .Where(h => h != null && h.comps.Any(c => c is HediffComp_Immunizable && pawn.health.immunity.GetImmunityRecord(h.def)?.ImmunityChangePerTick(pawn, true, h) > 0));
                float immunityGainSpeed = immunizableHeddifs
                    .Sum(h => pawn.health.immunity.GetImmunityRecord(h.def).ImmunityChangePerTick(pawn, true, h) * 60000f);
                float severityGainSpeed = immunizableHeddifs
                    .Where(h => h != null && h.comps.Any(c => c is HediffComp_Immunizable))
                    .Select(h => h.TryGetComp<HediffComp_Immunizable>())
                    .Where(c => c != null)
                    .Sum(c => c.Props.severityPerDayNotImmune);
                float severityTendedSpeed = immunizableHeddifs
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

                float chemicalLevel = pawn.story.traits.HasTrait(TraitDefOf.DrugDesire) ? pawn.needs.TryGetNeed<Need_Chemical_Any>().CurLevelPercentage : 1f;
                pawnInfo["chemicalLevel"] = chemicalLevel;

                // Log.Message($"Pawn {pawn.Name} has {injuriesCount} injuries, bleeding rate: {bleedingRate}, needs tending: {needsTending}, immunity gain speed: {immunityGainSpeed}, severity gain speed: {severityGainSpeed}, severity tended speed: {severityTendedSpeed}, true severity gained: {severityGainSpeed + severityTendedSpeed}, immunity rate - true severity gained: {immunityGainSpeed - (severityGainSpeed + severityTendedSpeed)}. Pawn Immunity Stat Value: {pawn.GetStatValue(StatDefOf.ImmunityGainSpeed, applyPostProcess: true)}");

                foreach (var skill in pawn.skills.skills)
                {
                    pawnInfo[skill.def.defName] = skill.Level;
                    pawnInfo[$"{skill.def.defName}_passion"] = (int)skill.passion;

                    // Vanilla Skills Expanded
                    pawnInfo[$"has_{skill.def.defName}_apathyPassion"] = skill.passion.GetLabel() == "Apathy" ? 1 : 0;
                    pawnInfo[$"has_{skill.def.defName}_criticalPassion"] = skill.passion.GetLabel() == "Critical" ? 1 : 0;
                    pawnInfo[$"has_{skill.def.defName}_naturalPassion"] = skill.passion.GetLabel() == "Natural" ? 1 : 0;

                }

                int childrenCount = 0;
                if (pawn.relations != null)
                {
                    childrenCount = pawn.relations.Children
                    .Where(
                        p => p.ageTracker.CurLifeStage.defName == "HumanlikeChild" 
                        || p.ageTracker.CurLifeStage.defName == "HumanlikePreTeenager"
                        || p.ageTracker.CurLifeStage.defName == "HumanlikeBaby"
                        )
                    .Count();
                }

                pawnInfo["childrenCount"] = childrenCount;
            }
            return pawnInfo;
        }
    }
}
