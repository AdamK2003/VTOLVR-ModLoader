// unset

using System;
using System.IO;

namespace VTPatcher
{
    static class Logger
    {
        private static string _logFile = "Log VTPatcher.txt";
        public static void CreateLogs()
        {
            if (Injector.ModLoaderFolder != null)
                _logFile = Path.Combine(Injector.ModLoaderFolder.FullName, _logFile);
            
            if (File.Exists(_logFile))
                File.Delete(_logFile);

            using (TextWriter writer = File.CreateText(_logFile))
            {
                writer.WriteLine($"Started at {DateTime.Now}");
                writer.Flush();
            }
        }
        
        public static void Log(object message) =>
            WriteLogMessage($"[{DateTime.Now} | LOG]{message}");

        public static void Warning(object message) => 
            WriteLogMessage($"[{DateTime.Now} | WARNING]{message}");
        
        public static void Error(object message) => 
            WriteLogMessage($"[{DateTime.Now} | ERROR]{message}");

        private static void WriteLogMessage(string message)
        {
            using (StreamWriter writer = File.AppendText(_logFile))
            {
                writer.WriteLine(message);
            }
        }
    }
}