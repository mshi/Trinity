using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EquipmentSwap;
using Trinity.Technicals;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Logger = Trinity.Technicals.Logger;

namespace Trinity
{
    public partial class Trinity
    {

        private static void HandleShrine()
        {
            using (new PerformanceLogger("HandleShrine"))
            {
                ACDItem oldItem = null;
                if (Settings.Combat.Misc.SwapBracerForShrine)
                {
                    oldItem = EquipmentSwapper.EquipNemesisBracer();
                }
                Logger.LogDebug(LogCategory.Behavior,
                                "Using {0} on {1} Distance {2} Radius {3}",
                                SNOPower.Axe_Operate_Gizmo, CurrentTarget.InternalName,
                                CurrentTarget.CentreDistance, CurrentTarget.Radius);
                Logger.Log("[HandleShrine] Touching shrine {0} Distance {1} Radius {2}", CurrentTarget.InternalName,
                           CurrentTarget.CentreDistance, CurrentTarget.Radius);
                ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0,
                                    CurrentTarget.ACDGuid);
                if (oldItem != null)
                {
                    EquipmentSwapper.EquipOriginalBracer(oldItem);
                }
            }
        }


    }
}
