using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Trinity.UIComponents;

namespace EquipmentSwap
{
    class Config
    {
        private static Window configWindow;

        public static void CloseWindow()
        {
            configWindow.Close();
        }

        public static Window GetDisplayWindow()
        {
            try
            {
                if (configWindow == null)
                {
                    configWindow = new Window();
                }

                string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string xamlPath = Path.Combine(assemblyPath, "Plugins", "Trinity", "EquipmentSwap", "Config.xaml");

                string xamlContent = File.ReadAllText(xamlPath);

                // This hooks up our object with our UserControl DataBinding
                configWindow.DataContext = new Config();

                UserControl mainControl = (UserControl)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
                configWindow.Content = mainControl;
                configWindow.Width = 200;
                configWindow.Height = 175;
                configWindow.ResizeMode = ResizeMode.NoResize;
                configWindow.Background = Brushes.DarkGray;

                configWindow.Title = "EquipTest";

                configWindow.Closed += ConfigWindow_Closed;
                Demonbuddy.App.Current.Exit += ConfigWindow_Closed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            return configWindow;
        }

        public ICommand EquipNemesis
        {
            get
            {
                return new RelayCommand((parameter) =>
                {
                    try
                    {
                        EquipmentSwapper.EquipShrineItems();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error Equipping Shrine items");
                        if (ex.InnerException != null)
                        {
                            Logger.Error(ex.InnerException, "InnerException: ");
                        }
                    }
                });
            }
        }

        public ICommand EquipOriginal
        {
            get
            {
                return new RelayCommand((parameter) =>
                {
                    try
                    {
                        EquipmentSwapper.EquipOriginalItems();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error Equipping original item");
                    }
                });
            }
        }

        static void ConfigWindow_Closed(object sender, System.EventArgs e)
        {
            if (configWindow != null)
            {
                configWindow.Closed -= ConfigWindow_Closed;
                configWindow = null;
            }
        }
    }
}
