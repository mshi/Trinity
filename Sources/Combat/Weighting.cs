using System;
using System.Linq;
using Trinity.Combat.Abilities;
using Trinity.Config.Combat;
using Trinity.DbProvider;
using Trinity.Technicals;
using Trinity.XmlTags;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile.Common;
using Zeta.Internals.Actors;
using Zeta.Internals.SNO;
namespace Trinity
{
    public partial class Trinity : IPlugin
    {
        private static double GetLastHadUnitsInSights()
        {
            return Math.Max(DateTime.Now.Subtract(lastHadUnitInSights).TotalMilliseconds, DateTime.Now.Subtract(lastHadEliteUnitInSights).TotalMilliseconds);
        }

        private static void RefreshDiaGetWeights()
        {
            using (new PerformanceLogger("RefreshDiaObjectCache.Weighting"))
            {
                double MovementSpeed = PlayerMover.GetMovementSpeed();

                bool noGoblinsPresent = (!AnyTreasureGoblinsPresent && Settings.Combat.Misc.GoblinPriority >= GoblinPriority.Prioritize) || Settings.Combat.Misc.GoblinPriority < GoblinPriority.Prioritize;

                // Store if we are ignoring all units this cycle or not
                bool ignoreAllUnits = !XmlTags.TrinityTownPortal.ForceClearArea && !AnyElitesPresent && !AnyMobsInRange && noGoblinsPresent && Player.CurrentHealthPct >= 0.85d;

                bool prioritizeCloseRangeUnits = (ForceCloseRangeTarget || Player.IsRooted || MovementSpeed < 1 || ObjectCache.Count(u => u.Type == GObjectType.Unit && u.RadiusDistance < 5f) >= 3);

                bool hasWrathOfTheBerserker = Player.ActorClass == ActorClass.Barbarian && GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker);

                int TrashMobCount = ObjectCache.Count(u => u.Type == GObjectType.Unit && u.IsTrashMob);
                int EliteCount = CombatBase.IgnoringElites ? 0 : ObjectCache.Count(u => u.Type == GObjectType.Unit && u.IsBossOrEliteRareUnique);
                int AvoidanceCount = Settings.Combat.Misc.AvoidAOE ? 0 : ObjectCache.Count(o => o.Type == GObjectType.Avoidance && o.CentreDistance <= 50f);

                bool profileTagCheck = false;
                if (ProfileManager.CurrentProfileBehavior != null)
                {
                    Type behaviorType = ProfileManager.CurrentProfileBehavior.GetType();
                    if (behaviorType == typeof(WaitTimerTag) || behaviorType == typeof(UseTownPortalTag) || behaviorType == typeof(XmlTags.TrinityTownRun) || behaviorType == typeof(XmlTags.TrinityTownPortal))
                    {
                        profileTagCheck = true;
                    }
                }

                bool ShouldIgnoreElites =
                    !DataDictionary.QuestLevelAreaIds.Contains(Player.LevelAreaId) &&
                     !profileTagCheck &&
                     !XmlTags.TrinityTownPortal.ForceClearArea &&
                     !TownRun.IsTryingToTownPortal() &&
                    CombatBase.IgnoringElites;

                Logger.Log(TrinityLogLevel.Debug, LogCategory.Weight,
                    "Starting weights: packSize={0} packRadius={1} MovementSpeed={2} Elites={3} AoEs={4} disableIgnoreTag={5} closeRangePriority={6} townRun={7} forceClear={8} questing={9} level={10}",
                    Settings.Combat.Misc.TrashPackSize, Settings.Combat.Misc.TrashPackClusterRadius, MovementSpeed, EliteCount, AvoidanceCount, profileTagCheck,
                    prioritizeCloseRangeUnits, TownRun.IsTryingToTownPortal(), TrinityTownPortal.ForceClearArea, DataDictionary.QuestLevelAreaIds.Contains(Player.LevelAreaId), Player.Level);

                foreach (TrinityCacheObject cacheObject in ObjectCache.OrderBy(c => c.CentreDistance))
                {
                    bool questing = DataDictionary.QuestLevelAreaIds.Contains(Player.LevelAreaId);
                    bool townPortal = TownRun.IsTryingToTownPortal();
                    bool elitesInRange =
                        ObjectCache.Any(u => u.IsEliteRareUnique && u.Position.Distance2D(cacheObject.Position) <= 25f);

                    bool shouldIgnoreTrashMobs =
                        (!questing &&
                        !XmlTags.TrinityTownPortal.ForceClearArea &&
                        !townPortal &&
                        !profileTagCheck &&
                        !prioritizeCloseRangeUnits &&
                        Settings.Combat.Misc.TrashPackSize > 1 &&
                        //EliteCount == 0 &&
                        !elitesInRange &&
                        //AvoidanceCount == 0 &&
                        Player.Level >= 15 &&
                        MovementSpeed >= 1 &&
                        Player.CurrentHealthPct > 0.10
                        );

                    string unitWeightInfo = "";

                    // Just to make sure each one starts at 0 weight...
                    cacheObject.Weight = 0d;

                    // Now do different calculations based on the object type
                    switch (cacheObject.Type)
                    {
                        // Weight Units
                        case GObjectType.Unit:
                            {
                                // No champions, no mobs nearby, no treasure goblins to prioritize, and not injured, so skip mobs
                                if (ignoreAllUnits)
                                {
                                    break;
                                }

                                int nearbyMonsterCount = ObjectCache.Count(u => u.IsTrashMob && cacheObject.Position.Distance2D(u.Position) <= Settings.Combat.Misc.TrashPackClusterRadius);

                                bool isInHotSpot = GroupHotSpots.CacheObjectIsInHotSpot(cacheObject);

                                bool ignoring = false;
                                // Ignore Solitary Trash mobs (no elites present)
                                // Except if has been primary target or if already low on health (<= 20%)
                                if (shouldIgnoreTrashMobs && cacheObject.IsTrashMob && //!cacheObject.HasBeenPrimaryTarget &&
                                    !isInHotSpot &&
                                    !(nearbyMonsterCount >= Settings.Combat.Misc.TrashPackSize))
                                {
                                    unitWeightInfo = "Ignoring ";
                                    ignoring = true;
                                }
                                else
                                {
                                    unitWeightInfo = "Adding ";
                                }
                                unitWeightInfo += String.Format("nearbyCount={0} radiusDistance={1:0} hotspot={2} ShouldIgnore={3} elitesInRange={4}",
                                    nearbyMonsterCount, cacheObject.RadiusDistance, isInHotSpot, shouldIgnoreTrashMobs, elitesInRange);
                                if (ignoring)
                                    break;

                                // Ignore elite option, except if trying to town portal
                                if (!cacheObject.IsBoss && ShouldIgnoreElites && cacheObject.IsEliteRareUnique)
                                {
                                    break;
                                }

                                // Ignore trash mobs < 15% health or 50% health with a DoT
                                if (cacheObject.IsTrashMob && shouldIgnoreTrashMobs &&
                                    (cacheObject.HitPointsPct < Settings.Combat.Misc.IgnoreTrashBelowHealth ||
                                     cacheObject.HitPointsPct < Settings.Combat.Misc.IgnoreTrashBelowHealthDoT && cacheObject.HasDotDPS))
                                {
                                    break;
                                }

                                // Monster is in cache but not within kill range
                                if (!cacheObject.IsBoss && cacheObject.RadiusDistance > cacheObject.KillRange)
                                {
                                    // monsters near players given higher weight
                                    foreach (var player in ObjectCache.Where(p => p.Type == GObjectType.Player))
                                    {
                                        cacheObject.Weight += Math.Max((15f - cacheObject.Position.Distance2D(player.Position) / 15f * 500d), 10d);
                                    }
                                    if (cacheObject.Weight <= 0)
                                        break;
                                }

                                if (cacheObject.HitPoints <= 0)
                                {
                                    break;
                                }

                                if (cacheObject.RadiusDistance <= 25f && !bAnyNonWWIgnoreMobsInRange && !DataDictionary.WhirlwindIgnoreSNOIds.Contains(cacheObject.ActorSNO))
                                {
                                    bAnyNonWWIgnoreMobsInRange = true;
                                }

                                // Force a close range target because we seem to be stuck *OR* if not ranged and currently rooted
                                if (prioritizeCloseRangeUnits)
                                {
                                    cacheObject.Weight = Math.Max((50 - cacheObject.RadiusDistance) / 50 * 2000d, 2d);

                                    // Goblin priority KAMIKAZEEEEEEEE
                                    if (cacheObject.IsTreasureGoblin && Settings.Combat.Misc.GoblinPriority == GoblinPriority.Kamikaze)
                                        cacheObject.Weight += 25000;
                                }
                                else
                                {

                                    // Not attackable, could be shielded, make super low priority
                                    if (cacheObject.HasAffixShielded && cacheObject.Unit.IsInvulnerable)
                                    {
                                        // Only 100 weight helps prevent it being prioritized over an unshielded
                                        cacheObject.Weight = 100;
                                    }
                                    // Not forcing close-ranged targets from being stuck, so let's calculate a weight!
                                    else
                                    {
                                        // Elites/Bosses that are killed should have weight erased so we don't keep attacking
                                        if ((cacheObject.IsEliteRareUnique || cacheObject.IsBoss) && cacheObject.HitPointsPct <= 0)
                                        {
                                            cacheObject.Weight = 0;
                                            break;
                                        }

                                        // Starting weight of 500
                                        if (cacheObject.IsTrashMob)
                                            cacheObject.Weight = Math.Max((CurrentBotKillRange - cacheObject.RadiusDistance) / CurrentBotKillRange * 500d, 2d);

                                        // Starting weight of 1000 for elites
                                        if (cacheObject.IsBossOrEliteRareUnique)
                                            cacheObject.Weight = Math.Max((90f - cacheObject.RadiusDistance) / 90f * 2000d, 20d);


                                        // Monsters near players given higher weight
                                        if (cacheObject.Weight > 0)
                                        {
                                            foreach (var player in ObjectCache.Where(p => p.Type == GObjectType.Player && p.ACDGuid != Player.ACDGuid))
                                            {
                                                cacheObject.Weight += Math.Max(((55f - cacheObject.Position.Distance2D(player.Position)) / 55f * 500d), 2d);
                                            }
                                        }

                                        // Is standing in HotSpot - focus fire!
                                        if (isInHotSpot)
                                        {
                                            cacheObject.Weight += 10000d;
                                        }

                                        // Give extra weight to ranged enemies
                                        if ((Player.ActorClass == ActorClass.Barbarian || Player.ActorClass == ActorClass.Monk) &&
                                            (cacheObject.MonsterStyle == MonsterSize.Ranged || DataDictionary.RangedMonsterIds.Contains(c_ActorSNO)))
                                        {
                                            cacheObject.Weight += 1100d;
                                            cacheObject.ForceLeapAgainst = true;
                                        }

                                        // Lower health gives higher weight - health is worth up to 1000ish extra weight
                                        if (cacheObject.IsTrashMob && cacheObject.HitPointsPct < 0.20)
                                            cacheObject.Weight += Math.Max((100 - cacheObject.HitPointsPct) / 100 * 1000d, 100);

                                        // Elites on low health get extra priority - up to 2500ish
                                        if (cacheObject.IsBossOrEliteRareUnique && cacheObject.HitPointsPct < 0.20)
                                            cacheObject.Weight += Math.Max((100 - cacheObject.HitPointsPct) / 100 * 2500d, 100);

                                        // Goblins on low health get extra priority - up to 2000ish
                                        if (Settings.Combat.Misc.GoblinPriority >= GoblinPriority.Prioritize && cacheObject.IsTreasureGoblin && cacheObject.HitPointsPct <= 0.98)
                                            cacheObject.Weight += Math.Max((100 - cacheObject.HitPointsPct) / 100 * 2000d, 100);

                                        // Bonuses to priority type monsters from the dictionary/hashlist set at the top of the code
                                        int extraPriority;
                                        if (DataDictionary.MonsterCustomWeights.TryGetValue(cacheObject.ActorSNO, out extraPriority))
                                        {
                                            // adding a constant multiple of 3 to all weights here (e.g. 999 becomes 2997)
                                            cacheObject.Weight += extraPriority * 3d;
                                        }

                                        // Close range get higher weights the more of them there are, to prevent body-blocking
                                        if (!cacheObject.IsBoss && cacheObject.RadiusDistance <= 5f)
                                        {
                                            cacheObject.Weight += (2000d * cacheObject.Radius);
                                        }

                                        // Special additional weight for corrupt growths in act 4 ONLY if they are at close range (not a standard priority thing)
                                        if ((cacheObject.ActorSNO == 210120 || cacheObject.ActorSNO == 210268) && cacheObject.CentreDistance <= 25f)
                                            cacheObject.Weight += 2000d;

                                        // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                        if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                            cacheObject.Weight += 1000d;

                                        if (ObjectCache.Any(u =>MathEx.IntersectsPath(u.Position, u.Radius, Trinity.Player.Position,
                                                        cacheObject.Position)))
                                            cacheObject.Weight *= 0.10d;

                                        // Prevent going less than 300 yet to prevent annoyances (should only lose this much weight from priority reductions in priority list?)
                                        if (cacheObject.Weight < 300)
                                            cacheObject.Weight = 300d;

                                        // If standing Molten, Arcane, or Poison Tree near unit, reduce weight
                                        if (PlayerKiteDistance <= 0 &&
                                            AvoidanceObstacleCache.Any(aoe =>
                                            (aoe.AvoidanceType == AvoidanceType.Arcane ||
                                            aoe.AvoidanceType == AvoidanceType.MoltenCore ||
                                                //aoe.AvoidanceType == AvoidanceType.MoltenTrail ||
                                            aoe.AvoidanceType == AvoidanceType.PoisonTree) &&
                                            cacheObject.Position.Distance2D(aoe.Location) <= aoe.Radius))
                                            cacheObject.Weight *= 0.25;

                                        // If any AoE between us and target, reduce weight, for melee only
                                        if (!Settings.Combat.Misc.KillMonstersInAoE &&
                                            PlayerKiteDistance <= 0 &&
                                            AvoidanceObstacleCache.Any(aoe => aoe.AvoidanceType != AvoidanceType.PlagueCloud &&
                                                MathUtil.IntersectsPath(aoe.Location, aoe.Radius, Player.Position, cacheObject.Position)))
                                            cacheObject.Weight *= 0.25;

                                        // See if there's any AOE avoidance in that spot, if so reduce the weight to 1, for melee only
                                        if (!Settings.Combat.Misc.KillMonstersInAoE &&
                                            PlayerKiteDistance <= 0 &&
                                            AvoidanceObstacleCache.Any(aoe => aoe.AvoidanceType != AvoidanceType.PlagueCloud &&
                                                cacheObject.Position.Distance2D(aoe.Location) <= aoe.Radius))
                                            cacheObject.Weight = 1d;

                                        if (PlayerKiteDistance > 0)
                                        {
                                            if (ObjectCache.Any(m => m.Type == GObjectType.Unit &&
                                                MathUtil.IntersectsPath(cacheObject.Position, cacheObject.Radius, Player.Position, m.Position) &&
                                                m.RActorGuid != cacheObject.RActorGuid))
                                            {
                                                cacheObject.Weight = 0d;
                                            }
                                        }


                                        // Deal with treasure goblins - note, of priority is set to "0", then the is-a-goblin flag isn't even set for use here - the monster is ignored
                                        if (cacheObject.IsTreasureGoblin && !ObjectCache.Any(u => (u.Type == GObjectType.Door || u.Type == GObjectType.Barricade) && u.RadiusDistance <= 40f))
                                        {
                                            // Logging goblin sightings
                                            if (lastGoblinTime == DateTime.Today)
                                            {
                                                iTotalNumberGoblins++;
                                                lastGoblinTime = DateTime.Now;
                                                Logger.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "Goblin #{0} in sight. Distance={1:0}", iTotalNumberGoblins, cacheObject.CentreDistance);
                                            }
                                            else
                                            {
                                                if (DateTime.Now.Subtract(lastGoblinTime).TotalMilliseconds > 30000)
                                                    lastGoblinTime = DateTime.Today;
                                            }

                                            if (AvoidanceObstacleCache.Any(aoe => cacheObject.Position.Distance2D(aoe.Location) <= aoe.Radius) && Settings.Combat.Misc.GoblinPriority != GoblinPriority.Kamikaze)
                                            {
                                                cacheObject.Weight = 1;
                                                break;
                                            }

                                            // Original Trinity stuff for priority handling now
                                            switch (Settings.Combat.Misc.GoblinPriority)
                                            {
                                                case GoblinPriority.Normal:
                                                    // Treating goblins as "normal monsters". Ok so I lied a little in the config, they get a little extra weight really! ;)
                                                    cacheObject.Weight += 751;
                                                    break;
                                                case GoblinPriority.Prioritize:
                                                    // Super-high priority option below... 
                                                    cacheObject.Weight += 5000;
                                                    break;
                                                case GoblinPriority.Kamikaze:
                                                    // KAMIKAZE SUICIDAL TREASURE GOBLIN RAPE AHOY!
                                                    cacheObject.Weight += 20000;
                                                    break;

                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case GObjectType.HotSpot:
                            {
                                // If there's monsters in our face, ignore
                                if (prioritizeCloseRangeUnits)
                                    break;

                                // If it's very close, ignore
                                if (cacheObject.CentreDistance <= V.F("Cache.HotSpot.MinDistance"))
                                {
                                    break;
                                }
                                else if (!AvoidanceObstacleCache.Any(aoe => aoe.Location.Distance2D(cacheObject.Position) <= aoe.Radius))
                                {
                                    float maxDist = V.F("Cache.HotSpot.MaxDistance");
                                    cacheObject.Weight = (maxDist - cacheObject.CentreDistance) / maxDist * 50000d;
                                }
                                break;
                            }
                        case GObjectType.Item:
                        case GObjectType.Gold:
                            {
                                // Weight Items

                                // We'll weight them based on distance, giving gold less weight and close objects more
                                //if (cacheObject.GoldAmount > 0)
                                //    cacheObject.Weight = 5000d - (Math.Floor(cacheObject.CentreDistance) * 2000d);
                                //else
                                //    cacheObject.Weight = 8000d - (Math.Floor(cacheObject.CentreDistance) * 1900d);

                                if (cacheObject.GoldAmount > 0)
                                    cacheObject.Weight = (300 - cacheObject.CentreDistance) / 300 * 9000d;
                                else
                                    cacheObject.Weight = (300 - cacheObject.CentreDistance) / 300 * 9000d;


                                // Point-blank items get a weight increase 
                                if (cacheObject.GoldAmount <= 0 && cacheObject.CentreDistance <= 12f)
                                    cacheObject.Weight += 1000d;

                                // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                if (cacheObject.RActorGuid == CurrentTargetRactorGUID)
                                    cacheObject.Weight += 800;

                                // Give yellows more weight
                                if (cacheObject.GoldAmount <= 0 && cacheObject.ItemQuality >= ItemQuality.Rare4)
                                    cacheObject.Weight += 4000d;

                                // Give legendaries more weight
                                if (cacheObject.GoldAmount <= 0 && cacheObject.ItemQuality >= ItemQuality.Legendary)
                                    cacheObject.Weight += 15000d;

                                // Are we prioritizing close-range stuff atm? If so limit it at a value 3k lower than monster close-range priority
                                //if (PrioritizeCloseRangeUnits)
                                //    cacheObject.Weight = (200f - cacheObject.CentreDistance) / 200f * 18000d;

                                if (Player.ActorClass == ActorClass.Monk && TimeSinceUse(SNOPower.Monk_TempestRush) < 1000 && cacheObject.ItemQuality < ItemQuality.Legendary)
                                {
                                    cacheObject.Weight = 500;
                                }

                                // If there's a monster in the path-line to the item, reduce the weight to 1, except legendaries
                                if (//cacheObject.ItemQuality < ItemQuality.Legendary && 
                                    MonsterObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius * 1.2f, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight = 1;

                                // ignore any items/gold if there is mobs in kill radius and we aren't combat looting
                                if (CurrentTarget != null && AnyMobsInRange && !Zeta.CommonBot.Settings.CharacterSettings.Instance.CombatLooting && cacheObject.ItemQuality < ItemQuality.Legendary)
                                    cacheObject.Weight = 1;

                                // See if there's any AOE avoidance in that spot or inbetween us, if so reduce the weight to 1
                                if (AvoidanceObstacleCache.Any(aoe => cacheObject.Position.Distance2D(aoe.Location) <= aoe.Radius))
                                    cacheObject.Weight = 1;

                                // Deprioritize item if a monster is blocking our path
                                if (MonsterObstacleCache.Any(o => MathUtil.IntersectsPath(o.Location, o.Radius, Player.Position, c_Position)))
                                {
                                    cacheObject.Weight *= 0.10d;
                                }
                                // ignore non-legendaries and gold near elites if we're ignoring elites
                                // not sure how we should safely determine this distance
                                if (CombatBase.IgnoringElites && cacheObject.ItemQuality < ItemQuality.Legendary &&
                                    ObjectCache.Any(u => u.Type == GObjectType.Unit && u.IsEliteRareUnique && u.Position.Distance2D(cacheObject.Position) <= 40f))
                                {
                                    cacheObject.Weight = 0;
                                }

                                break;
                            }
                        case GObjectType.Globe:
                            {
                                // Calculate a spot reaching a little bit further out from the globe, to help globe-movements
                                if (cacheObject.Weight > 0)
                                    cacheObject.Position = MathEx.CalculatePointFrom(cacheObject.Position, Player.Position, cacheObject.CentreDistance + 3f);

                                // Weight Health Globes

                                bool witchDoctorManaLow =
                                    Player.ActorClass == ActorClass.WitchDoctor &&
                                    Player.PrimaryResourcePct <= 0.15 &&
                                    ZetaDia.CPlayer.PassiveSkills.Contains(SNOPower.Witchdoctor_Passive_GruesomeFeast);

                                if ((Player.CurrentHealthPct >= 1 || !Settings.Combat.Misc.CollectHealthGlobe))
                                {
                                    cacheObject.Weight = 0;
                                }
                                // Give all globes super low weight if we don't urgently need them, but are not 100% health
                                else if (!witchDoctorManaLow && (Player.CurrentHealthPct > PlayerEmergencyHealthGlobeLimit))
                                {
                                    double myHealth = Player.CurrentHealthPct;

                                    double minPartyHealth = 1d;
                                    if (ObjectCache.Any(p => p.Type == GObjectType.Player && p.RActorGuid != Player.RActorGuid))
                                        minPartyHealth = ObjectCache.Where(p => p.Type == GObjectType.Player && p.RActorGuid != Player.RActorGuid).Min(p => p.HitPointsPct);

                                    if (myHealth > 0d && myHealth < V.D("Weight.Globe.MinPlayerHealthPct"))
                                        cacheObject.Weight = (1d - myHealth) * 1000d;

                                    // Added weight for lowest health of party member
                                    if (minPartyHealth > 0d && minPartyHealth < V.D("Weight.Globe.MinPartyHealthPct"))
                                        cacheObject.Weight = (1d - minPartyHealth) * 2500d;
                                }
                                else
                                {
                                    // Ok we have globes enabled, and our health is low
                                    cacheObject.Weight = (90f - cacheObject.RadiusDistance) / 90f * 17000d;

                                    if (witchDoctorManaLow)
                                        cacheObject.Weight += 10000d; // 10k for WD's!

                                    // Point-blank items get a weight increase
                                    if (cacheObject.CentreDistance <= 15f)
                                        cacheObject.Weight += 3000d;

                                    // Close items get a weight increase
                                    if (cacheObject.CentreDistance <= 60f)
                                        cacheObject.Weight += 1500d;

                                    // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                    if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                        cacheObject.Weight += 800;
                                }

                                // If there's a monster in the path-line to the item, reduce the weight by 15% for each
                                Vector3 point = cacheObject.Position;
                                foreach (CacheObstacleObject tempobstacle in MonsterObstacleCache.Where(cp =>
                                    MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, point)))
                                {
                                    cacheObject.Weight *= 0.85;
                                }

                                if (cacheObject.CentreDistance > 10f)
                                {
                                    // See if there's any AOE avoidance in that spot, if so reduce the weight by 10%
                                    if (AvoidanceObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                        cacheObject.Weight *= 0.9;

                                }

                                // do not collect health globes if we are kiting and health globe is too close to monster or avoidance
                                if (PlayerKiteDistance > 0)
                                {
                                    if (MonsterObstacleCache.Any(m => m.Location.Distance(cacheObject.Position) < PlayerKiteDistance))
                                        cacheObject.Weight = 0;
                                    if (AvoidanceObstacleCache.Any(m => m.Location.Distance(cacheObject.Position) < PlayerKiteDistance))
                                        cacheObject.Weight = 0;
                                }
                                break;
                            }
                        case GObjectType.HealthWell:
                            {
                                if (MonsterObstacleCache.Any(unit => MathUtil.IntersectsPath(unit.Location, unit.Radius, Player.Position, cacheObject.Position)))
                                {
                                    // As a percentage of health with typical maximum weight
                                    cacheObject.Weight = 50000d * (1 - Trinity.Player.CurrentHealthPct);
                                }
                                break;
                            }
                        case GObjectType.Shrine:
                            {
                                // Weight Shrines
                                cacheObject.Weight = Math.Max(((75f - cacheObject.RadiusDistance) / 75f * 14500f), 100d);

                                // Very close shrines get a weight increase
                                if (cacheObject.CentreDistance <= 30f)
                                    cacheObject.Weight += 10000d;

                                if (cacheObject.Weight > 0)
                                {
                                    // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                    if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                        cacheObject.Weight += 400;

                                    // If there's a monster in the path-line to the item
                                    if (MonsterObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                        cacheObject.Weight = 1;

                                    // See if there's any AOE avoidance in that spot, if so reduce the weight to 1
                                    if (AvoidanceObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                        cacheObject.Weight = 1;

                                    // if there's any monsters nearby
                                    if (TargetUtil.AnyMobsInRange(15f))
                                        cacheObject.Weight = 1;

                                    if (prioritizeCloseRangeUnits)
                                        cacheObject.Weight = 1;
                                }
                                break;
                            }
                        case GObjectType.Door:
                            {
                                if (!ObjectCache.Any(u => u.Type == GObjectType.Unit && u.HitPointsPct > 0 &&
                                    MathUtil.IntersectsPath(u.Position, u.Radius, Player.Position, cacheObject.Position)))
                                {
                                    if (cacheObject.RadiusDistance <= 20f)
                                        cacheObject.Weight += 15000d;

                                    // We're standing on the damn thing... open it!!
                                    if (cacheObject.RadiusDistance <= 12f)
                                        cacheObject.Weight += 30000d;
                                }
                                break;
                            }
                        case GObjectType.Destructible:
                        case GObjectType.Barricade:
                            {

                                // rrrix added this as a single "weight" source based on the DestructableRange.
                                // Calculate the weight based on distance, where a distance = 1 is 5000, 90 = 0
                                cacheObject.Weight = (90f - cacheObject.RadiusDistance) / 90f * 5000f;

                                // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                    cacheObject.Weight += 400;

                                //// Close destructibles get a weight increase
                                //if (cacheObject.CentreDistance <= 16f)
                                //    cacheObject.Weight += 1500d;

                                // If there's a monster in the path-line to the item, reduce the weight by 50%
                                if (MonsterObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight *= 0.5;

                                // See if there's any AOE avoidance in that spot, if so reduce the weight to 1
                                if (AvoidanceObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight = 1;

                                // Are we prioritizing close-range stuff atm? If so limit it at a value 3k lower than monster close-range priority
                                if (prioritizeCloseRangeUnits)
                                    cacheObject.Weight = (200d - cacheObject.CentreDistance) / 200d * 19200d;

                                //// We're standing on the damn thing... break it
                                if (cacheObject.RadiusDistance <= 5f)
                                    cacheObject.Weight += 40000d;

                                //// Fix for WhimsyShire Pinata
                                if (DataDictionary.ResplendentChestIds.Contains(cacheObject.ActorSNO))
                                    cacheObject.Weight = 100 + cacheObject.RadiusDistance;
                                break;
                            }
                        case GObjectType.Interactable:
                            {
                                // Weight Interactable Specials

                                // Need to Prioritize, forget it!
                                if (prioritizeCloseRangeUnits)
                                    break;

                                // Very close interactables get a weight increase
                                cacheObject.Weight = (90d - cacheObject.CentreDistance) / 90d * 1500d;
                                if (cacheObject.CentreDistance <= 12f)
                                    cacheObject.Weight += 1000d;

                                // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                    cacheObject.Weight += 400;

                                // If there's a monster in the path-line to the item, reduce the weight by 50%
                                if (MonsterObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight *= 0.5;

                                // See if there's any AOE avoidance in that spot, if so reduce the weight to 1
                                if (AvoidanceObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight = 1;

                                //if (bAnyMobsInCloseRange || (CurrentTarget != null && CurrentTarget.IsBossOrEliteRareUnique))
                                //    cacheObject.Weight = 1;

                                break;
                            }
                        case GObjectType.Container:
                            {

                                // Weight Containers

                                // Very close containers get a weight increase
                                cacheObject.Weight = (190d - cacheObject.CentreDistance) / 190d * 11000d;
                                if (cacheObject.CentreDistance <= 12f)
                                    cacheObject.Weight += 600d;

                                // Was already a target and is still viable, give it some free extra weight, to help stop flip-flopping between two targets
                                if (cacheObject.RActorGuid == CurrentTargetRactorGUID && cacheObject.CentreDistance <= 25f)
                                    cacheObject.Weight += 400;

                                // If there's a monster in the path-line to the item, reduce the weight by 50%
                                if (MonsterObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight *= 0.5;

                                // See if there's any AOE avoidance in that spot, if so reduce the weight to 1
                                if (AvoidanceObstacleCache.Any(cp => MathUtil.IntersectsPath(cp.Location, cp.Radius, Player.Position, cacheObject.Position)))
                                    cacheObject.Weight = 1;
                                break;
                            }

                    }

                    // Force the character to stay where it is if there is nothing available that is out of avoidance stuff and we aren't already in avoidance stuff
                    if (cacheObject.Weight == 1 && !StandingInAvoidance && ObjectCache.Any(o => o.Type == GObjectType.Avoidance))
                    {
                        cacheObject.Weight = 0;
                        ShouldStayPutDuringAvoidance = true;
                    }
                    Logger.Log(TrinityLogLevel.Debug, LogCategory.Weight,
                        "Weight={2:0} name={0} sno={1} type={3} R-Dist={4:0} IsElite={5} RAGuid={6} {7}",
                            cacheObject.InternalName, cacheObject.ActorSNO, cacheObject.Weight, cacheObject.Type, cacheObject.RadiusDistance, cacheObject.IsEliteRareUnique, cacheObject.RActorGuid, unitWeightInfo);

                    // Prevent current target dynamic ranged weighting flip-flop 
                    if (CurrentTargetRactorGUID == cacheObject.RActorGuid && cacheObject.Weight <= 1)
                    {
                        cacheObject.Weight = 100;
                    }

                    // Is the weight of this one higher than the current-highest weight? Then make this the new primary target!
                    if (cacheObject.Weight > w_HighestWeightFound && cacheObject.Weight > 0)
                    {
                        // Clone the current CacheObject
                        CurrentTarget = cacheObject.Clone();
                        w_HighestWeightFound = cacheObject.Weight;

                        // See if we can try attempting kiting later
                        NeedToKite = false;
                        vKitePointAvoid = Vector3.Zero;

                        // Kiting and Avoidance
                        if (CurrentTarget.Type == GObjectType.Unit)
                        {
                            var AvoidanceList = AvoidanceObstacleCache.Where(o =>
                                // Distance from avoidance to target is less than avoidance radius
                                o.Location.Distance(CurrentTarget.Position) <= (GetAvoidanceRadius(o.ActorSNO) * 1.2) &&
                                    // Distance from obstacle to me is <= cacheObject.RadiusDistance
                                o.Location.Distance(Player.Position) <= (cacheObject.RadiusDistance - 4f)
                                );

                            // if there's any obstacle within a specified distance of the avoidance radius *1.2 
                            if (AvoidanceList.Any())
                            {
                                foreach (CacheObstacleObject o in AvoidanceList)
                                {
                                    Logger.Log(TrinityLogLevel.Debug, LogCategory.Targetting, "Avoidance: Id={0} Weight={1} Loc={2} Radius={3} Name={4}", o.ActorSNO, o.Weight, o.Location, o.Radius, o.Name);
                                }

                                vKitePointAvoid = CurrentTarget.Position;
                                NeedToKite = true;
                            }
                        }
                    }
                }

                // Loop through all the objects and give them a weight
                if (CurrentTarget != null && CurrentTarget.InternalName != null && CurrentTarget.ActorSNO > 0 && CurrentTarget.RActorGuid != CurrentTargetRactorGUID)
                {
                    RecordTargetHistory();

                    Logger.Log(TrinityLogLevel.Verbose,
                                    LogCategory.Targetting,
                                    "Target changed to name={2} sno={0} type={1} raGuid={3}",
                                    CurrentTarget.InternalName,
                                    CurrentTarget.ActorSNO,
                                    CurrentTarget.Type,
                                    CurrentTarget.RActorGuid);
                }
            }
        }

        private static void RecordTargetHistory()
        {
            string targetMd5Hash = HashGenerator.GenerateObjecthash(CurrentTarget);

            // clean up past targets
            if (!GenericCache.ContainsKey(targetMd5Hash))
            {
                CurrentTarget.HasBeenPrimaryTarget = true;
                CurrentTarget.TimesBeenPrimaryTarget = 1;
                CurrentTarget.FirstTargetAssignmentTime = DateTime.Now;
                GenericCache.AddToCache(new GenericCacheObject(targetMd5Hash, CurrentTarget, new TimeSpan(0, 10, 0)));
            }
            else if (GenericCache.ContainsKey(targetMd5Hash))
            {
                TrinityCacheObject cTarget = (TrinityCacheObject)GenericCache.GetObject(targetMd5Hash).Value;
                bool isEliteLowHealth = cTarget.HitPointsPct <= 0.75 && cTarget.IsBossOrEliteRareUnique;
                bool isLegendaryItem = cTarget.Type == GObjectType.Item && cTarget.ItemQuality >= ItemQuality.Legendary;
                if (!cTarget.IsBoss && cTarget.TimesBeenPrimaryTarget > 15 && !isEliteLowHealth && !isLegendaryItem)
                {
                    Logger.Log(TrinityLogLevel.Normal, LogCategory.UserInformation, "Blacklisting target {0} ActorSNO={1} RActorGUID={2} due to possible stuck/flipflop!",
                        CurrentTarget.InternalName, CurrentTarget.ActorSNO, CurrentTarget.RActorGuid);

                    hashRGUIDBlacklist60.Add(CurrentTarget.RActorGuid);

                    // Add to generic blacklist for safety, as the RActorGUID on items and gold can change as we move away and get closer to the items (while walking around corners)
                    // So we can't use any ID's but rather have to use some data which never changes (actorSNO, position, type, worldID)
                    GenericBlacklist.AddToBlacklist(new GenericCacheObject()
                    {
                        Key = CurrentTarget.ObjectHash,
                        Value = null,
                        Expires = DateTime.Now.AddSeconds(60)
                    });
                }
                else
                {
                    cTarget.TimesBeenPrimaryTarget++;
                    GenericCache.UpdateObject(new GenericCacheObject(targetMd5Hash, cTarget, new TimeSpan(0, 10, 0)));
                }

            }
        }
    }
}
