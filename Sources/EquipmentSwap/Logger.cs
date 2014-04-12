
using System;

namespace EquipmentSwap
{
    public class Logger
    {
        private static readonly log4net.ILog DBLog = Zeta.Common.Logger.GetLoggerInstanceForType();

        public static void Error(Exception e, string message, params object[] args)
        {
            message = "[EquipmentSwapper] " + message;
            if (e != null)
            {
                DBLog.Error(String.Format(message, args), e);
            }
            else
            {
                DBLog.ErrorFormat(message, args);
            }
        }

        public static void Error(string message, params object[] args)
        {
            Error(null, message, args);
        }

        public static void Info(string message, params object[] args)
        {
            message = "[EquipmentSwapper] " + message;
            DBLog.InfoFormat(message, args);
        }

        public static void Debug(string message, params object[] args)
        {
            message = "[EquipmentSwapper] " + message;
            DBLog.DebugFormat(message, args);
        }

    }
}
