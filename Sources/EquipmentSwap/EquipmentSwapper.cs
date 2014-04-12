// InventoryWindow.IsVisible and ZetaDia.Me.Inventory.* only gets updated on OnPulse (i think).
// TODO: Queue task to occur on the next on pulse. (check for bracer, close inventory, etc)
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
                    Logger.Info("Swapping item {0} with {1}.", _originalBracer.Name, targetItem.Name);
                    ZetaDia.Me.Inventory.EquipItem(targetItem.DynamicId, InventorySlot.Bracers);
                    BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
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

            Logger.Info("Swapping back item {0}.", oldItem.Name);
            ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, InventorySlot.Bracers);
            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTimeShort));

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
