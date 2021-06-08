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
        private static string dir, templateFolder, dlls;
        private static List<Process> processes = new List<Process>();
        private static Dictionary<string, string> paths = new Dictionary<string, string>()
        {
            { "msbuild", @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"},
            { "unity", @"C:\Program Files\Unity\Hub\Editor\2019.1.8f1\Editor\Unity.exe"},
            { "nuget", @"B:\Gitlab Runner\nuget.exe" }
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
            else if (args.Contains("move"))
                MoveToDesktop();
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
            Log("Building Core.dll");
            Run($"\"{paths["nuget"]}\"",
                $"restore",
                @"");
            Run(paths["msbuild"],
                "-p:Configuration=Release -nologo CoreCore.csproj /t:Restore /t:Clean,Build ",
                @"\CoreCore");
            Log("Building ModLoader.dll\n");
            Run(paths["msbuild"],
                "-p:Configuration=Release;Documentationfile=bin\\Release\\ModLoader.xml -nologo \"Mod Loader.csproj\"",
                @"\ModLoader");
        }
        private static void BuildWPFApp()
        {
            Log("Building VTOLVR-ModLoader.exe\n");
            Run($"\"{paths["nuget"]}\"",
                $"restore",
                @"");
            Run(paths["msbuild"],
                "-p:Configuration=Release -nologo LauncherCore.csproj /t:Restore",
                @"\LauncherCore");
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
            Run($"\"{paths["nuget"]}\"",
                $"restore",
                @"");
            Run(paths["msbuild"],
                "-p:Configuration=Release -nologo UpdaterCore.csproj /t:Restore",
                @"\UpdaterCore");
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

            TryMove(dir + @"\CoreCore\bin\Release\net5.0\CoreCore.dll", dir + @"\temp\VTOLVR_Data\Managed\Core.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.dll", dir + @"\temp\VTOLVR_ModLoader\ModLoader.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.xml", dir + @"\temp\VTOLVR_ModLoader\ModLoader.xml");
            TryMove(dir + @"\LauncherCore\bin\Release\net5.0-windows\win-x64\LauncherCore.exe", dir + @"\temp\VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
            TryMove(dir + @"\UpdaterCore\bin\Release\net5.0-windows\UpdaterCore.exe", dir + @"\temp\VTOLVR_ModLoader\Updater.exe");
            //TryMove(dir + @"\VTOLVR Unity Project\Assets\_ModLoader\Exported Asset Bundle\modloader.assets", dir + @"\temp\VTOLVR_ModLoader\VTOLVR-modloader.assets");

            TryDelete(dir + @"\InstallerCore\Resources\ModLoader.zip");
            ZipFile.CreateFromDirectory(dir + @"\temp\", dir + @"\InstallerCore\Resources\ModLoader.zip");
            Directory.Delete(dir + @"\temp", true);
        }

        private static void BuildInstaller()
        {
            Log("Building Installer.exe");
            Run($"\"{paths["nuget"]}\"",
                $"restore",
                @"");
            Run(paths["msbuild"],
                "-p:Configuration=Release -nologo InstallerCore.csproj /t:Restore",
                @"\InstallerCore");
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
            {
                string directory = dirPath.Replace(templateFolder, dir + @"\autoupdate\template");
                Log($"Creating folder: {directory}");
                Directory.CreateDirectory(directory);
            }
            //Copy all files
            foreach (string newPath in Directory.GetFiles(templateFolder, "*.*",
                SearchOption.AllDirectories))
            {
                string filePath = newPath.Replace(templateFolder, dir + @"\autoupdate\template");
                Log($"Copying file from {newPath} to {filePath}");
                File.Copy(newPath, filePath, true);
            }

            Log("Creating Default folders");
            Directory.CreateDirectory(dir + @"\autoupdate\template\VTOLVR_Data\Managed");
            Directory.CreateDirectory(dir + @"\autoupdate\template\VTOLVR_Data\Plugins");
            Directory.CreateDirectory(dir + @"\autoupdate\template\VTOLVR_ModLoader\mods");
            Directory.CreateDirectory(dir + @"\autoupdate\template\VTOLVR_ModLoader\skins");

            Log("Moving Applications");
            TryMove(dir + @"\CoreCore\bin\Release\net5.0\CoreCore.dll", dir + @"\autoupdate\template\VTOLVR_Data\Managed\Core.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.dll", dir + @"\autoupdate\template\VTOLVR_ModLoader\ModLoader.dll");
            TryMove(dir + @"\ModLoader\bin\Release\ModLoader.xml", dir + @"\autoupdate\template\VTOLVR_ModLoader\ModLoader.xml");
            TryMove(dir + @"\LauncherCore\bin\Release\net5.0-windows\win-x64\LauncherCore.exe", dir + @"\autoupdate\template\VTOLVR_ModLoader\VTOLVR-ModLoader.exe");
            TryMove(dir + @"\UpdaterCore\bin\Release\net5.0-windows\UpdaterCore.exe", dir + @"\autoupdate\template\VTOLVR_ModLoader\Updater.exe");

            Log("Creating zip");
            ZipFile.CreateFromDirectory(dir + @"\autoupdate\", dir + @"\autoupdate.zip");
        }
        private static void MoveToDesktop()
        {
            Log("Moving Files to desktop");
            string root = Path.Combine(
                @"B:\Desktop\",
                "VTOL VR Mod Loader Release");
            Log("Creating Directory");
            Directory.CreateDirectory(root);

            if (File.Exists(Path.Combine(root, "autoupdate.zip")))
            {
                Log("Deleting autoupdate.zip");
                TryDelete(Path.Combine(root, "autoupdate.zip"));
            }
            if (File.Exists(Path.Combine(root, "Installer.exe")))
            {
                Log("Deleting Installer.zip");
                TryDelete(Path.Combine(root, "Installer.exe"));
            }

            Log($"Moving to {root}");
            Log("Moving Autoupdate.zip");
            TryMove(Path.Combine(dir, "autoupdate.zip"), Path.Combine(root, "autoupdate.zip"));
            Log("Moving Installer.exe");
            TryMove(
                Path.Combine(dir, "InstallerCore", "bin", "Release", "net5.0-windows",
                    "win-x64", "InstallerCore.exe"),
                Path.Combine(root, "Installer.exe"));
            Log("Finished");
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


        private static void Run(string file, string args, string workingDirectory)
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
