using GilesTrinity.Technicals;
using System;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using GilesTrinity.Settings.Combat;

namespace GilesTrinity
{
    public partial class GilesTrinity : IPlugin
    {
        private static TrinityPower GetMonkPower(bool bCurrentlyAvoiding, bool bOOCBuff, bool bDestructiblePower)
        {
            // Monks need 80 for special spam like tempest rushing
            MinEnergyReserve = 80;

            // 4 Mantras for the initial buff (slow-use)
            if (Hotbar.Contains(SNOPower.Monk_MantraOfEvasion) && !GetHasBuff(SNOPower.Monk_MantraOfEvasion) &&
                PlayerStatus.PrimaryResource >= 50 && GilesUseTimer(SNOPower.Monk_MantraOfEvasion, true))
            {
                return new TrinityPower(SNOPower.Monk_MantraOfEvasion, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY);
            }
            if (Hotbar.Contains(SNOPower.Monk_MantraOfConviction) && !GetHasBuff(SNOPower.Monk_MantraOfConviction) &&
                (PlayerStatus.PrimaryResource >= 50 && PlayerStatus.PrimaryResource >= 85) && GilesUseTimer(SNOPower.Monk_MantraOfConviction, true))
            {
                return new TrinityPower(SNOPower.Monk_MantraOfConviction, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY);
            }
            if (Hotbar.Contains(SNOPower.Monk_MantraOfHealing) && !GetHasBuff(SNOPower.Monk_MantraOfHealing) &&
                PlayerStatus.PrimaryResource >= 50 && GilesUseTimer(SNOPower.Monk_MantraOfHealing, true))
            {
                return new TrinityPower(SNOPower.Monk_MantraOfHealing, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY);
            }
            if (Hotbar.Contains(SNOPower.Monk_MantraOfRetribution) && !GetHasBuff(SNOPower.Monk_MantraOfRetribution) &&
                PlayerStatus.PrimaryResource >= 50 && GilesUseTimer(SNOPower.Monk_MantraOfRetribution, true))
            {
                return new TrinityPower(SNOPower.Monk_MantraOfRetribution, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY);
            }
            // Mystic ally
            if (Hotbar.Contains(SNOPower.Monk_MysticAlly) && PlayerStatus.PrimaryResource >= 90 && iPlayerOwnedMysticAlly == 0 &&
                GilesUseTimer(SNOPower.Monk_MysticAlly) && PowerManager.CanCast(SNOPower.Monk_MysticAlly))
            {
                return new TrinityPower(SNOPower.Monk_MysticAlly, 0f, vNullLocation, iCurrentWorldID, -1, 2, 2, USE_SLOWLY);
            }
            // InnerSanctuary
            if (!bOOCBuff && PlayerStatus.CurrentHealthPct <= 0.45 && Hotbar.Contains(SNOPower.Monk_InnerSanctuary) &&
                GilesUseTimer(SNOPower.Monk_InnerSanctuary, true) &&
                PlayerStatus.PrimaryResource >= 30 && PowerManager.CanCast(SNOPower.Monk_InnerSanctuary))
            {
                return new TrinityPower(SNOPower.Monk_InnerSanctuary, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
            }
            // Serenity if health is low
            if ((PlayerStatus.CurrentHealthPct <= 0.50 || (PlayerStatus.IsIncapacitated && PlayerStatus.CurrentHealthPct <= 0.90)) && Hotbar.Contains(SNOPower.Monk_Serenity) &&
                GilesUseTimer(SNOPower.Monk_Serenity, true) &&
                PlayerStatus.PrimaryResource >= 10 && PowerManager.CanCast(SNOPower.Monk_Serenity))
            {
                return new TrinityPower(SNOPower.Monk_Serenity, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
            }
            // Breath of heaven when needing healing or the buff
            if (!bOOCBuff && (PlayerStatus.CurrentHealthPct <= 0.6 || !GetHasBuff(SNOPower.Monk_BreathOfHeaven)) && Hotbar.Contains(SNOPower.Monk_BreathOfHeaven) &&
                (PlayerStatus.PrimaryResource >= 35 || (!Hotbar.Contains(SNOPower.Monk_Serenity) && PlayerStatus.PrimaryResource >= 25)) &&
                GilesUseTimer(SNOPower.Monk_BreathOfHeaven) && PowerManager.CanCast(SNOPower.Monk_BreathOfHeaven))
            {
                return new TrinityPower(SNOPower.Monk_BreathOfHeaven, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
            }
            // Blinding Flash
            if (!bOOCBuff && PlayerStatus.PrimaryResource >= 20 && Hotbar.Contains(SNOPower.Monk_BlindingFlash) &&
                (
                    ElitesWithinRange[RANGE_15] >= 1 ||
                    PlayerStatus.CurrentHealthPct <= 0.4 ||
                    (AnythingWithinRange[RANGE_15] >= 3) ||
                    (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 15f) ||
                // as pre-sweeping wind buff
                    (AnythingWithinRange[RANGE_15] >= 1 && Hotbar.Contains(SNOPower.Monk_SweepingWind) && !GetHasBuff(SNOPower.Monk_SweepingWind) && Settings.Combat.Monk.HasInnaSet)
                ) &&
                // Check if either we don't have sweeping winds, or we do and it's ready to cast in a moment
                (CheckAbilityAndBuff(SNOPower.Monk_SweepingWind) ||
                 (Hotbar.Contains(SNOPower.Monk_SweepingWind) && !GetHasBuff(SNOPower.Monk_SweepingWind) &&
                 (Settings.Combat.Monk.HasInnaSet ? PlayerStatus.PrimaryResource >= 15 : PlayerStatus.PrimaryResource >= 85)) ||
                 PlayerStatus.CurrentHealthPct <= 0.25) &&
                GilesUseTimer(SNOPower.Monk_BlindingFlash) && PowerManager.CanCast(SNOPower.Monk_BlindingFlash))
            {
                return new TrinityPower(SNOPower.Monk_BlindingFlash, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY); 
            }
            // Blinding Flash as a DEFENSE
            if (!bOOCBuff && PlayerStatus.PrimaryResource >= 10 && Hotbar.Contains(SNOPower.Monk_BlindingFlash) &&
                PlayerStatus.CurrentHealthPct <= 0.25 && AnythingWithinRange[RANGE_15] >= 1 &&
                GilesUseTimer(SNOPower.Monk_BlindingFlash) && PowerManager.CanCast(SNOPower.Monk_BlindingFlash))
            {
                return new TrinityPower(SNOPower.Monk_BlindingFlash, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY); 
            }


            // Sweeping wind
            if (!bOOCBuff && Hotbar.Contains(SNOPower.Monk_SweepingWind) && !GetHasBuff(SNOPower.Monk_SweepingWind) &&
                (ElitesWithinRange[RANGE_25] > 0 || AnythingWithinRange[RANGE_20] >= 3 || (AnythingWithinRange[RANGE_20] >= 1 && Settings.Combat.Monk.HasInnaSet) ||
                (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 25f)) &&
                // Check if either we don't have blinding flash, or we do and it's been cast in the last 8000ms
                (DateTime.Now.Subtract(dictAbilityLastUse[SNOPower.Monk_BlindingFlash]).TotalMilliseconds <= 8000 || CheckAbilityAndBuff(SNOPower.Monk_BlindingFlash) ||
                ElitesWithinRange[RANGE_25] > 0 && DateTime.Now.Subtract(dictAbilityLastUse[SNOPower.Monk_BlindingFlash]).TotalMilliseconds <= 12500) &&
                // Check our mantras, if we have them, are up first
                HasMonkMantraAbilityAndBuff() &&
                // Check the re-use timer and energy costs
                (PlayerStatus.PrimaryResource >= 75 || (Settings.Combat.Monk.HasInnaSet && PlayerStatus.PrimaryResource >= 5)))
            {
                SweepWindSpam = DateTime.Now;
                return new TrinityPower(SNOPower.Monk_SweepingWind, 0f, vNullLocation, iCurrentWorldID, -1, 0, 1, USE_SLOWLY); 
            }


            // Seven Sided Strike
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_15] >= 1 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 15f) || PlayerStatus.CurrentHealthPct <= 0.55) &&
                Hotbar.Contains(SNOPower.Monk_SevenSidedStrike) && ((PlayerStatus.PrimaryResource >= 50 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve) &&
                GilesUseTimer(SNOPower.Monk_SevenSidedStrike, true) && PowerManager.CanCast(SNOPower.Monk_SevenSidedStrike))
            {
                return new TrinityPower(SNOPower.Monk_SevenSidedStrike, 16f, CurrentTarget.Position, iCurrentWorldID, -1, 2, 3, USE_SLOWLY);
            }
            // Exploding Palm
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_25] > 0 || AnythingWithinRange[RANGE_15] >= 3 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 14f)) &&
                Hotbar.Contains(SNOPower.Monk_ExplodingPalm) &&
                ((PlayerStatus.PrimaryResource >= 40 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve) &&
                GilesUseTimer(SNOPower.Monk_ExplodingPalm) && PowerManager.CanCast(SNOPower.Monk_ExplodingPalm))
            {
                return new TrinityPower(SNOPower.Monk_ExplodingPalm, 14f, vNullLocation, -1, CurrentTarget.ACDGuid, 1, 1, USE_SLOWLY);
            }
            // Cyclone Strike
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_20] >= 1 || AnythingWithinRange[RANGE_20] >= 2 ||
                (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 18f)) &&
                Hotbar.Contains(SNOPower.Monk_CycloneStrike) &&
                ((PlayerStatus.PrimaryResource >= 50 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve) &&
                GilesUseTimer(SNOPower.Monk_CycloneStrike) && PowerManager.CanCast(SNOPower.Monk_CycloneStrike))
            {
                return new TrinityPower(SNOPower.Monk_CycloneStrike, 0f, vNullLocation, iCurrentWorldID, -1, 2, 2, USE_SLOWLY);
            }

            // For tempest rush re-use
            if (!bOOCBuff && LastPowerUsed == SNOPower.Monk_TempestRush && PlayerStatus.PrimaryResource >= 10 &&
                (Settings.Combat.Monk.TROption != TempestRushOption.MovementOnly))
            {
                GenerateMonkZigZag();
                return new TrinityPower(SNOPower.Monk_TempestRush, 23f, vSideToSideTarget, iCurrentWorldID, -1, 0, 0, USE_SLOWLY);
            }

            // Tempest rush at elites or groups of mobs
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated && !PlayerStatus.IsRooted && Hotbar.Contains(SNOPower.Monk_TempestRush) &&
                ((PlayerStatus.PrimaryResource >= 15 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve) &&
                (Settings.Combat.Monk.TROption == TempestRushOption.Always ||
                Settings.Combat.Monk.TROption == TempestRushOption.CombatOnly ||
                (Settings.Combat.Monk.TROption == TempestRushOption.ElitesGroupsOnly && (ElitesWithinRange[RANGE_25] > 0 || AnythingWithinRange[RANGE_15] > 2))))
            {
                GenerateMonkZigZag();
                return new TrinityPower(SNOPower.Monk_TempestRush, 60f, vSideToSideTarget, iCurrentWorldID, -1, 0, 0, USE_SLOWLY);
            }
            // 4 Mantra spam for the 4 second buff
            if (!bOOCBuff && !Settings.Combat.Monk.DisableMantraSpam && (!Hotbar.Contains(SNOPower.Monk_TempestRush) || PlayerStatus.PrimaryResource >= 98 ||
                (PlayerStatus.CurrentHealthPct <= 0.55 && PlayerStatus.PrimaryResource >= 75) || CurrentTarget.IsBoss) &&
                    (PlayerStatus.PrimaryResource >= 135 ||
                    (GetHasBuff(SNOPower.Monk_SweepingWind) && (PlayerStatus.PrimaryResource >= 60 &&
                    PlayerStatus.PrimaryResource >= 110 || (PlayerStatus.PrimaryResource >= 100 && PlayerStatus.CurrentHealthPct >= 0.6) ||
                    (PlayerStatus.PrimaryResource >= 50 && PlayerStatus.CurrentHealthPct >= 0.6)) &&
                // Checking we have no expensive finishers
                    !Hotbar.Contains(SNOPower.Monk_SevenSidedStrike) && !Hotbar.Contains(SNOPower.Monk_LashingTailKick) &&
                    !Hotbar.Contains(SNOPower.Monk_WaveOfLight) && !Hotbar.Contains(SNOPower.Monk_CycloneStrike) &&
                    !Hotbar.Contains(SNOPower.Monk_ExplodingPalm))) &&
                (ElitesWithinRange[RANGE_15] >= 1 || AnythingWithinRange[RANGE_15] >= 3 ||
                (AnythingWithinRange[RANGE_15] >= 1 && (Settings.Combat.Monk.HasInnaSet && PlayerStatus.PrimaryResource >= 70)))) 
            {
                if (Hotbar.Contains(SNOPower.Monk_MantraOfEvasion) && GilesUseTimer(SNOPower.Monk_MantraOfEvasion))
                {
                    return new TrinityPower(SNOPower.Monk_MantraOfEvasion, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
                }
                if (Hotbar.Contains(SNOPower.Monk_MantraOfConviction) && GilesUseTimer(SNOPower.Monk_MantraOfConviction))
                {
                    return new TrinityPower(SNOPower.Monk_MantraOfConviction, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
                }
                if (Hotbar.Contains(SNOPower.Monk_MantraOfRetribution) && GilesUseTimer(SNOPower.Monk_MantraOfRetribution))
                {
                    return new TrinityPower(SNOPower.Monk_MantraOfRetribution, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
                }
                if (Hotbar.Contains(SNOPower.Monk_MantraOfHealing) && GilesUseTimer(SNOPower.Monk_MantraOfHealing))
                {
                    return new TrinityPower(SNOPower.Monk_MantraOfHealing, 0f, vNullLocation, iCurrentWorldID, -1, 1, 1, USE_SLOWLY);
                }
            }
            // Lashing Tail Kick
            if (!bOOCBuff && !bCurrentlyAvoiding && Hotbar.Contains(SNOPower.Monk_LashingTailKick) && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_15] > 0 || AnythingWithinRange[RANGE_15] > 4 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 10f)) &&
                // Either doesn't have sweeping wind, or does but the buff is already up
                (!Hotbar.Contains(SNOPower.Monk_SweepingWind) || (Hotbar.Contains(SNOPower.Monk_SweepingWind) && GetHasBuff(SNOPower.Monk_SweepingWind))) &&
                GilesUseTimer(SNOPower.Monk_LashingTailKick) &&
                ((PlayerStatus.PrimaryResource >= 65 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve))
            {
                return new TrinityPower(SNOPower.Monk_LashingTailKick, 10f, vNullLocation, -1, CurrentTarget.ACDGuid, 1, 1, USE_SLOWLY);
            }
            // Wave of light
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_25] > 0 ||
                (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 14f) ||
                AnythingWithinRange[RANGE_15] > 2) &&
                Hotbar.Contains(SNOPower.Monk_WaveOfLight) &&
                GilesUseTimer(SNOPower.Monk_WaveOfLight) &&
                (PlayerStatus.PrimaryResource >= 90 || PlayerStatus.PrimaryResourcePct >= 0.85) && HasMonkMantraAbilityAndBuff())
            {
                return new TrinityPower(SNOPower.Monk_WaveOfLight, 16f, vNullLocation, -1, CurrentTarget.ACDGuid, 1, 1, USE_SLOWLY);
            }
            // Dashing Strike
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated &&
                (ElitesWithinRange[RANGE_25] > 0 || (CurrentTarget.IsBossOrEliteRareUnique && CurrentTarget.RadiusDistance <= 14f) || AnythingWithinRange[RANGE_15] > 2) &&
                Hotbar.Contains(SNOPower.Monk_DashingStrike) && ((PlayerStatus.PrimaryResource >= 30 && !PlayerStatus.WaitingForReserveEnergy) || PlayerStatus.PrimaryResource >= MinEnergyReserve))
            {
                return new TrinityPower(SNOPower.Monk_DashingStrike, 14f, vNullLocation, -1, CurrentTarget.ACDGuid, 0, 1, USE_SLOWLY);
            }

            // Fists of thunder as the primary, repeatable attack
            if (!bOOCBuff && !bCurrentlyAvoiding && Hotbar.Contains(SNOPower.Monk_FistsofThunder)
                && (DateTime.Now.Subtract(OtherThanDeadlyReach).TotalMilliseconds < 2700 && DateTime.Now.Subtract(ForeSightFirstHit).TotalMilliseconds < 29000 || !Hotbar.Contains(SNOPower.Monk_DeadlyReach) || CurrentTarget.RadiusDistance > 12f ||
                AnythingWithinRange[RANGE_50] < 5 && ElitesWithinRange[RANGE_50] <= 0 && !WantToSwap))
            {
                if (DateTime.Now.Subtract(OtherThanDeadlyReach).TotalMilliseconds < 2700)
                    OtherThanDeadlyReach = DateTime.Now;
                if (GetHasBuff(SNOPower.Monk_SweepingWind) && DateTime.Now.Subtract(SweepWindSpam).TotalMilliseconds < 5500)
                    SweepWindSpam = DateTime.Now; //intell -- inna
                return new TrinityPower(SNOPower.Monk_FistsofThunder, 30f, vNullLocation, -1, CurrentTarget.ACDGuid, 0, 0, SIGNATURE_SPAM);
            }
            // Crippling wave
            if (!bOOCBuff && !bCurrentlyAvoiding && Hotbar.Contains(SNOPower.Monk_CripplingWave)
                && (DateTime.Now.Subtract(OtherThanDeadlyReach).TotalMilliseconds < 2700 && DateTime.Now.Subtract(ForeSightFirstHit).TotalMilliseconds < 29000 || !Hotbar.Contains(SNOPower.Monk_DeadlyReach)
                || AnythingWithinRange[RANGE_50] < 5 && ElitesWithinRange[RANGE_50] <= 0 && !WantToSwap))
            {
                OtherThanDeadlyReach = DateTime.Now;
                if (GetHasBuff(SNOPower.Monk_SweepingWind) && DateTime.Now.Subtract(SweepWindSpam).TotalMilliseconds < 5500)
                    SweepWindSpam = DateTime.Now; //intell -- inna
                return new TrinityPower(SNOPower.Monk_CripplingWave, 14f, vNullLocation, -1, CurrentTarget.ACDGuid, 0, 1, USE_SLOWLY);
            }
            // Way of hundred fists
            if (!bOOCBuff && !bCurrentlyAvoiding && Hotbar.Contains(SNOPower.Monk_WayOfTheHundredFists)
                && (DateTime.Now.Subtract(OtherThanDeadlyReach).TotalMilliseconds < 2700 && DateTime.Now.Subtract(ForeSightFirstHit).TotalMilliseconds < 29000 || !Hotbar.Contains(SNOPower.Monk_DeadlyReach)
                || AnythingWithinRange[RANGE_50] < 5 && ElitesWithinRange[RANGE_50] <= 0 && !WantToSwap))
            {
                OtherThanDeadlyReach = DateTime.Now;
                if (GetHasBuff(SNOPower.Monk_SweepingWind) && DateTime.Now.Subtract(SweepWindSpam).TotalMilliseconds < 5500)
                    SweepWindSpam = DateTime.Now; //intell -- inna
                return new TrinityPower(SNOPower.Monk_WayOfTheHundredFists, 14f, vNullLocation, -1, CurrentTarget.ACDGuid, 0, 1, SIGNATURE_SPAM);
            }
            // Deadly reach
            if (!bOOCBuff && !bCurrentlyAvoiding && Hotbar.Contains(SNOPower.Monk_DeadlyReach))
            {
                if (DateTime.Now.Subtract(ForeSightFirstHit).TotalMilliseconds > 29000)
                {
                    ForeSightFirstHit = DateTime.Now;
                }
                else if (DateTime.Now.Subtract(ForeSight2).TotalMilliseconds > 400 && DateTime.Now.Subtract(ForeSightFirstHit).TotalMilliseconds > 1400)
                {
                    OtherThanDeadlyReach = DateTime.Now;
                }
                if (DateTime.Now.Subtract(ForeSight2).TotalMilliseconds > 2800)
                {
                    ForeSight2 = DateTime.Now;
                }
                if (GetHasBuff(SNOPower.Monk_SweepingWind) && DateTime.Now.Subtract(SweepWindSpam).TotalMilliseconds < 5500)
                    SweepWindSpam = DateTime.Now; //intell -- inna
                return new TrinityPower(SNOPower.Monk_DeadlyReach, 16f, vNullLocation, -1, CurrentTarget.ACDGuid, 0, 1, USE_SLOWLY);
            }
            // Default attacks
            if (!bOOCBuff && !bCurrentlyAvoiding && !PlayerStatus.IsIncapacitated)
            {
                if (GetHasBuff(SNOPower.Monk_SweepingWind) && DateTime.Now.Subtract(SweepWindSpam).TotalMilliseconds < 5500)
                    SweepWindSpam = DateTime.Now; //intell -- inna
                return new TrinityPower(SNOPower.Weapon_Melee_Instant, 10f, vNullLocation, -1, CurrentTarget.ACDGuid, 1, 1, USE_SLOWLY);
            }
            return defaultPower;
        }

        private static void GenerateMonkZigZag()
        {
            bool bGenerateNewZigZag = (DateTime.Now.Subtract(lastChangedZigZag).TotalMilliseconds >= 1500 ||
                (vPositionLastZigZagCheck != vNullLocation && PlayerStatus.CurrentPosition == vPositionLastZigZagCheck && DateTime.Now.Subtract(lastChangedZigZag).TotalMilliseconds >= 200) ||
                Vector3.Distance(PlayerStatus.CurrentPosition, vSideToSideTarget) <= 4f ||
                CurrentTarget.ACDGuid != iACDGUIDLastWhirlwind);
            vPositionLastZigZagCheck = PlayerStatus.CurrentPosition;
            if (bGenerateNewZigZag)
            {
                float fExtraDistance = CurrentTarget.CentreDistance <= 20f ? 15f : 5f;
                vSideToSideTarget = FindZigZagTargetLocation(CurrentTarget.Position, CurrentTarget.CentreDistance + fExtraDistance);
                // Resetting this to ensure the "no-spam" is reset since we changed our target location
                //powerLastSnoPowerUsed = SNOPower.None;
                iACDGUIDLastWhirlwind = CurrentTarget.ACDGuid;
                lastChangedZigZag = DateTime.Now;
            }
        }

        private static TrinityPower GetMonkDestroyPower()
        {
            if (Hotbar.Contains(SNOPower.Monk_TempestRush) && LastPowerUsed == SNOPower.Monk_TempestRush && PlayerStatus.PrimaryResource > 10f)
                return new TrinityPower(SNOPower.Monk_TempestRush, 40f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
            if (Hotbar.Contains(SNOPower.Monk_FistsofThunder))
                return new TrinityPower(SNOPower.Monk_FistsofThunder, 15f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
            if (Hotbar.Contains(SNOPower.Monk_DeadlyReach))
                return new TrinityPower(SNOPower.Monk_DeadlyReach, 10f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
            if (Hotbar.Contains(SNOPower.Monk_CripplingWave))
                return new TrinityPower(SNOPower.Monk_CripplingWave, 10f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
            if (Hotbar.Contains(SNOPower.Monk_WayOfTheHundredFists))
                return new TrinityPower(SNOPower.Monk_WayOfTheHundredFists, 10f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
            return new TrinityPower(SNOPower.Weapon_Melee_Instant, 10f, vNullLocation, -1, -1, 0, 0, USE_SLOWLY);
        }
        /// <summary>
        /// Returns true if we have a mantra and it's up, or if we don't have a Mantra at all
        /// </summary>
        /// <returns></returns>
        private static bool HasMonkMantraAbilityAndBuff()
        {
            return
                (CheckAbilityAndBuff(SNOPower.Monk_MantraOfConviction) ||
                CheckAbilityAndBuff(SNOPower.Monk_MantraOfEvasion) ||
                CheckAbilityAndBuff(SNOPower.Monk_MantraOfHealing) ||
                CheckAbilityAndBuff(SNOPower.Monk_MantraOfRetribution) ||
                DoesNotHaveMonkMantraAbility());
        }
        private static bool DoesNotHaveMonkMantraAbility()
        {
            return
                (!Hotbar.Contains(SNOPower.Monk_MantraOfConviction) &&
                !Hotbar.Contains(SNOPower.Monk_MantraOfEvasion) &&
                !Hotbar.Contains(SNOPower.Monk_MantraOfHealing) &&
                !Hotbar.Contains(SNOPower.Monk_MantraOfRetribution));
        }


    }
}
