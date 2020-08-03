using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Build
{
    class Program
    {
        private static string dir, templateFolder,dlls;
        private static List<Process> processes = new List<Process>();
        private static Dictionary<string, string> paths = new Dictionary<string, string>()
        {
            { "msbuild", @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"},
            { "unity", @"C:\Program Files\Unity\Hub\Editor\2019.1.8f1\Editor\Unity.exe"},
            { "nuget", @"C:\Program Files\nuget.exe" }
        };
        static void Main(string[] args)
        {
            dir = Directory.GetCurrentDirectory();
            CheckArgs(args);
        }
        private static void CheckArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("template="))
                {
                    templateFolder = args[i].Replace("template=", string.Empty);
                }
                if (args[i].Contains("dlls="))
                {
                    dlls = args[i].Replace("dlls=", string.Empty);
                    MoveDeps();
                }
            }

            if (args.Contains("builddll"))
                BuildDLL();
            else if (args.Contains("buildwpf"))
                BuildWPFApp();
            else if (args.Contains("buildassets"))
                BuildAssetBundle();
            else if (args.Contains("buildupdater"))
                BuildUpdater();
            else if (args.Contains("zip"))
                ZIPContents();
            else if (args.Contains("buildinstaller"))
                BuildInstaller();
            else if (args.Contains("autoupdatezip"))
                CreateUpdaterZip();
        }
        private static void MoveDeps()
        {
            string[] deps = Directory.GetFiles(dlls, "*.dll", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < deps.Length; i++)
            {
                File.Copy(deps[i], deps[i].Replace(dlls, dir + @"\dll"));
            }
            Log("Moved " + (deps.Length + 1) + " dependencies");
        }
        private static void BuildDLL()
        {
            Log("Building ModLoader.dll\n");
            Run("cmd.exe",
                "/c dotnet build --configuration Release",
                @"\ModLoader");
        }
        private static void BuildWPFApp()
        {
            Log("Building VTOLVR-ModLoader.exe\n");
            Run(paths["nuget"],
                "restore -SolutionDirectory " + dir,
                @"\VTOLVR-ModLoader");
            Run(paths["msbuild"],
                "VTOLVR-ModLoader.csproj -property:Configuration=Release;TargetFrameworkVersion=4.6 -tv:14.0",
                @"\VTOLVR-ModLoader");
        }

        private static void BuildAssetBundle()
        {
            Log("Building ModLoader.assets");
            Run(paths["unity"],
                "-quit -batchmode -projectPath -executeMethod ModdingUtilitys.BuildAllAssetBundles",
                @"\VTOLVR Unity Project");
        }

        private static void BuildUpdater()
        {
            Log("Building Updater");
            Run(paths["msbuild"],
                "Updater.csproj -property:Configuration=Release;TargetFrameworkVersion=4.6 -tv:14.0",
                @"\Updater");
        }

        private static void ZIPContents()
        {
            Log("Zipping Contents");

            if (string.IsNullOrEmpty(templateFolder))
            {
                Log("ERROR: 'template' arg missing");
                Environment.Exit(1);
                return;
            }

            //Copy all folders
            foreach (string dirPath in Directory.GetDirectories(templateFolder, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(templateFolder, dir + @"\temp"));
            //Copy all files
            foreach (string newPath in Directory.GetFiles(templateFolder, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(templateFolder, dir + @"\temp"), true);

            Directory.CreateDirectory(dir + @"\temp\VTOLVR_Data\Managed");
            Directory.CreateDirectory(dir + @"\temp\VTOLVR_Data\Plugins");
            Directory.CreateDirectory(dir + @"\temp\VTOLVR_ModLoader\mods");
            Directory.CreateDirectory(dir + @"\temp\VTOLVR_ModLoader\skins");

            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.dll", dir + @"\temp\VTOLVR_ModLoader\ModLoader.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.xml", dir + @"\temp\VTOLVR_ModLoader\ModLoader.xml");
            TryMove(dir + @"\VTOLVR-ModLoader\bin\Release\VTOLVR-ModLoader.exe", dir + @"\temp\VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
            TryMove(dir + @"\Updater\bin\Release\Updater.exe", dir + @"\temp\VTOLVR_ModLoader\Updater.exe");
            //TryMove(dir + @"\VTOLVR Unity Project\Assets\_ModLoader\Exported Asset Bundle\modloader.assets", dir + @"\temp\VTOLVR_ModLoader\VTOLVR-modloader.assets");

            TryDelete(dir + @"\Installer\Resources\ModLoader.zip");
            ZipFile.CreateFromDirectory(dir + @"\temp\", dir + @"\Installer\Resources\ModLoader.zip");
            Directory.Delete(dir + @"\temp", true);
        }

        private static void BuildInstaller()
        {
            Log("Building Installer.exe");
            Run(paths["msbuild"],
                "Installer.csproj -property:Configuration=Release;TargetFrameworkVersion=4.6 -tv:14.0",
                @"\Installer");
        }

        private static void CreateUpdaterZip()
        {
            Log("Creating zip for auto updater");
            if (string.IsNullOrEmpty(templateFolder))
            {
                Log("ERROR: 'template' arg missing");
                Environment.Exit(1);
                return;
            }

            //Copy all folders
            foreach (string dirPath in Directory.GetDirectories(templateFolder, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(templateFolder, dir + @"\autoupdate"));
            //Copy all files
            foreach (string newPath in Directory.GetFiles(templateFolder, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(templateFolder, dir + @"\autoupdate"), true);

            Directory.CreateDirectory(dir + @"\autoupdate\VTOLVR_Data\Managed");
            Directory.CreateDirectory(dir + @"\autoupdate\VTOLVR_Data\Plugins");
            Directory.CreateDirectory(dir + @"\autoupdate\VTOLVR_ModLoader\mods");
            Directory.CreateDirectory(dir + @"\autoupdate\VTOLVR_ModLoader\skins");

            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.dll", dir + @"\autoupdate\VTOLVR_ModLoader\ModLoader.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.xml", dir + @"\autoupdate\VTOLVR_ModLoader\ModLoader.xml");
            TryMove(dir + @"\VTOLVR-ModLoader\bin\Release\VTOLVR-ModLoader.exe", dir + @"\autoupdate\VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
            TryMove(dir + @"\Updater\bin\Release\Updater.exe", dir + @"\autoupdate\VTOLVR_ModLoader\Updater.exe");

            ZipFile.CreateFromDirectory(dir + @"\autoupdate\", dir + @"\autoupdate.zip");
        }

        private static bool TryMove(string sourceFileName, string destFileName)
        {
            try
            {
                File.Move(sourceFileName, destFileName);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to move a file (" + sourceFileName + ")");
                Console.WriteLine(e.ToString());
                Environment.Exit(2);
                return false;
            }
        }
        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }

        
        private static void Run(string file,string args, string workingDirectory)
        {
            Process process = new Process();
            processes.Add(process);
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = file;
            startInfo.Arguments = args;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WorkingDirectory = dir + workingDirectory;
            process.StartInfo = startInfo;
            process.Start();
            string output;
            while ((output = process.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(output);
            }
            process.WaitForExit();
            if (process.ExitCode != 0)
                Environment.Exit(process.ExitCode);
        }

        private static void Log(object message)
        {
            Console.WriteLine(message);
        }
    }
}
