using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    static class Logger
    {
        public enum LogType { Log, Warning, Error }
        public static Action<object, LogType> OnMessageLogged;

        public static void Log(object message)
        {
            OnMessageLogged?.Invoke(message, LogType.Log);
        }
        public static void Warning(object message)
        {
            OnMessageLogged?.Invoke(message, LogType.Warning);
        }
        public static void Error(object message)
        {
            OnMessageLogged?.Invoke(message, LogType.Error);
        }
    }
}
