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
        private enum HandleShrineStep { Open, EquipBracer, EquipGlove, TouchShrine, ReEquipBracer, ReEquipGlove, Close }

        private static HandleShrineStep _shrineNextStep = HandleShrineStep.Open;

        private static void TouchShrine()
        {
            Logger.LogDebug(LogCategory.Behavior,
                "Using {0} on {1} Distance {2} Radius {3}",
                SNOPower.Axe_Operate_Gizmo, CurrentTarget.InternalName,
                CurrentTarget.CentreDistance, CurrentTarget.Radius);
            Logger.Log("[HandleShrine] Touching shrine {0} Distance {1} Radius {2}", CurrentTarget.InternalName,
                       CurrentTarget.CentreDistance, CurrentTarget.Radius);
            ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0,
                                CurrentTarget.ACDGuid);
        }

        private static bool HandleShrine()
        {
            using (new PerformanceLogger("HandleShrine"))
            {
                if (CurrentTarget.Type != GObjectType.Shrine && _shrineNextStep == HandleShrineStep.TouchShrine)
                {
                    Logger.LogError("[HandleShrine] Non-shrine type passed in handle shrine as current target");
                    return true;
                }
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
                if (Settings.Combat.Misc.SwapEquipsForShrine)
                {
                    switch (_shrineNextStep)
                    {
                        case HandleShrineStep.Open:
                            EquipmentSwapper.OpenInventory();
                            _shrineNextStep = HandleShrineStep.EquipBracer;
                            return false;
                        case HandleShrineStep.EquipBracer:
                            EquipmentSwapper.EquipBracer();
                            _shrineNextStep = HandleShrineStep.EquipGlove;
                            return false;
                        case HandleShrineStep.EquipGlove:
                            EquipmentSwapper.EquipGlove();
                            _shrineNextStep = HandleShrineStep.TouchShrine;
                            return false;
                        case HandleShrineStep.TouchShrine:
                            TouchShrine();
                            _shrineNextStep = HandleShrineStep.ReEquipBracer;
                            return true;
                        case HandleShrineStep.ReEquipBracer:
                            EquipmentSwapper.EquipOriginalBracer();
                            _shrineNextStep = HandleShrineStep.ReEquipGlove;
                            return false;
                        case HandleShrineStep.ReEquipGlove:
                            EquipmentSwapper.EquipOriginalGlove();
                            _shrineNextStep = HandleShrineStep.Close;
                            return false;
                        case HandleShrineStep.Close:
                            EquipmentSwapper.CloseInventory();
                            _shrineNextStep = HandleShrineStep.Open;
                            return true;
                    }
                }
                else
                {
                    TouchShrine();
                    return true;
                }
            }
            Logger.LogError("HandleShrine had an invalid state. Skipping");
            return true; // default to pass, but this scenario should never happen
        }


    }
}
