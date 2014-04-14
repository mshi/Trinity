// InventoryWindow.IsVisible and ZetaDia.Me.Inventory.* only gets updated on OnPulse (i think).
// TODO: Queue task to occur on the next on pulse. (check for bracer, close inventory, etc)

using System;
using System.Linq;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;

namespace EquipmentSwap
{
    class EquipmentSwapper
    {
        private const int WaitTime = 250;
        private static ACDItem _originalBracer;
        private static ACDItem _originalGlove;
        private static bool _bracerChanged;
        private static bool _gloveChanged;

        public static void EquipItem(ACDItem originalItem, ACDItem targetItem, InventorySlot slot)
        {
            Logger.Info("Swapping item {0} with {1}.", originalItem.Name, targetItem.Name);
            ZetaDia.Me.Inventory.EquipItem(targetItem.DynamicId, slot);
            BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
        }

        public static void EquipBracer()
        {
            _originalBracer = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Bracers);
            var targetBracer =
                ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Nemesis Bracers"));
            if (targetBracer == null || !targetBracer.IsValid)
            {
                Logger.Info("Nemesis Bracers not found");
                _bracerChanged = false;
            }
            else
            {
                EquipItem(_originalBracer, targetBracer, InventorySlot.Bracers);
                _bracerChanged = true;
            }
        }

        public static void EquipGlove()
        {
            _originalGlove = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == InventorySlot.Hands);
            var targetGlove = ZetaDia.Me.Inventory.Backpack.FirstOrDefault(item => item.Name.Equals("Gloves of Worship"));
            if (targetGlove == null || !targetGlove.IsValid)
            {
                Logger.Info("Gloves of Worship not found");
                _gloveChanged = false;
            }
            else
            {
                EquipItem(_originalGlove, targetGlove, InventorySlot.Hands);
                _gloveChanged = true;
            }
        }

        public static void EquipOriginalItem(ACDItem oldItem, InventorySlot slot)
        {
            var equippedItem = ZetaDia.Me.Inventory.Equipped.Single(x => x.InventorySlot == slot);
            if (equippedItem.DynamicId == oldItem.DynamicId)
            {
                Logger.Info("Already wearing original item {0}", oldItem.Name);
            }
            else
            {
                Logger.Info("Swapping back item {0} (from shrine item {1}).", oldItem.Name, equippedItem.Name);
                ZetaDia.Me.Inventory.EquipItem(oldItem.DynamicId, slot);
                BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
            }
        }

        public static void EquipOriginalBracer()
        {
            if (_originalBracer == null)
            {
                Logger.Error("No original bracer exist.");
            }
            else if (!_bracerChanged)
            {
                Logger.Error("Bracer didn't change.");
            }
            else
            {
                EquipOriginalItem(_originalBracer, InventorySlot.Bracers);
                _bracerChanged = false;
            }
        }

        public static void EquipOriginalGlove()
        {
            if (_originalGlove == null)
            {
                Logger.Error("No original glove exist.");
            }
            else if (!_gloveChanged)
            {
                Logger.Error("Glove didn't change.");
            }
            else
            {
                EquipOriginalItem(_originalGlove, InventorySlot.Hands);
                _gloveChanged = false;
            }
        }

        public static void OpenInventory()
        {
            if (!UIElements.InventoryWindow.IsVisible)
            {
                UIManager.ToggleInventoryMenu();
                BotMain.PauseFor(TimeSpan.FromMilliseconds(WaitTime));
                Logger.Info("Opened inventory");
            }
            else
            {
                Logger.Info("Inventory already open");
            }
        }

        public static void CloseInventory()
        {
            if (UIElements.InventoryWindow.IsVisible)
            {
                UIManager.ToggleInventoryMenu();
                Logger.Info("Closed inventory");
            }
            else
            {
                Logger.Info("Inventory already closed");
            }
        }
    }
}
