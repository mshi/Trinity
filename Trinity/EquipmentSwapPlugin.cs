using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.Common.Plugins;

namespace EquipmentSwap
{
    public class EquipmentSwapPlugin : IPlugin
    {
        public Version Version { get { return new Version(0, 0, 1); } }
        public string Author { get { return "UndeadGhost"; } }
        public string Description { get { return "Equip items based on conditions"; } }
        public string Name { get { return "Equipment Swap"; } }
        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }

        public void OnEnabled()
        {
            Logger.Info("Enabled.");
        }

        public void OnDisabled()
        {
            Logger.Info("Disabled.");
        }

        public void OnInitialize()
        {
            Logger.Info("Initialized.");
        }

        public void OnPulse()
        {
        }

        public void OnShutdown()
        {

        }
        System.Windows.Window IPlugin.DisplayWindow
        {
            get
            {
                return Config.GetDisplayWindow();
            }
        }

    }
}
