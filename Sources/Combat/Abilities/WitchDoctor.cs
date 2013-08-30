﻿using System;
using System.Linq;
using Trinity.Combat.Abilities;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
namespace Trinity
{
    public partial class Trinity : IPlugin
    {
        private static TrinityPower GetWitchDoctorPower(bool IsCurrentlyAvoiding, bool UseOOCBuff, bool UseDestructiblePower)
        {
            bool hasGraveInjustice = ZetaDia.CPlayer.PassiveSkills.Contains(SNOPower.Witchdoctor_Passive_GraveInjustice);

            bool hasAngryChicken = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Hex && s.RuneIndex == 1);
            bool isChicken = hasAngryChicken && Player.IsHidden;

            // Hex with angry chicken, is chicken, explode!
            if (!UseOOCBuff && isChicken && (TargetUtil.AnyMobsInRange(12f, 1, false) || CurrentTarget.RadiusDistance <= 10f || UseDestructiblePower) && PowerManager.CanCast(SNOPower.Witchdoctor_Hex_Explode))
            {
                ShouldRefreshHotbarAbilities = true;
                return new TrinityPower(SNOPower.Witchdoctor_Hex_Explode, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 2, 2, WAIT_FOR_ANIM);
            }
            else if (hasAngryChicken)
            {
                ShouldRefreshHotbarAbilities = true;
            }

            // Pick the best destructible power available
            if (UseDestructiblePower)
            {
                return GetWitchDoctorDestroyPower();
            }

