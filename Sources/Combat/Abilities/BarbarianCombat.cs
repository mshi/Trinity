﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trinity.Technicals;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Internals.Actors;

namespace Trinity.Combat.Abilities
{
    class BarbarianCombat : CombatBase
    {
        private static bool allowSprintOOC = true;

        public static TrinityPower GetPower()
        {
            TrinityPower power = null;

            if (UseDestructiblePower)
                power = DestroyObjectPower;

            if (UseOOCBuff)
            {
                // Sprint OOC
                if (IsNull(power) && CanCastSprintOOC)
                    power = PowerSprint;

                // Threatening Shout OOC

            }

            // Ignore Pain when low on health
            if (IsNull(power) && CanCastIgnorePain)
                power = PowerIgnorePain;

            IsWaitingForSpecial = false;

            // Check if we should conserve Fury for specials
            if (IsNull(power) && Player.PrimaryResource < MinEnergyReserve)
            {
                if (ShouldWaitForEarthquake)
                {
                    Logger.LogNormal("Waiting for Barbarian_Earthquake!");
                    IsWaitingForSpecial = true;
                }
                if (ShouldWaitForWrathOfTheBerserker)
                {
                    Logger.LogNormal("Waiting for Barbarian_WrathOfTheBerserker 1!");
                    IsWaitingForSpecial = true;
                }
                if (ShouldWaitForCallOfTheAncients)
                {
                    Logger.LogNormal("Waiting for Barbarian_CallOfTheAncients!");
                    IsWaitingForSpecial = true;
                }
            }

            // Earthquake
            if (IsNull(power) && CanCastEarthquake)
                power = PowerEarthquake;

            // WOTB
            if (IsNull(power) && CanCastWrathOfTheBerserker)
            {
                Logger.Log(TrinityLogLevel.Verbose, LogCategory.UserInformation, "Barbarian_WrathOfTheBerserker being used!({0})", CurrentTarget.InternalName);
                power = PowerWrathOfTheBerserker;
            }

            // Call of the Ancients
            if (IsNull(power) && CanCastCallOfTheAncients)
                power = PowerCallOfTheAncients;

            // Battle Rage
            if (IsNull(power) && CanCastBattleRage)
                power = PowerBattleRage;

            // War Cry
            if (IsNull(power) && CanUseWarCry)
                power = PowerWarCry;

            // Threatening Shout
            if (IsNull(power) && CanUseThreatingShout)
                power = PowerThreateningShout;

            // Ground Stomp
            if (IsNull(power) && CanUseGroundStomp)
                power = PowerGroundStomp;

            // Revenge
            if (IsNull(power) && CanUseRevenge)
                power = PowerRevenge;

            // Furious Charge
            if (IsNull(power) && CanUseFuriousCharge)
                power = PowerFuriousCharge;

            // Leap
            if (IsNull(power) && CanUseLeap)
                power = PowerLeap;

            // Rend
            if (IsNull(power) && CanUseRend)
                power = PowerRend;

            // Overpower
            if (IsNull(power) && CanUseOverPower)
                power = PowerOverpower;

            // Seismic Slam
            if (IsNull(power) && CanUseSeismicSlam)
                power = PowerSeismicSlam;

            // Ancient Spear
            if (IsNull(power) && CanUseAncientSpear)
                power = PowerAncientSpear;

            // Sprint
            if (IsNull(power) && CanUseSprint)
                power = PowerSprint;

            // Frenzy to 5 stacks
            if (IsNull(power) && CanUseFrenzyTo5)
                power = PowerFrenzy;

            // Whirlwind
            if (IsNull(power) && CanUseWhirlwind)
                power = PowerWhirlwind;

            // Battle Rage
            if (IsNull(power) && CanUseBattleRage)
                power = PowerBattleRage;

            // Hammer of the Ancients
            if (IsNull(power) && CanUseHammerOfTheAncients)
                power = PowerHammerOfTheAncients;

            // Weapon Throw
            if (IsNull(power) && CanUseWeaponThrow)
                power = PowerWeaponThrow;

            // Frenzy Fury Generator
            if (IsNull(power) && CanUseFrenzy)
                power = PowerFrenzy;

            // Bash Fury Generator
            if (IsNull(power) && CanUseBash)
                power = PowerBash;

            // Cleave Fury Generator
            if (IsNull(power) && CanUseCleave)
                power = PowerCleave;

            // Default Attacks
            if (IsNull(power))
                power = CombatBase.DefaultPower;

            return power;

        }


