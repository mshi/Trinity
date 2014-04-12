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
        private const int WaitTimeShort = 750;
        private const int WaitTime = 1500;
        private const int WaitTimeMax = 3000;
        private static void HandleShrine()
        {
            using (new PerformanceLogger("HandleShrine"))
            {
                ACDItem oldItem = null;
                if (Settings.Combat.Misc.SwapBracerForShrine)
                {
                    oldItem = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
                    var targetItem =
                        ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Nemesis Bracers"));
                    if (targetItem == null || !targetItem.IsValid)
                    {
                        Logger.Log("[HandleShrine] Nemesis Bracers not found");
                    }
                    else
                    {
                        OpenInventory();
                        BotMain.PauseWhile(() => !UIElements.InventoryWindow.IsVisible, WaitTimeShort,
                                           TimeSpan.FromMilliseconds(WaitTimeMax));
                        Logger.Log("[HandleShrine] Opened inventory");
                        var attempts = 0;
                        while (attempts++ < 10)
                        {
                            Logger.Log("[HandleShrine] Swapping item {0} with {1}. Attempt {2}", oldItem.Name, targetItem.Name, attempts);
                            ZetaDia.Me.Inventory.EquipItem(targetItem.DynamicId, InventorySlot.Bracers);
                            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeShort));
                            if (ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).DynamicId == targetItem.DynamicId)
                            {
                                break;
                            }
                        }
                        //BotMain.PauseWhile(
                        //    () =>
                        //    ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).DynamicId != targetItem.DynamicId, WaitTimeShort, TimeSpan.FromMilliseconds(WaitTimeMax));
                    }

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
                    BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
                    var attempts = 0;
                    while (attempts++ < 10)
                    {
                        Logger.Log("[HandleShrine] Swapping back item {0}. Attempt {1}", oldItem.Name, attempts);
                        ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, InventorySlot.Bracers);
                        BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeShort));
                        if (ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).DynamicId == oldItem.DynamicId)
                        {
                            break;
                        }
                    }

                    CloseInventory();
                    Logger.Log("[HandleShrine] Closed inventory");
                }
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