            // Witch doctors have no reserve requirements?
            MinEnergyReserve = 0;
            // Spirit Walk Cast on 65% health or while avoiding anything but molten core or incapacitated or Chasing Goblins
            if (Hotbar.Contains(SNOPower.Witchdoctor_SpiritWalk) && Player.PrimaryResource >= 49 &&
                (
                 Player.CurrentHealthPct <= 0.65 || Player.IsIncapacitated || Player.IsRooted || (Settings.Combat.Misc.AllowOOCMovement && UseOOCBuff) ||
                 (!UseOOCBuff && CurrentTarget.IsTreasureGoblin && CurrentTarget.HitPointsPct < 0.90 && CurrentTarget.RadiusDistance <= 40f)
                ) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_SpiritWalk))
            {
                return new TrinityPower(SNOPower.Witchdoctor_SpiritWalk, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Witch Doctor - Terror
            //skillDict.Add("SoulHarvest", SNOPower.Witchdoctor_SoulHarvest);
            //runeDict.Add("SwallowYourSoul", 3);
            //runeDict.Add("Siphon", 0);
            //runeDict.Add("Languish", 2);
            //runeDict.Add("SoulToWaste", 1);
            //runeDict.Add("VengefulSpirit", 4);

            bool hasVengefulSpirit = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_SoulHarvest && s.RuneIndex == 4);

            // Soul Harvest Any Elites or 2+ Norms and baby it's harvest season
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_SoulHarvest) && !Player.IsIncapacitated && Player.PrimaryResource >= 59 && GetBuffStacks(SNOPower.Witchdoctor_SoulHarvest) <= 4 &&
                (TargetUtil.AnyMobsInRange(16f, 2) || TargetUtil.IsEliteTargetInRange(16f)) && PowerManager.CanCast(SNOPower.Witchdoctor_SoulHarvest))
            {
                return new TrinityPower(SNOPower.Witchdoctor_SoulHarvest, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Soul Harvest with VengefulSpirit
            if (!UseOOCBuff && !IsCurrentlyAvoiding && !Player.IsIncapacitated && Hotbar.Contains(SNOPower.Witchdoctor_SoulHarvest) && hasVengefulSpirit && Player.PrimaryResource >= 59
                && TargetUtil.AnyMobsInRange(16, 3) && GetBuffStacks(SNOPower.Witchdoctor_SoulHarvest) <= 4 && PowerManager.CanCast(SNOPower.Witchdoctor_SoulHarvest))
            {
                return new TrinityPower(SNOPower.Witchdoctor_SoulHarvest, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Sacrifice AKA Zombie Dog Jihad, use on Elites Only or to try and Save yourself
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_Sacrifice) &&
                (TargetUtil.AnyElitesInRange(15, 1) || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 9f)) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Sacrifice))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Sacrifice, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 1, 0, WAIT_FOR_ANIM);
            }

            // Gargantuan, Recast on 1+ Elites or Bosses to trigger Restless Giant
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Gargantuan) && !Player.IsIncapacitated && Player.PrimaryResource >= 147 &&
                (ElitesWithinRange[RANGE_15] >= 1 ||
                 (CurrentTarget != null && (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 15f)) || iPlayerOwnedGargantuan == 0) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Gargantuan))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Gargantuan, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 2, 1, WAIT_FOR_ANIM);
            }

            // Zombie Dogs non-sacrifice build
            if (!IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_SummonZombieDog) && !Hotbar.Contains(SNOPower.Witchdoctor_Sacrifice) && !Player.IsIncapacitated &&
                Player.PrimaryResource >= 49 && (ElitesWithinRange[RANGE_20] >= 2 || AnythingWithinRange[RANGE_20] >= 5 ||
                 (CurrentTarget != null && ((CurrentTarget.IsEliteRareUnique || CurrentTarget.IsTreasureGoblin) && CurrentTarget.RadiusDistance <= 30f)) || PlayerOwnedZombieDog <= 2) &&
                !Settings.Combat.WitchDoctor.ZeroDogs &&
                 PowerManager.CanCast(SNOPower.Witchdoctor_SummonZombieDog))
            {
                return new TrinityPower(SNOPower.Witchdoctor_SummonZombieDog, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Zombie Dogs for Sacrifice
            if (Hotbar.Contains(SNOPower.Witchdoctor_SummonZombieDog) && Hotbar.Contains(SNOPower.Witchdoctor_Sacrifice) &&
                Player.PrimaryResource >= 49 &&
                (LastPowerUsed == SNOPower.Witchdoctor_Sacrifice || PlayerOwnedZombieDog <= 2) &&
                //((TimeSinceUse(SNOPower.Witchdoctor_SummonZombieDog) > 1000 && TimeSinceUse(SNOPower.Witchdoctor_Sacrifice) < 1000) || TimeSinceUse(SNOPower.Witchdoctor_SummonZombieDog) > 1800) &&
                CombatBase.LastPowerUsed != SNOPower.Witchdoctor_SummonZombieDog &&
                //Settings.Combat.WitchDoctor.ZeroDogs &&
                PowerManager.CanCast(SNOPower.Witchdoctor_SummonZombieDog))
            {
                return new TrinityPower(SNOPower.Witchdoctor_SummonZombieDog, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 2, 2, WAIT_FOR_ANIM);
            }

            // Hex with angry chicken, check if we want to shape shift and explode
            if (!UseOOCBuff && !IsCurrentlyAvoiding && (TargetUtil.AnyMobsInRange(12f, 1, false) || CurrentTarget.RadiusDistance <= 10f) && 
                CombatBase.CanCast(SNOPower.Witchdoctor_Hex) && hasAngryChicken && Player.PrimaryResource >= 49)
            {
                ShouldRefreshHotbarAbilities = true;
                return new TrinityPower(SNOPower.Witchdoctor_Hex, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 2, WAIT_FOR_ANIM);
            }

            // Hex Spam Cast without angry chicken
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Hex) && !Player.IsIncapacitated && Player.PrimaryResource >= 49 && !hasAngryChicken &&
               (TargetUtil.AnyElitesInRange(12) || TargetUtil.AnyMobsInRange(12, 2) || TargetUtil.IsEliteTargetInRange(18f)) && PowerManager.CanCast(SNOPower.Witchdoctor_Hex))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Hex, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, NO_WAIT_ANIM);
            }
            // Mass Confuse, elites only or big mobs or to escape on low health
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_MassConfusion) && !Player.IsIncapacitated && Player.PrimaryResource >= 74 &&
                (ElitesWithinRange[RANGE_12] >= 1 || AnythingWithinRange[RANGE_12] >= 6 || Player.CurrentHealthPct <= 0.25 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 12f)) &&
                !CurrentTarget.IsTreasureGoblin && PowerManager.CanCast(SNOPower.Witchdoctor_MassConfusion))
            {
                return new TrinityPower(SNOPower.Witchdoctor_MassConfusion, 0f, Vector3.Zero, -1, CurrentTarget.ACDGuid, 1, 1, WAIT_FOR_ANIM);
            }
            // Big Bad Voodoo, elites and bosses only
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_BigBadVoodoo) && !Player.IsIncapacitated &&
                !CurrentTarget.IsTreasureGoblin &&
                (ElitesWithinRange[RANGE_6] > 0 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 12f)) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_BigBadVoodoo))
            {
                return new TrinityPower(SNOPower.Witchdoctor_BigBadVoodoo, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Grasp of the Dead, look below, droping globes and dogs when using it on elites and 3 norms
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_GraspOfTheDead) && !Player.IsIncapacitated &&
                (TargetUtil.AnyMobsInRange(30, 2)) &&
                Player.PrimaryResource >= 78 && PowerManager.CanCast(SNOPower.Witchdoctor_GraspOfTheDead))
            {
                var bestClusterPoint = TargetUtil.GetBestClusterPoint(15);

                return new TrinityPower(SNOPower.Witchdoctor_GraspOfTheDead, 25f, bestClusterPoint, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }
            //skillDict.Add("Horrify", SNOPower.Witchdoctor_Horrify);
            //runeDict.Add("Phobia", 2);
            //runeDict.Add("Stalker", 4);
            //runeDict.Add("FaceOfDeath", 1);
            //runeDict.Add("FrighteningAspect", 0);
            //runeDict.Add("RuthlessTerror", 3);

            bool hasPhobia = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Horrify && s.RuneIndex == 2);
            bool hasStalker = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Horrify && s.RuneIndex == 4);
            bool hasFaceOfDeath = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Horrify && s.RuneIndex == 1);
            bool hasFrighteningAspect = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Horrify && s.RuneIndex == 0);
            bool hasRuthlessTerror = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Horrify && s.RuneIndex == 3);

            float horrifyRadius = hasFaceOfDeath ? 24f : 12f;

            // Horrify 
            if (UseOOCBuff && hasGraveInjustice && Hotbar.Contains(SNOPower.Witchdoctor_Horrify) && !Player.IsIncapacitated && Player.PrimaryResource >= 37 &&
                !hasStalker && !hasFrighteningAspect && PowerManager.CanCast(SNOPower.Witchdoctor_Horrify) && TargetUtil.AnyMobsInRange(horrifyRadius, 3))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Horrify, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 2, WAIT_FOR_ANIM);
            }

            // Horrify Buff When not in combat for movement speed -- Stalker
            if (UseOOCBuff && hasGraveInjustice && Hotbar.Contains(SNOPower.Witchdoctor_Horrify) && !Player.IsIncapacitated && Player.PrimaryResource >= 37 &&
                hasStalker && PowerManager.CanCast(SNOPower.Witchdoctor_Horrify))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Horrify, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 2, WAIT_FOR_ANIM);
            }
            // Horrify Buff at 35% health -- Freightening Aspect
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_Horrify) && !Player.IsIncapacitated && Player.PrimaryResource >= 37 &&
                Player.CurrentHealthPct <= 0.35 && hasFrighteningAspect &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Horrify))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Horrify, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 0, 2, WAIT_FOR_ANIM);
            }
            // Fetish Army, elites only
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_FetishArmy) && !Player.IsIncapacitated &&
                (ElitesWithinRange[RANGE_25] > 0 || ((CurrentTarget.IsEliteRareUnique || CurrentTarget.IsTreasureGoblin || CurrentTarget.IsBoss) && CurrentTarget.RadiusDistance <= 16f)) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_FetishArmy))
            {
                return new TrinityPower(SNOPower.Witchdoctor_FetishArmy, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 1, 1, WAIT_FOR_ANIM);
            }

            //skillDict.Add("SpiritBarage", SNOPower.Witchdoctor_SpiritBarrage);
            //runeDict.Add("TheSpiritIsWilling", 3);
            //runeDict.Add("WellOfSouls", 1);
            //runeDict.Add("Phantasm", 2);
            //runeDict.Add("Phlebotomize", 0);
            //runeDict.Add("Manitou", 4);

            bool hasManitou = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_SpiritBarrage && s.RuneIndex == 4);

            // Spirit Barrage Manitou
            if (Hotbar.Contains(SNOPower.Witchdoctor_SpiritBarrage) && Player.PrimaryResource >= 108 && TimeSinceUse(SNOPower.Witchdoctor_SpiritBarrage) > 18000 && hasManitou)
            {
                return new TrinityPower(SNOPower.Witchdoctor_SpiritBarrage, 0f, Vector3.Zero, CurrentWorldDynamicId, -1, 2, 2, WAIT_FOR_ANIM);
            }

            //skillDict.Add("Haunt", SNOPower.Witchdoctor_Haunt);
            //runeDict.Add("ConsumingSpirit", 0);
            //runeDict.Add("ResentfulSpirit", 4);
            //runeDict.Add("LingeringSpirit", 1);
            //runeDict.Add("GraspingSpirit", 2);
            //runeDict.Add("DrainingSpirit", 3);

            bool hasResentfulSpirit = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Haunt && s.RuneIndex == 4);

            // Haunt the shit out of monster and maybe they will give you treats
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Haunt) && 
                !Player.IsIncapacitated && Player.PrimaryResource >= 98 &&
                !SpellTracker.IsUnitTracked(CurrentTarget, SNOPower.Witchdoctor_Haunt) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Haunt))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Haunt, 21f, Vector3.Zero, -1, CurrentTarget.ACDGuid, 1, 1, WAIT_FOR_ANIM);
            }

            //skillDict.Add("LocustSwarm", SNOPower.Witchdoctor_Locust_Swarm);

            // Locust
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Locust_Swarm) && !Player.IsIncapacitated && Player.PrimaryResource >= 196 &&
                !SpellTracker.IsUnitTracked(CurrentTarget, SNOPower.Witchdoctor_Locust_Swarm) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Locust_Swarm) && LastPowerUsed != SNOPower.Witchdoctor_Locust_Swarm)
            {
                return new TrinityPower(SNOPower.Witchdoctor_Locust_Swarm, 12f, Vector3.Zero, -1, CurrentTarget.ACDGuid, 1, 1, WAIT_FOR_ANIM);
            }

            // Sacrifice for 0 Dogs
            if (!UseOOCBuff && Hotbar.Contains(SNOPower.Witchdoctor_Sacrifice) &&
                Settings.Combat.WitchDoctor.ZeroDogs &&
                PowerManager.CanCast(SNOPower.Witchdoctor_Sacrifice))
            {
                return new TrinityPower(SNOPower.Witchdoctor_Sacrifice, 9f, Vector3.Zero, CurrentWorldDynamicId, -1, 1, 2, WAIT_FOR_ANIM);
            }

            // Wall of Zombies
            if (!UseOOCBuff && !IsCurrentlyAvoiding && !Player.IsIncapacitated && Hotbar.Contains(SNOPower.Witchdoctor_WallOfZombies) && !Player.IsIncapacitated &&
                (ElitesWithinRange[RANGE_15] > 0 || AnythingWithinRange[RANGE_15] > 3 || ((CurrentTarget.IsEliteRareUnique || CurrentTarget.IsTreasureGoblin || CurrentTarget.IsBoss) && CurrentTarget.RadiusDistance <= 25f)) &&
                Player.PrimaryResource >= 103 && PowerManager.CanCast(SNOPower.Witchdoctor_WallOfZombies))
            {
                return new TrinityPower(SNOPower.Witchdoctor_WallOfZombies, 25f, CurrentTarget.Position, CurrentWorldDynamicId, -1, 1, 1, WAIT_FOR_ANIM);
            }

            var zombieChargerRange = hasGraveInjustice ? Math.Min(Player.GoldPickupRadius + 8f, 11f) : 11f;

            // Zombie Charger aka Zombie bears Spams Bears @ Everything from 11feet away
            if (!UseOOCBuff && !IsCurrentlyAvoiding && !Player.IsIncapacitated && Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 140 &&
                TargetUtil.AnyMobsInRange(zombieChargerRange) &&
                PowerManager.CanCast(SNOPower.Witchdoctor_ZombieCharger))
            {
                return new TrinityPower(SNOPower.Witchdoctor_ZombieCharger, zombieChargerRange, CurrentTarget.Position, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            //skillDict.Add("Firebats", SNOPower.Witchdoctor_Firebats);
            //runeDict.Add("DireBats", 0);
            //runeDict.Add("VampireBats", 3);
            //runeDict.Add("PlagueBats", 2);
            //runeDict.Add("HungryBats", 1);
            //runeDict.Add("CloudOfBats", 4);

            bool hasDireBats = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Firebats && s.RuneIndex == 0);
            bool hasVampireBats = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Firebats && s.RuneIndex == 3);
            bool hasPlagueBats = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Firebats && s.RuneIndex == 2);
            bool hasHungryBats = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Firebats && s.RuneIndex == 1);
            bool hasCloudOfBats = HotbarSkills.AssignedSkills.Any(s => s.Power == SNOPower.Witchdoctor_Firebats && s.RuneIndex == 4);

            int fireBatsMana = TimeSinceUse(SNOPower.Witchdoctor_Firebats) < 125 ? 66 : 220;

            // Fire Bats:Cloud of bats 
            if (!UseOOCBuff && !IsCurrentlyAvoiding && hasCloudOfBats && TargetUtil.AnyMobsInRange(8f) &&
                Hotbar.Contains(SNOPower.Witchdoctor_Firebats) && !Player.IsIncapacitated && Player.PrimaryResource >= fireBatsMana)
            {
                return new TrinityPower(SNOPower.Witchdoctor_Firebats, Settings.Combat.WitchDoctor.FirebatsRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 0, NO_WAIT_ANIM);
            }

            // Fire Bats fast-attack
            if (!UseOOCBuff && !Player.IsIncapacitated && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Firebats) && Player.PrimaryResource >= fireBatsMana &&
                 TargetUtil.AnyMobsInRange(Settings.Combat.WitchDoctor.FirebatsRange) && !hasCloudOfBats)
            {
                return new TrinityPower(SNOPower.Witchdoctor_Firebats, Settings.Combat.WitchDoctor.FirebatsRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 0, NO_WAIT_ANIM);
            }

            var acidCloudRange = hasGraveInjustice ? Math.Min(Player.GoldPickupRadius + 8f, 30f) : 30f;

            // Acid Cloud
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_AcidCloud) && !Player.IsIncapacitated &&
                Player.PrimaryResource >= 172 && PowerManager.CanCast(SNOPower.Witchdoctor_AcidCloud))
            {
                Vector3 bestClusterPoint;
                if (hasGraveInjustice)
                    bestClusterPoint = TargetUtil.GetBestClusterPoint(15f, Math.Min(Player.GoldPickupRadius + 8f, 30f));
                else
                    bestClusterPoint = TargetUtil.GetBestClusterPoint(15f, 30f);

                return new TrinityPower(SNOPower.Witchdoctor_AcidCloud, acidCloudRange, bestClusterPoint, CurrentWorldDynamicId, -1, 1, 1, WAIT_FOR_ANIM);
            }

            // Zombie Charger backup
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && !Player.IsIncapacitated && Player.PrimaryResource >= 140 &&
                PowerManager.CanCast(SNOPower.Witchdoctor_ZombieCharger))
            {
                return new TrinityPower(SNOPower.Witchdoctor_ZombieCharger, zombieChargerRange, CurrentTarget.Position, CurrentWorldDynamicId, -1, 0, 0, WAIT_FOR_ANIM);
            }

            // Spirit Barrage
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_SpiritBarrage) && !Player.IsIncapacitated && Player.PrimaryResource >= 108 &&
                PowerManager.CanCast(SNOPower.Witchdoctor_SpiritBarrage) && !hasManitou)
            {
                return new TrinityPower(SNOPower.Witchdoctor_SpiritBarrage, 21f, Vector3.Zero, -1, CurrentTarget.ACDGuid, 2, 2, WAIT_FOR_ANIM);
            }

            // Poison Darts fast-attack Spams Darts when mana is too low (to cast bears) @12yds or @10yds if Bears avialable
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_PoisonDart) && !Player.IsIncapacitated)
            {
                float fUseThisRange = 35f;
                if (Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 150)
                    fUseThisRange = 30f;
                return new TrinityPower(SNOPower.Witchdoctor_PoisonDart, fUseThisRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 2, WAIT_FOR_ANIM);
            }
            // Corpse Spiders fast-attacks Spams Spiders when mana is too low (to cast bears) @12yds or @10yds if Bears avialable
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_CorpseSpider) && !Player.IsIncapacitated)
            {
                float fUseThisRange = 35f;
                if (Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 150)
                    fUseThisRange = 30f;
                return new TrinityPower(SNOPower.Witchdoctor_CorpseSpider, fUseThisRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 1, WAIT_FOR_ANIM);
            }
            // Toads fast-attacks Spams Toads when mana is too low (to cast bears) @12yds or @10yds if Bears avialable
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_PlagueOfToads) && !Player.IsIncapacitated)
            {
                float fUseThisRange = 35f;
                if (Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 150)
                    fUseThisRange = 30f;
                return new TrinityPower(SNOPower.Witchdoctor_PlagueOfToads, fUseThisRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 1, WAIT_FOR_ANIM);
            }
            // Fire Bomb fast-attacks Spams Bomb when mana is too low (to cast bears) @12yds or @10yds if Bears avialable
            if (!UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Witchdoctor_Firebomb) && !Player.IsIncapacitated)
            {
                float fUseThisRange = 35f;
                if (Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 150)
                    fUseThisRange = 30f;
                return new TrinityPower(SNOPower.Witchdoctor_Firebomb, fUseThisRange, Vector3.Zero, -1, CurrentTarget.ACDGuid, 0, 1, WAIT_FOR_ANIM);
            }

            // Default attacks
            return CombatBase.DefaultPower;
        }

        private static TrinityPower GetWitchDoctorDestroyPower()
        {
            if (Hotbar.Contains(SNOPower.Witchdoctor_Sacrifice) && Settings.Combat.WitchDoctor.ZeroDogs)
                return new TrinityPower(SNOPower.Witchdoctor_Sacrifice, 12f, Vector3.Zero, -1, -1, 1, 2, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_Firebats))
                return new TrinityPower(SNOPower.Witchdoctor_Firebats, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_Firebomb))
                return new TrinityPower(SNOPower.Witchdoctor_Firebomb, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_PoisonDart))
                return new TrinityPower(SNOPower.Witchdoctor_PoisonDart, 15f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_ZombieCharger) && Player.PrimaryResource >= 140)
                return new TrinityPower(SNOPower.Witchdoctor_ZombieCharger, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_CorpseSpider))
                return new TrinityPower(SNOPower.Witchdoctor_CorpseSpider, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_PlagueOfToads))
                return new TrinityPower(SNOPower.Witchdoctor_PlagueOfToads, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            if (Hotbar.Contains(SNOPower.Witchdoctor_AcidCloud) && Player.PrimaryResource >= 172)
                return new TrinityPower(SNOPower.Witchdoctor_AcidCloud, 12f, Vector3.Zero, -1, -1, 0, 0, WAIT_FOR_ANIM);
            return CombatBase.DefaultPower;
        }

    }
}
