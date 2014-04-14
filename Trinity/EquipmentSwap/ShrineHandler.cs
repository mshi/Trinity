using EquipmentSwap;
using Trinity.Technicals;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Logger = Trinity.Technicals.Logger;

namespace Trinity
{
    public partial class Trinity
    {
        private enum HandleShrineStep { Equip, TouchShrine, ReEquipOriginal }

        private static HandleShrineStep _shrineNextStep = HandleShrineStep.Equip;

        private static bool HandleShrine()
        {
            using (new PerformanceLogger("HandleShrine"))
            {
                if (CurrentTarget.InternalName.Equals("PoolOfReflection"))
                {
                    Logger.Log("[HandleShrine] This pool of reflection. Proceed.");
                    Logger.LogDebug(LogCategory.Behavior,
                                    "Using {0} on {1} Distance {2} Radius {3}",
                                    SNOPower.Axe_Operate_Gizmo, CurrentTarget.InternalName,
                                    CurrentTarget.CentreDistance, CurrentTarget.Radius);
                    ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0,
                                        CurrentTarget.ACDGuid);
                    return true;
                }
                if (Settings.Combat.Misc.SwapBracerForShrine && _shrineNextStep == HandleShrineStep.Equip)
                {
                    EquipmentSwapper.EquipShrineItems();
                    _shrineNextStep = HandleShrineStep.TouchShrine;
                    return false;
                }

                if (_shrineNextStep == HandleShrineStep.TouchShrine || !Settings.Combat.Misc.SwapBracerForShrine)
                {
                    Logger.LogDebug(LogCategory.Behavior,
                                    "Using {0} on {1} Distance {2} Radius {3}",
                                    SNOPower.Axe_Operate_Gizmo, CurrentTarget.InternalName,
                                    CurrentTarget.CentreDistance, CurrentTarget.Radius);
                    Logger.Log("[HandleShrine] Touching shrine {0} Distance {1} Radius {2}", CurrentTarget.InternalName,
                               CurrentTarget.CentreDistance, CurrentTarget.Radius);
                    ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0,
                                        CurrentTarget.ACDGuid);
                    if (Settings.Combat.Misc.SwapBracerForShrine)
                    {
                        _shrineNextStep = HandleShrineStep.ReEquipOriginal;
                        return false;
                    }
                    return true;
                }

                if (_shrineNextStep == HandleShrineStep.ReEquipOriginal)
                {
                    EquipmentSwapper.EquipOriginalItems();
                    _shrineNextStep = HandleShrineStep.Equip;
                    return true;
                }
            }
            Logger.LogError("HandleShrine had an invalid state. Skipping");
            return true; // default to pass, but this scenario should never happen
        }


    }
}
