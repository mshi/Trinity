using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private const int WAIT_TIME = 500;
        private static void HandleShrine()
        {
            ACDItem oldItem = null;
            if (Settings.Combat.Misc.SwapBracerForShrine)
            {
                oldItem = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
                var targetItem = ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Nemesis Bracers"));
                if (targetItem == null || !targetItem.IsValid)
                {
                    Logger.Log("[HandleShrine] Nemesis Bracers not found");
                }
                else
                {
                    Logger.Log("[HandleShrine] Swapping item {0} with {1}", oldItem.Name, targetItem.Name);
                    OpenInventory();
                    ZetaDia.Me.Inventory.EquipItem(targetItem.DynamicId, InventorySlot.Bracers);
                    BotMain.PauseFor(new TimeSpan(0, 0, 0, 0, WAIT_TIME));
                }

            }
            Logger.LogDebug(LogCategory.Behavior,
                            "Using {0} on {1} Distance {2} Radius {3}",
                            SNOPower.Axe_Operate_Gizmo, CurrentTarget.InternalName,
                            CurrentTarget.CentreDistance, CurrentTarget.Radius);
            ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, Vector3.Zero, 0,
                                                                    CurrentTarget.ACDGuid);
            if (oldItem != null)
            {
                BotMain.PauseFor(new TimeSpan(0, 0, 0, 0, WAIT_TIME));
                Logger.Log("[HandleShrine] Swapping back item {0}", oldItem.Name);
                ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, InventorySlot.Bracers);
                CloseInventory();
            }
        }

        private static void OpenInventory()
        {
            if (!UIElements.InventoryWindow.IsVisible)
            {
                UIManager.ToggleInventoryMenu();
            }
        }

        private static void CloseInventory()
        {
            if (UIElements.InventoryWindow.IsVisible)
            {
                UIManager.ToggleInventoryMenu();
            }
        }
    }
}
