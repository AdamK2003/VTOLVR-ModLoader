// unset

using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Console = Launcher.Views.Console;

namespace Launcher.Classes.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProgramData
    {
        private static string _userPath = String.Empty;
        [JsonProperty("VTOL VR Path")] 
        public string VTOLPath = string.Empty;

        public static void Save(ProgramData data)
        {
            if (_userPath.Equals(string.Empty))
                _userPath = GetSavePath();
            
            using (TextWriter tw = new StreamWriter(_userPath))
            {
                var js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(tw, data);
            }
        }

        public static void Delete()
        {
            if (_userPath.Equals(string.Empty))
                _userPath = GetSavePath();
            
            if (File.Exists(_userPath))
            {
                File.Delete(_userPath);
            }
        }

        public static string GetSavePath()
        {
            string usersPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Startup.AppdataFolder);
            Directory.CreateDirectory(usersPath);
            usersPath = Path.Combine(usersPath, Startup.DataFile);
            
            return usersPath;
        }
    }
}