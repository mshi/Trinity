// InventoryWindow.IsVisible and ZetaDia.Me.Inventory.* only gets updated on OnPulse (i think).
// TODO: Queue task to occur on the next on pulse. (check for bracer, close inventory, etc)
//#define INVENTORY_VISIBILITY_WORKS

using System;
using System.Linq;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace EquipmentSwap
{
    class EquipmentSwapper
    {
        private const int WaitTime = 750;
        private static ACDItem _originalBracer;
        private static ACDItem _originalGlove;
        private static bool _isOpen;

        public static void EquipShrineItems()
        {
            bool equipBracer = false;
            bool equipGlove = false;
            _originalBracer = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
            _originalGlove = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Hands);
            var targetBracer =
                ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Nemesis Bracers"));
            var targetGlove = ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Gloves of Worship"));
            if (targetBracer == null || !targetBracer.IsValid)
            {
                Logger.Info("Nemesis Bracers not found");
            }
            else
            {
                equipBracer = true;
            }
            if (targetGlove == null || !targetGlove.IsValid)
            {
                Logger.Info("Gloves of Worship not found");
            }
            else
            {
                equipGlove = true;
            }
            if (equipBracer || equipGlove)
            {
                OpenInventory();
#if INVENTORY_VISIBILITY_WORKS
                    BotMain.PauseWhile(() => !UIElements.InventoryWindow.IsVisible, WaitTimeShort,
                                       TimeSpan.FromMilliseconds(WaitTimeMax));
#else
                BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
#endif
                Logger.Info("Opened inventory");
                if (equipBracer)
                {
                    Logger.Info("Swapping item {0} with {1}.", _originalBracer.Name, targetBracer.Name);
                    ZetaDia.Me.Inventory.EquipItem(targetBracer.DynamicId, InventorySlot.Bracers);
                    BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
                }
                if (equipGlove)
                {
                    Logger.Info("Swapping item {0} with {1}.", _originalGlove.Name, targetGlove.Name);
                    ZetaDia.Me.Inventory.EquipItem(targetGlove.DynamicId, InventorySlot.Hands);
                    BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
                }
            }
        }

        public static void EquipOriginalItem(ACDItem oldItem)
        {
            Logger.Info("Swapping back item {0}.", oldItem.Name);
            ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, InventorySlot.Bracers);
            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
        }

        public static void EquipOriginalItems()
        {
            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
            if (_originalBracer == null)
            {
                Logger.Error("No original bracer exist.");
            }
            else
            {
                var currentBracer = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
                if (currentBracer.DynamicId == _originalBracer.DynamicId)
                {
                    Logger.Info("Already wearing original bracer");
                }
                else
                {
                    EquipOriginalItem(_originalBracer);
                }
            }
            if (_originalGlove == null)
            {
                Logger.Error("No original glove exist.");
            }
            else
            {
                var currentGlove = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Hands);
                if (currentGlove.DynamicId == _originalGlove.DynamicId)
                {
                    Logger.Info("Already wearing original glove");
                }
                else
                {
                    EquipOriginalItem(_originalGlove);
                }
            }
            CloseInventory();
            Logger.Info("Closed inventory");
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
