﻿using System;
using System.Linq;
using Zeta.Game;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace Trinity.Helpers
{
    public class Composites
    {
        public static Composite CreateVendorRunBehavior(Composite child)
        {
            return
            new PrioritySelector(
                CreateUseHoradricCache(),
                child
            );
        }

        private static DateTime lastCheckedForHoradricCache = DateTime.MinValue;
        private static DateTime lastFoundHoradricCache = DateTime.MinValue;

        public static DateTime LastCheckedForHoradricCache
        {
            get { return Composites.lastCheckedForHoradricCache; }
            set { Composites.lastCheckedForHoradricCache = value; }
        }

        public static DateTime LastFoundHoradricCache
        {
            get { return Composites.lastFoundHoradricCache; }
            set { Composites.lastFoundHoradricCache = value; }
        }

        public static Composite CreateUseHoradricCache()
        {
            return
            new PrioritySelector(
                new Decorator(ret => Trinity.Player.IsInTown && DateTime.UtcNow.Subtract(lastCheckedForHoradricCache).TotalSeconds > 30,
                    new Sequence(
                        new Action(ret => lastCheckedForHoradricCache = DateTime.UtcNow),
                        new Decorator(ret => HasHoradricCaches(),
                            new Action(ret => OpenHoradricCache())
                        )
                    )
                )
            );
        }


        internal static RunStatus OpenHoradricCache()
        {
            return RunStatus.Success;
            if (HasHoradricCaches())
            {
                var item = ZetaDia.Me.Inventory.Backpack.First(i => i.GameBalanceId == Items.ItemIds.HORADRIC_CACHE);
                ZetaDia.Me.Inventory.UseItem(item.DynamicId);
                lastFoundHoradricCache = DateTime.UtcNow;
                return RunStatus.Running;
            }

            return RunStatus.Success;

        }

        internal static bool HasHoradricCaches()
        {
            return ZetaDia.Me.Inventory.Backpack.Any(i => i.GameBalanceId == Items.ItemIds.HORADRIC_CACHE);
        }

    }
}
