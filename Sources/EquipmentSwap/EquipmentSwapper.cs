// InventoryWindow.IsVisible doesn't seem to work
//#define INVENTORY_VISIBILITY_WORKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;

namespace EquipmentSwap
{
    class EquipmentSwapper
    {
        private const int WaitTimeShort = 500;
        private const int WaitTime = 1500;
        private const int WaitTimeMax = 3000;
        private static ACDItem _originalBracer;
        private static bool _isOpen;

        public static ACDItem EquipNemesisBracer()
        {
            try
            {
                _originalBracer = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
                var targetItem =
                    ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Nemesis Bracers"));
                if (targetItem == null || !targetItem.IsValid)
                {
                    Logger.Info("Nemesis Bracers not found");
                }
                else
                {
                    OpenInventory();
#if INVENTORY_VISIBILITY_WORKS
                    BotMain.PauseWhile(() => !UIElements.InventoryWindow.IsVisible, WaitTimeShort,
                                       TimeSpan.FromMilliseconds(WaitTimeMax));
#else
                    BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeMax));
#endif
                    Logger.Info("Opened inventory");
                    var attempts = 0;
                    while (attempts++ < 10)
                    {
                        Logger.Info("Swapping item {0} with {1}. Attempt {2}", _originalBracer.Name, targetItem.Name,
                                    attempts);
                        ZetaDia.Me.Inventory.EquipItem(targetItem.DynamicId, InventorySlot.Bracers);
                        BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeShort));
                        if (
                            ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).
                                DynamicId == targetItem.DynamicId)
                        {
                            break;
                        }
                    }
                    //BotMain.PauseWhile(
                    //    () =>
                    //    ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).DynamicId != targetItem.DynamicId, WaitTimeShort, TimeSpan.FromMilliseconds(WaitTimeMax));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception while trying to equip nemesis bracer.");
            }
            return _originalBracer;
        }

        public static void EquipOriginalBracer(ACDItem oldItem)
        {
            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
            var attempts = 0;
            while (attempts++ < 10)
            {
                Logger.Info("Swapping back item {0}. Attempt {1}", oldItem.Name, attempts);
                ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, InventorySlot.Bracers);
                BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeShort));
                if (ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers).DynamicId ==
                    oldItem.DynamicId)
                {
                    break;
                }
            }

            CloseInventory();
            Logger.Info("Closed inventory");
        }

        public static void EquipOriginalBracer()
        {
            if (_originalBracer == null)
            {
                Logger.Error("No original bracer exist.");
                return;
            }
            EquipOriginalBracer(_originalBracer);
        }

        private static void OpenInventory()
        {
#if INVENTORY_VISIBILITY_WORKS
            if (!UIElements.InventoryWindow.IsVisible)
#else
            if (!_isOpen)
#endif
            {
                UIManager.ToggleInventoryMenu();
                _isOpen = true;
            }
        }

        private static void CloseInventory()
        {
#if INVENTORY_VISIBILITY_WORKS
            if (UIElements.InventoryWindow.IsVisible)
#else
            if (_isOpen)
#endif
            {
                _isOpen = false;
                UIManager.ToggleInventoryMenu();
            }
        }
    }
}