        public static bool CanCastIgnorePain
        {
            get
            {
                return
                    !UseOOCBuff &&
                    CanCast(SNOPower.Barbarian_IgnorePain) &&
                    Player.CurrentHealthPct <= V.F("Barbarian.IgnorePain.MinHealth");
            }
        }
        public static bool ShouldWaitForEarthquake
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !IsCurrentlyAvoiding &&
                    CanCast(SNOPower.Barbarian_Earthquake) &&
                    TargetUtil.AnyElitesInRange(25) &&
                    !GetHasBuff(SNOPower.Barbarian_Earthquake) &&
                    Player.PrimaryResource <= 50; ;
            }
        }
        public static bool CanCastEarthquake
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !IsCurrentlyAvoiding &&
                    !Player.IsIncapacitated &&
                    CanCast(SNOPower.Barbarian_Earthquake) &&
                    !GetHasBuff(SNOPower.Barbarian_Earthquake) &&
                    TargetUtil.IsEliteTargetInRange(13f) &&
                    Player.PrimaryResource > 50;
            }
        }
        public static bool ShouldWaitForWrathOfTheBerserker
        {
            get
            {
                // WOTB with elites
                bool wotbElites =
                    (WOTBGoblins || WOTBElitesPresent);

                return
                    !UseOOCBuff &&
                    !IsCurrentlyAvoiding &&
                    Player.PrimaryResource <= 50 &&
                    CanCast(SNOPower.Barbarian_WrathOfTheBerserker) &&
                    !GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker) &&
                    (WOTBIgnoreElites || wotbElites);
            }
        }

        /// <summary>
        /// If using WOTB on all elites, or if we should only use on "hard" affixes
        /// </summary>
        public static bool WOTBElitesPresent
        {
            get
            {
                // WotB only used on Arcane, Frozen, Jailer, Molten, Electrified+Reflect Damage elites, or bosses and ubers, or when more than 4 elites are present
                bool wotbHardElitesPresent = HardElitesPresent ||
                    Trinity.ObjectCache.Any(o => DataDictionary.ForceUseWOTBIds.Contains(o.ActorSNO)) ||
                        TargetUtil.AnyElitesInRange(V.F("Barbarian.WOTB.HardEliteRangeOverride"), V.I("Barbarian.WOTB.HardEliteCountOverride"));

                return
                    (!Settings.Combat.Barbarian.WOTBHardOnly && TargetUtil.AnyElitesInRange(V.F("Barbarian.WOTB.MinRange"), V.I("Barbarian.WOTB.MinCount")))
                    || (wotbHardElitesPresent && Settings.Combat.Barbarian.WOTBHardOnly);
            }
        }

        /// <summary>
        /// Make sure we are allowed to use wrath on goblins, else make sure this isn't a goblin
        /// </summary>
        public static bool WOTBGoblins
        {
            get
            {
                return !Settings.Combat.Barbarian.UseWOTBGoblin || (Settings.Combat.Barbarian.UseWOTBGoblin && CurrentTarget.IsTreasureGoblin);
            }
        }

        /// <summary>
        /// If ignoring elites completely, trigger on 3 trash within 25 yards, or 10 trash in 50 yards
        /// </summary>
        public static bool WOTBIgnoreElites
        {
            get
            {
                return
                    Settings.Combat.Misc.IgnoreElites &&
                    (TargetUtil.AnyMobsInRange(V.F("Barbarian.WOTB.RangeNear"), V.I("Barbarian.WOTB.CountNear")) ||
                    TargetUtil.AnyMobsInRange(V.F("Barbarian.WOTB.RangeFar"), V.I("Barbarian.WOTB.CountFar")) ||
                    TargetUtil.AnyMobsInRange(Settings.Combat.Misc.TrashPackClusterRadius, Settings.Combat.Misc.TrashPackSize)) ||
                    !Settings.Combat.Misc.IgnoreElites;
            }
        }

        public static bool CanCastWrathOfTheBerserker
        {
            get
            {
                /* WOTB should be used when the following conditions are met:
                 * If ignoring elites, when 3 monsters in 25 yards or 10 monsters in 50 yards are present, OR
                 * If using on hard elites only, when an elite with the required affix is present, OR
                 * If normal mode, when any elite is within 20 yards, OR
                 * If we have low health (use potion health)
                 * And not on the Heart of sin
                 */

                return
                    !UseOOCBuff &&
                    Player.PrimaryResource >= V.I("Barbarian.WOTB.MinFury") &&
                    // Don't still have the buff
                    !GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker) &&
                    CanCast(SNOPower.Barbarian_WrathOfTheBerserker) &&
                    // Only with Battle Rage
                    (Hotbar.Contains(SNOPower.Barbarian_BattleRage) && GetHasBuff(SNOPower.Barbarian_BattleRage) || !Hotbar.Contains(SNOPower.Barbarian_BattleRage)) &&
                    // Not on heart of sin after Cydaea
                    CurrentTarget.ActorSNO != 193077 &&
                    (WOTBGoblins || WOTBIgnoreElites || WOTBElitesPresent ||
                    //Or if our health is low
                    Player.CurrentHealthPct <= Settings.Combat.Barbarian.PotionLevel);
            }
        }
        public static bool ShouldWaitForCallOfTheAncients
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !IsCurrentlyAvoiding &&
                    CanCast(SNOPower.Barbarian_CallOfTheAncients) &&
                    TargetUtil.AnyElitesInRange(V.F("Barbarian.CallOfTheAncients.MinEliteRange")) &&
                    !GetHasBuff(SNOPower.Barbarian_CallOfTheAncients) &&
                    Player.PrimaryResource <= V.F("Barbarian.CallOfTheAncients.MinFury");
            }
        }
        public static bool CanCastCallOfTheAncients
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !IsCurrentlyAvoiding &&
                    CanCast(SNOPower.Barbarian_CallOfTheAncients) &&
                    !Player.IsIncapacitated &&
                    TargetUtil.AnyElitesInRange(V.F("Barbarian.CallOfTheAncients.MinEliteRange"));
            }
        }
        public static bool CanCastBattleRage
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !Player.IsIncapacitated &&
                    CanCast(SNOPower.Barbarian_BattleRage, CanCastFlags.NoTimer) &&
                    (SNOPowerUseTimer(SNOPower.Barbarian_BattleRage) || !GetHasBuff(SNOPower.Barbarian_BattleRage)) &&
                    Player.PrimaryResource >= V.F("Barbarian.BattleRage.MinFury");
            }
        }
        public static bool CanCastSprintOOC
        {
            get
            {
                return
                UseOOCBuff &&
                AllowSprintOOC &&
                !Player.IsIncapacitated &&
                CanCast(SNOPower.Barbarian_Sprint) &&
                (Settings.Combat.Misc.AllowOOCMovement || GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker)) &&
                !GetHasBuff(SNOPower.Barbarian_Sprint) &&
                Player.PrimaryResource >= V.F("Barbarian.Sprint.MinFury");
            }
        }
        public static bool CanUseWarCry
        {
            get
            {
                return
                    CanCast(SNOPower.Barbarian_WarCry, CanCastFlags.NoTimer) &&
                    !Player.IsIncapacitated &&
                    (Player.PrimaryResource <= V.F("Barbarian.WarCry.MaxFury") || !GetHasBuff(SNOPower.Barbarian_WarCry));
            }
        }
        public static bool CanUseThreatingShout
        {
            get
            {
                var range = V.F("Barbarian.ThreatShout.Range");

                bool inCombat = !UseOOCBuff &&
                    CanCast(SNOPower.Barbarian_ThreateningShout) &&
                    !Player.IsIncapacitated &&
                    ((TargetUtil.AnyMobsInRange(range, Settings.Combat.Barbarian.MinThreatShoutMobCount)) || TargetUtil.IsEliteTargetInRange(range)) &&
                    (
                        (Hotbar.Contains(SNOPower.Barbarian_Whirlwind) && Player.PrimaryResource <= V.I("Barbarian.Whirlwind.MinFury")) ||
                        (IsWaitingForSpecial && Player.PrimaryResource <= MinEnergyReserve)
                    );

                bool outOfCombat = UseOOCBuff &&
                     !Player.IsIncapacitated &&
                     Settings.Combat.Barbarian.ThreatShoutOOC && CanCast(SNOPower.Barbarian_ThreateningShout) &&
                     Player.PrimaryResource < V.D("Barbarian.ThreatShout.OOCMaxFury");

                return inCombat || outOfCombat;

            }
        }
        public static bool CanUseGroundStomp
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !Player.IsIncapacitated &&
                    CanCast(SNOPower.Barbarian_GroundStomp) &&
                    (
                        TargetUtil.AnyElitesInRange(V.F("Barbarian.GroundStomp.EliteRange"), V.I("Barbarian.GroundStomp.EliteCount")) ||
                        TargetUtil.AnyMobsInRange(V.F("Barbarian.GroundStomp.TrashRange"), V.I("Barbarian.GroundStomp.TrashCount")) ||
                        Player.CurrentHealthPct <= V.F("Barbarian.GroundStomp.UseBelowHealthPct")
                    );
            }
        }
        public static bool CanUseRevenge
        {
            get
            {
                return
                    !UseOOCBuff &&
                    CanCast(SNOPower.Barbarian_Revenge) &&
                    !Player.IsIncapacitated &&
                    // Don't use revenge on goblins, too slow!
                    (!CurrentTarget.IsTreasureGoblin || TargetUtil.AnyMobsInRange(V.F("Barbarian.Revenge.TrashRange"), V.I("Barbarian.Revenge.TrashCount")));
            }
        }
        public static bool CanUseFuriousCharge
        {
            get
            {
                return
                    !UseOOCBuff &&
                    CanCast(SNOPower.Barbarian_FuriousCharge) &&
                    (TargetUtil.AnyElitesInRange(V.F("Barbarian.FuriousCharge.EliteRange"), V.I("Barbarian.FuriousCharge.EliteCount")) ||
                    TargetUtil.AnyElitesInRange(V.F("Barbarian.FuriousCharge.TrashRange"), V.I("Barbarian.FuriousCharge.TrashCount")));
            }
        }
        public static bool CanUseLeap
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !Player.IsIncapacitated &&
                    CanCast(SNOPower.Barbarian_Leap) &&
                    (TargetUtil.AnyMobsInRange(V.F("Barbarian.Leap.TrashRange"), V.I("Barbarian.Leap.TrashCount")) ||
                    TargetUtil.AnyElitesInRange(V.F("Barbarian.Leap.EliteRange"), V.I("Barbarian.Leap.EliteCount")));
            }
        }
        public static bool CanUseRend
        {
            get
            {
                return
                    !UseOOCBuff &&
                    !Player.IsIncapacitated &&
                    CanCast(SNOPower.Barbarian_Rend) &&
                    TargetUtil.AnyMobsInRange(V.F("Barbarian.Rend.MaxRange")) && !CurrentTarget.IsTreasureGoblin &&
                    ((!IsWaitingForSpecial && Player.PrimaryResource >= V.I("Barbarian.Rend.MinFury")) || (IsWaitingForSpecial && Player.PrimaryResource > MinEnergyReserve)) &&
                    (Trinity.ObjectCache.Count(o => o.Type == GObjectType.Unit && !o.HasDotDPS && o.RadiusDistance <= V.F("Barbarian.Rend.MaxRange")) >= V.I("Barbarian.Rend.MinNonBleedMobCount") || !CurrentTarget.HasDotDPS) &&
                    (TimeSinceUse(SNOPower.Barbarian_Rend) > V.I("Barbarian.Rend.MinUseIntervalMillseconds")) &&
                    Trinity.LastPowerUsed != SNOPower.Barbarian_Rend;
            }
        }
        public static bool CanUseOverPower
        {
            get
            {
                return !UseOOCBuff && CanCast(SNOPower.Barbarian_Overpower) && !Player.IsIncapacitated &&
                    (Hotbar.Contains(SNOPower.Barbarian_BattleRage) && GetHasBuff(SNOPower.Barbarian_BattleRage) || !Hotbar.Contains(SNOPower.Barbarian_BattleRage)) &&
                    (CurrentTarget.RadiusDistance <= V.F("Barbarian.OverPower.MaxRange") || 
                        (
                        TargetUtil.AnyMobsInRange(V.F("Barbarian.OverPower.MaxRange")) &&
                        (CurrentTarget.IsEliteRareUnique || CurrentTarget.IsMinion || CurrentTarget.IsBoss || GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker) ||
                        (CurrentTarget.IsTreasureGoblin && CurrentTarget.CentreDistance <= V.F("Barbarian.OverPower.MaxRange")) || Hotbar.Contains(SNOPower.Barbarian_SeismicSlam))
                        )
                    );
            }
        }
        public static bool CanUseSeismicSlam
        {
            get
            {
                return !UseOOCBuff && !IsWaitingForSpecial && CanCast(SNOPower.Barbarian_SeismicSlam) && !Player.IsIncapacitated &&
                    (!Hotbar.Contains(SNOPower.Barbarian_BattleRage) || (Hotbar.Contains(SNOPower.Barbarian_BattleRage) && GetHasBuff(SNOPower.Barbarian_BattleRage))) &&
                    Player.PrimaryResource >= V.I("Barbarian.SeismicSlam.MinFury") && CurrentTarget.CentreDistance <= V.F("Barbarian.SeismicSlam.CurrentTargetRange") && 
                    (TargetUtil.AnyMobsInRange(V.F("Barbarian.SeismicSlam.TrashRange")) ||
                     TargetUtil.IsEliteTargetInRange(V.F("Barbarian.SeismicSlam.EliteRange")));
            }
        }
        public static bool CanUseAncientSpear
        {
            get
            {
                return !UseOOCBuff && !IsCurrentlyAvoiding && CanCast(SNOPower.Barbarian_AncientSpear) &&
                    CurrentTarget.HitPointsPct >= V.F("Barbarian.AncientSpear.MinHealthPct");
            }
        }
        public static bool CanUseSprint
        {
            get
            {
                return !UseOOCBuff && CanCast(SNOPower.Barbarian_Sprint) && !Player.IsIncapacitated &&
                    (
                        // last power used was whirlwind and we don't have sprint up
                        (LastPowerUsed == SNOPower.Barbarian_Whirlwind && !GetHasBuff(SNOPower.Barbarian_Sprint)) ||
                        // Fury Dump Options for sprint: use at max energy constantly
                        (Settings.Combat.Barbarian.FuryDumpWOTB && Player.PrimaryResourcePct >= 0.95 && GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker)) ||
                        (Settings.Combat.Barbarian.FuryDumpAlways && Player.PrimaryResourcePct >= 0.95) ||
                        // or on a timer
                        (
                         (SNOPowerUseTimer(SNOPower.Barbarian_Sprint) && !GetHasBuff(SNOPower.Barbarian_Sprint)) &&
                         // Always keep up if we are whirlwinding, if the target is a goblin, or if we are more than 16 feet away from the target
                         (Hotbar.Contains(SNOPower.Barbarian_Whirlwind) || CurrentTarget.IsTreasureGoblin || 
                          (CurrentTarget.CentreDistance >= V.F("Barbarian.Sprint.SingleTargetRange") && Player.PrimaryResource >= V.F("Barbarian.Sprint.SingleTargetMinFury"))
                         )
                        )
                    ) &&
                    // If they have battle-rage, make sure it's up
                    (!Hotbar.Contains(SNOPower.Barbarian_BattleRage) || (Hotbar.Contains(SNOPower.Barbarian_BattleRage) && GetHasBuff(SNOPower.Barbarian_BattleRage))) &&
                    // Check for minimum energy
                    Player.PrimaryResource >= V.F("Barbarian.Sprint.MinFury");
            }
        }
        public static bool CanUseFrenzyTo5
        {
            get
            {
                return !UseOOCBuff && !IsCurrentlyAvoiding && !Player.IsRooted && Hotbar.Contains(SNOPower.Barbarian_Frenzy) &&
                    !TargetUtil.AnyMobsInRange(15f, 3) && GetBuffStacks(SNOPower.Barbarian_Frenzy) < 5;
            }
        }
        public static bool CanUseWhirlwind
        {
            get
            {
                return !UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Barbarian_Whirlwind) && !Player.IsIncapacitated && !Player.IsRooted &&
                    (!IsWaitingForSpecial || (IsWaitingForSpecial && Player.PrimaryResource > MinEnergyReserve)) &&
                    //(!IsWaitingForSpecial || (IsWaitingForSpecial && !(TargetUtil.AnyMobsInRange(3, 15) || ForceCloseRangeTarget))) && // make sure we're not surrounded if waiting for special
                    // Don't WW against goblins, units in the special SNO list
                    (!Settings.Combat.Barbarian.SelectiveWhirlwind || (Settings.Combat.Barbarian.SelectiveWhirlwind && !DataDictionary.WhirlwindIgnoreSNOIds.Contains(CurrentTarget.ActorSNO))) &&
                    // Only if within 25 yards of main target
                    ((CurrentTarget.RadiusDistance <= 25f || TargetUtil.AnyMobsInRange(V.F("Barbarian.Whirlwind.TrashRange"), V.I("Barbarian.Whirlwind.TrashCount")))) &&
                    (TargetUtil.AnyMobsInRange(50, 2) || CurrentTarget.HitPointsPct >= 0.30 || CurrentTarget.IsBossOrEliteRareUnique || Player.CurrentHealthPct <= 0.60) &&
                    // Check for energy reservation amounts
                    //((playerStatus.dCurrentEnergy >= 20 && !playerStatus.bWaitingForReserveEnergy) || playerStatus.dCurrentEnergy >= iWaitingReservedAmount) &&
                    Player.PrimaryResource >= V.D("Barbarian.Whirlwind.MinFury") &&
                    // If they have battle-rage, make sure it's up
                    (!Hotbar.Contains(SNOPower.Barbarian_BattleRage) || (Hotbar.Contains(SNOPower.Barbarian_BattleRage) && GetHasBuff(SNOPower.Barbarian_BattleRage)));
            }
        }
        public static bool CanUseBattleRage
        {
            get
            {
                return !UseOOCBuff && Hotbar.Contains(SNOPower.Barbarian_BattleRage) && !Player.IsIncapacitated &&
                    // Fury Dump Options for battle rage IF they don't have sprint 
                    (
                    (Settings.Combat.Barbarian.FuryDumpWOTB && Player.PrimaryResourcePct >= 0.99 && GetHasBuff(SNOPower.Barbarian_WrathOfTheBerserker)) ||
                    (Settings.Combat.Barbarian.FuryDumpAlways && Player.PrimaryResourcePct >= 0.99) || !GetHasBuff(SNOPower.Barbarian_BattleRage)
                    ) &&
                    Player.PrimaryResource >= 20 && PowerManager.CanCast(SNOPower.Barbarian_BattleRage);
            }
        }
        public static bool CanUseHammerOfTheAncients
        {
            get
            {
                return !UseOOCBuff && !IsCurrentlyAvoiding && !Player.IsIncapacitated && !IsWaitingForSpecial && Hotbar.Contains(SNOPower.Barbarian_HammerOfTheAncients) &&
                    Player.PrimaryResource >= 20;
            }
        }
        public static bool CanUseWeaponThrow
        {
            get
            {
                return !UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Barbarian_WeaponThrow)
                    && (Player.PrimaryResource >= 10 && (CurrentTarget.RadiusDistance >= 5f || BarbHasNoPrimary()));
            }
        }
        public static bool CanUseFrenzy { get { return !UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Barbarian_Frenzy); } }
        public static bool CanUseBash { get { return !UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Barbarian_Bash); } }
        public static bool CanUseCleave { get { return !UseOOCBuff && !IsCurrentlyAvoiding && Hotbar.Contains(SNOPower.Barbarian_Cleave); } }

        public static TrinityPower PowerIgnorePain { get { return new TrinityPower(SNOPower.Barbarian_IgnorePain); } }
        public static TrinityPower PowerEarthquake { get { return new TrinityPower(SNOPower.Barbarian_Earthquake); } }
        public static TrinityPower PowerWrathOfTheBerserker { get { return new TrinityPower(SNOPower.Barbarian_WrathOfTheBerserker); } }
        public static TrinityPower PowerCallOfTheAncients { get { return new TrinityPower(SNOPower.Barbarian_CallOfTheAncients, V.I("Barbarian.CallOfTheAncients.TickDelay"), V.I("Barbarian.CallOfTheAncients.TickDelay")); } }
        public static TrinityPower PowerBattleRage { get { return new TrinityPower(SNOPower.Barbarian_BattleRage); } }
        public static TrinityPower PowerSprint { get { return new TrinityPower(SNOPower.Barbarian_Sprint, 0, 0); } }
        public static TrinityPower PowerWarCry { get { return new TrinityPower(SNOPower.Barbarian_WarCry); } }
        public static TrinityPower PowerThreateningShout { get { return new TrinityPower(SNOPower.Barbarian_ThreateningShout); } }
        public static TrinityPower PowerGroundStomp { get { return new TrinityPower(SNOPower.Barbarian_GroundStomp); } }
        public static TrinityPower PowerRevenge { get { return new TrinityPower(SNOPower.Barbarian_Revenge); } }
        public static TrinityPower PowerFuriousCharge
        {
            get
            {
                float extraDistance;
                if (CurrentTarget.CentreDistance <= 25)
                    extraDistance = 30;
                else
                    extraDistance = (25 - CurrentTarget.CentreDistance);
                if (extraDistance < V.F("Barbarian.FuriousCharge.MinExtraTargetDistance"))
                    extraDistance = V.F("Barbarian.FuriousCharge.MinExtraTargetDistance");
                Vector3 vNewTarget = MathEx.CalculatePointFrom(CurrentTarget.Position, Player.Position, CurrentTarget.CentreDistance + extraDistance);
                return new TrinityPower(SNOPower.Barbarian_FuriousCharge, V.F("Barbarian.FuriousCharge.UseRange"), vNewTarget);
            }
        }
        public static TrinityPower PowerLeap
        {
            get
            {
                float extraDistance = CurrentTarget.Radius;
                if (extraDistance <= V.F("Barbarian.Leap.MinExtraDistance"))
                    extraDistance = V.F("Barbarian.Leap.MinExtraDistance");
                if (CurrentTarget.CentreDistance + extraDistance > 35f)
                    extraDistance = 35 - CurrentTarget.CentreDistance;

                Vector3 vNewTarget = MathEx.CalculatePointFrom(CurrentTarget.Position, Player.Position, CurrentTarget.CentreDistance + extraDistance);
                return new TrinityPower(SNOPower.Barbarian_Leap, V.F("Barbarian.Leap.UseRange"), vNewTarget);
            }
        }
        public static TrinityPower PowerRend { get { return new TrinityPower(SNOPower.Barbarian_Rend, V.I("Barbarian.Rend.TickDelay"), V.I("Barbarian.Rend.TickDelay")); } }
        public static TrinityPower PowerOverpower { get { return new TrinityPower(SNOPower.Barbarian_Overpower); } }
        public static TrinityPower PowerSeismicSlam { get { return new TrinityPower(SNOPower.Barbarian_SeismicSlam, V.F("Barbarian.SeismicSlam.UseRange"), CurrentTarget.ACDGuid); } }
        public static TrinityPower PowerAncientSpear { get { return new TrinityPower(SNOPower.Barbarian_AncientSpear, V.F("Barbarian.AncientSpear.UseRange"), CurrentTarget.Position); } }
        public static TrinityPower PowerWhirlwind
        {
            get
            {
                bool shouldGetNewZigZag =
                    (DateTime.Now.Subtract(Trinity.LastChangedZigZag).TotalMilliseconds >= V.I("Barbarian.Whirlwind.ZigZagMaxTime") ||
                    CurrentTarget.ACDGuid != Trinity.LastZigZagUnitAcdGuid ||
                    ZigZagPosition.Distance2D(Player.Position) <= 5f);

                if (shouldGetNewZigZag)
                {
                    var wwdist = V.F("Barbarian.Whirlwind.ZigZagDistance");

                    ZigZagPosition = TargetUtil.GetZigZagTarget(CurrentTarget.Position, wwdist);

                    Trinity.LastZigZagUnitAcdGuid = CurrentTarget.ACDGuid;
                    Trinity.LastChangedZigZag = DateTime.Now;
                }
                return new TrinityPower(SNOPower.Barbarian_Whirlwind, V.F("Barbarian.Whirlwind.UseRange"), ZigZagPosition, Trinity.CurrentWorldDynamicId, -1, 0, 1, false);
            }
        }
        public static TrinityPower PowerHammerOfTheAncients { get { return new TrinityPower(SNOPower.Barbarian_HammerOfTheAncients, V.F("Barbarian.HammerOfTheAncients.UseRange"), CurrentTarget.ACDGuid); } }
        public static TrinityPower PowerWeaponThrow { get { return new TrinityPower(SNOPower.Barbarian_WeaponThrow, V.F("Barbarian.WeaponThrow.UseRange"), CurrentTarget.ACDGuid); } }
        public static TrinityPower PowerFrenzy { get { return new TrinityPower(SNOPower.Barbarian_Frenzy, V.F("Barbarian.Frenzy.UseRange"), CurrentTarget.ACDGuid); } }
        public static TrinityPower PowerBash { get { return new TrinityPower(SNOPower.Barbarian_Bash, V.F("Barbarian.Bash.UseRange"), CurrentTarget.ACDGuid); } }
        public static TrinityPower PowerCleave { get { return new TrinityPower(SNOPower.Barbarian_Cleave, V.F("Barbarian.Cleave.UseRange"), CurrentTarget.ACDGuid); } }

        private static TrinityPower DestroyObjectPower
        {
            get
            {
                if (Hotbar.Contains(SNOPower.Barbarian_Whirlwind) && Player.PrimaryResource > MinEnergyReserve)
                    return new TrinityPower(SNOPower.Barbarian_Whirlwind, V.F("Barbarian.Whirlwind.UseRange"), LastZigZagLocation);

                if (Hotbar.Contains(SNOPower.Barbarian_Frenzy))
                    return new TrinityPower(SNOPower.Barbarian_Frenzy, V.F("Barbarian.Frenzy.UseRange"));

                if (Hotbar.Contains(SNOPower.Barbarian_Bash))
                    return new TrinityPower(SNOPower.Barbarian_Bash, V.F("Barbarian.Bash.UseRange"));

                if (Hotbar.Contains(SNOPower.Barbarian_Cleave))
                    return new TrinityPower(SNOPower.Barbarian_Cleave, V.F("Barbarian.Cleave.UseRange"));

                if (Hotbar.Contains(SNOPower.Barbarian_Rend) && Player.PrimaryResourcePct >= 0.65)
                    return new TrinityPower(SNOPower.Barbarian_Rend, V.F("Barbarian.Rend.UseRange"));

                if (Hotbar.Contains(SNOPower.Barbarian_WeaponThrow) && Player.PrimaryResource >= 20)
                    return new TrinityPower(SNOPower.Barbarian_WeaponThrow, V.F("Barbarian.WeaponThrow.UseRange"));

                return CombatBase.DefaultPower;
            }
        }

        public static bool AllowSprintOOC
        {
            get { return allowSprintOOC; }
            set { allowSprintOOC = value; }
        }
        private static bool BarbHasNoPrimary()
        {
            return !(Hotbar.Contains(SNOPower.Barbarian_Frenzy) ||
                Hotbar.Contains(SNOPower.Barbarian_Bash) ||
                Hotbar.Contains(SNOPower.Barbarian_Cleave));
        }

    }
}
