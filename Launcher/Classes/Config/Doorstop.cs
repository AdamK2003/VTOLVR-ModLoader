using System.IO;
using Launcher.Views;
using Salaros.Configuration;

namespace Launcher.Classes.Config
{
    public static class Doorstop
    {
        #region Default Content
        public const string DefaultContent = @"# General Doorstop settings
[UnityDoorstop]
# Specifies whether assembly executing is enabled
enabled=true
# Specifies the path (absolute, or relative to the game's exe) to the DLL/EXE that should be executed by Doorstop
targetAssembly=VTOLVR_ModLoader\VTPatcher.dll
# Specifies whether Unity's output log should be redirected to <current folder>\output_log.txt
redirectOutputLog=false
# If enabled, DOORSTOP_DISABLE env var value is ignored
# USE THIS ONLY WHEN ASKED TO OR YOU KNOW WHAT THIS MEANS
ignoreDisableSwitch=false
# Overrides default Mono DLL search path
# Sometimes it is needed to instruct Mono to seek its assemblies from a different path
# (e.g. mscorlib is stripped in original game)
# This option causes Mono to seek mscorlib and core libraries from a different folder before Managed
# Original Managed folder is added as a secondary folder in the search path
dllSearchPathOverride=


# Settings related to bootstrapping a custom Mono runtime
# Do not use this in managed games!
# These options are intended running custom mono in IL2CPP games!
[MonoBackend]
# Path to main mono.dll runtime library
runtimeLib=
# Path to mono config/etc directory
configDir=
# Path to core managed assemblies (mscorlib et al.) directory
corlibDir=
# Specifies whether the mono soft debugger is enabled
debugEnabled=false
# Specifies whether the mono soft debugger should suspend the process and wait for the remote debugger
debugSuspend=false
# Specifies the listening address the soft debugger
debugAddress=127.0.0.1:10000";
        #endregion

        public const string DefaultFileName = "doorstop_config.ini";

        public static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    _filePath = Path.Combine(Program.VTOLFolder, DefaultFileName);
                }

                return _filePath;
            }
        }
        private static string _filePath = string.Empty;

        public static void Enable()
        {
            if (!FileExists())
            {
                CreateDefaultFile();
                return;
            }
            ToggleDoorstep(true);
        }

        public static void Disable()
        {
            if (!FileExists())
            {
                CreateDefaultFile();
            }
            ToggleDoorstep(false);
        }

        public static bool FileExists() => File.Exists(FilePath);

        public static void CreateDefaultFile()
        {
            Console.Log("Writing Default doorstep config");
            File.WriteAllText(FilePath, DefaultContent);
        }

        private static void ToggleDoorstep(bool value)
        {
            ConfigParser config = new(FilePath);
            config.SetValue("UnityDoorstop", "enabled", value);
            config.Save();
        }
    }
}