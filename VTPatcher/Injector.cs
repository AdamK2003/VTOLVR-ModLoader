using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using VTPatcher.Classes;
using VTPatcher.Enums;
using Debug = UnityEngine.Debug;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace VTPatcher
{
    public static class Injector
    {
        public static DirectoryInfo ModLoaderFolder;

        /// <summary>
        /// Entry Point from Doorstep
        /// </summary>
        public static void Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SetFolder();
            Logger.CreateLogs();

            if (!LoadAssemblies())
            {
                Logger.Error($"Failed to load Assemblies!");
                return;
            }

            if (!IsValidGame())
            {
                Logger.Log("Please use the steam version of VTOL to play with mods");
                return;
            }

            Logger.Log("Patching UnityEngine.Camera");

            try
            {
                PatchCameraClass();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to Patch Camera Class.\n{e.Message}");
                return;
            }
            
            Logger.Log("Patching VTOL VRs Code");

            try
            {
                PatchVTOL();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to Patch VTOL VRs Code\n{e.Message}");
                return;
            }

            stopwatch.Stop();
            Logger.Log($"Finished in {stopwatch.Elapsed}");
        }

        private static void SetFolder()
        {
            FileInfo assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
            ModLoaderFolder = assembly.Directory;
        }

        private static bool LoadAssemblies()
        {
            Logger.Log($"Loading Assemblies");
            string path = Path.Combine(ModLoaderFolder.FullName, "Mono.Cecil.dll");
            Logger.Log($"Loading {path}");
            if (File.Exists(path))
            {

                Assembly.LoadFile(path);
                return true;
            }

            return false;
        }

        private static bool IsValidGame()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Plugins",
                "steam_api64.dll");
            if (File.Exists(path))
            {
                return IsTrusted(path);
            }
            
            path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Plugins",
                "x86_64");
            string newPath = Path.Combine(path, "steam_api64.dll");
            
            if (Directory.Exists(path) && File.Exists(newPath))
            {
                return IsTrusted(newPath);
            }
            
            return true;
        }

        private static void PatchCameraClass()
        {
            string coreModulePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "VTOLVR_Data",
                "Managed",
                "UnityEngine.CoreModule.dll");

            AssemblyDefinition unityAD = AssemblyDefinition.ReadAssembly(
                coreModulePath,
                new ReaderParameters() {ReadWrite = false, InMemory = true, ReadingMode = ReadingMode.Immediate});

            ModuleDefinition module = unityAD.MainModule;
            TypeDefinition type = module.GetType("UnityEngine", "Camera");

            if (type == null)
            {
                Logger.Error("Couldn't get UnityEngine.Camera");
                return;
            }

            MethodDefinition cctor = null;
            foreach (var method in type.Methods)
            {
                if (method.IsRuntimeSpecialName && method.Name == ".cctor")
                    cctor = method;
            }

            MethodReference cbs = module.ImportReference(((Action)LoadModLoader).Method);
            bool changed = false;
            if (cctor == null)
            {
                Logger.Log("There was no cctor method. Creating a new one");

                cctor = new MethodDefinition(".cctor",
                    MethodAttributes.RTSpecialName | MethodAttributes.Static | MethodAttributes.SpecialName,
                    module.TypeSystem.Void);
                type.Methods.Add(cctor);

                // Unsure what this is
                var ilp = cctor.Body.GetILProcessor();
                ilp.Emit(OpCodes.Call, cbs);
                ilp.Emit(OpCodes.Ret);

                changed = true;
            }
            else
            {
                // If the constructor method already exsits,
                // we want to change the first two IL codes to call and return
                Logger.Log("There was a cctor method. Checking it");

                var ilp = cctor.Body.GetILProcessor();
                for (int i = 0; i < Math.Min(2, cctor.Body.Instructions.Count); i++)
                {
                    Instruction ins = cctor.Body.Instructions[i];
                    switch (i)
                    {
                        case 0 when ins.OpCode != OpCodes.Call:
                            ilp.Replace(ins, ilp.Create(OpCodes.Call, cbs));
                            changed = true;
                            break;
                        case 0:
                            var methodRef = ins.Operand as MethodReference;
                            if (methodRef?.FullName != cbs.FullName)
                            {
                                ilp.Replace(ins, ilp.Create(OpCodes.Call, cbs));
                                changed = true;
                            }

                            break;
                        case 1 when ins.OpCode != OpCodes.Ret:
                            ilp.Replace(ins, ilp.Create(OpCodes.Ret));
                            changed = true;
                            break;
                    }
                }
            }

            if (changed)
            {
                Logger.Log($"Writing Patch to {coreModulePath}");
                unityAD.Write(coreModulePath);
            }

            unityAD.Dispose();
        }

        private static void LoadModLoader()
        {
            string modloaderPath = Path.Combine("VTOLVR_ModLoader", "ModLoader.dll");
            if (Directory.Exists("VTOLVR_ModLoader") &&
                File.Exists(modloaderPath))
            {
                CrashReportHandler.enableCaptureExceptions = false;

                string oldPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "VTOLVR_Data",
                    "Plugins",
                    "x86_64");
                if (!Directory.Exists(oldPath))
                {
                    oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "VTOLVR_Data",
                        "Plugins",
                        "steam_api64.dll");
                }
                else
                {
                    oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "VTOLVR_Data",
                        "Plugins",
                        "x86_64",
                        "steam_api64.dll");
                }
                
                if (!SteamAuthentication.IsTrusted(oldPath))
                {
                    Debug.LogError("Unexpected Error, please contact vtolvr-mods.com staff\nError code: 667970");
                    Debug.Log(oldPath);
                    Application.Quit();
                    return;
                }
                
                PlayerLogText();
                if (GameStartup.version.releaseType == GameVersion.ReleaseTypes.Testing)
                {
                    Debug.Log($"This user is running modified game on the public testing branch. The Mod Loader has stopped running but is still technically loaded.");
                    return;
                }
                
                Assembly.LoadFile(modloaderPath);
                new GameObject("Mod Loader Manager",
                    typeof(ModLoader.ModLoaderManager),
                    typeof(ModLoader.SkinManager));
            }
            else
            {
                UnityEngine.Debug.Log($"I can't find mod loader.dll at {modloaderPath}");
            }

        }

        private static void PlayerLogText()
        {
            string playerLogMessage = @" 
                                                                                                         
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #     #                                              #     #                                            
 ##   ##   ####   #####   #####   ######  #####       #     #  ######  #####    ####   #   ####   #    # 
 # # # #  #    #  #    #  #    #  #       #    #      #     #  #       #    #  #       #  #    #  ##   # 
 #  #  #  #    #  #    #  #    #  #####   #    #      #     #  #####   #    #   ####   #  #    #  # #  # 
 #     #  #    #  #    #  #    #  #       #    #       #   #   #       #####        #  #  #    #  #  # # 
 #     #  #    #  #    #  #    #  #       #    #        # #    #       #   #   #    #  #  #    #  #   ## 
 #     #   ####   #####   #####   ######  #####          #     ######  #    #   ####   #   ####   #    # 

Thank you for download VTOL VR Mod loader by . Marsh.Mello .

Please don't report bugs unless you can reproduce them without any mods loaded
if you are having any issues with mods and would like to report a bug, please contact @. Marsh.Mello .#0001 
on the offical VTOL VR Discord or post an issue on gitlab. 

VTOL VR Modding Discord Server: https://discord.gg/XZeeafp
Mod Loader Gitlab: https://gitlab.com/vtolvr-mods/ModLoader
Mod Loader Website: https://vtolvr-mods.com/

Special Thanks to Ketkev and Nebriv for their continuous support to the mod loader and the website.

 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
";
            UnityEngine.Debug.Log(playerLogMessage);
        }

        private static void PatchVTOL()
        {
            string assemlycsharp = Path.Combine(
                ModLoaderFolder.Parent.FullName,
                "VTOLVR_Data",
                "Managed",
                "Assembly-CSharp.dll");
            ModuleDefinition module = ModuleDefinition.ReadModule(
                assemlycsharp,
                new ReaderParameters() {ReadWrite = false, InMemory = true, ReadingMode = ReadingMode.Immediate});

            foreach (var type in module.Types)
            {
                VirtualizeType(type);
            }

            module.Write(assemlycsharp);
            module.Dispose();
        }

        private static void VirtualizeType(TypeDefinition type)
        {
            if (type.IsSealed)
                type.IsSealed = false;

            if (type.IsNestedPrivate)
            {
                type.IsNestedPrivate = false;
                type.IsNestedPublic = true;
            }

            if (type.IsInterface || type.IsAbstract)
                return;

            foreach (var nestedType in type.NestedTypes)
            {
                VirtualizeType(nestedType);
            }

            foreach (var method in type.Methods)
            {
                if (method.IsManaged
                    && method.IsIL
                    && !method.IsStatic
                    && (!method.IsVirtual || method.IsFinal)
                    && !method.IsAbstract
                    && !method.IsAddOn
                    && !method.IsConstructor
                    && !method.IsSpecialName
                    && !method.IsGenericInstance
                    && !method.HasOverrides)
                {

                    method.IsVirtual = true;
                    method.IsFinal = false;
                    method.IsPublic = true;
                    method.IsPrivate = false;
                    method.IsNewSlot = true;
                    method.IsHideBySig = true;
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.IsPrivate)
                    field.IsFamily = true;
            }
        }
        
        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);
        
        private static uint WinVerifyTrust(string fileName)
        {

            Guid wintrust_action_generic_verify_v2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
            uint result = 0;
            using (WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO(fileName,
                Guid.Empty))
            using (UnmanagedPointer guidPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid))),
                AllocMethod.HGlobal))
            using (UnmanagedPointer wvtDataPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA))),
                AllocMethod.HGlobal))
            {
                WINTRUST_DATA data = new WINTRUST_DATA(fileInfo);
                IntPtr pGuid = guidPtr;
                IntPtr pData = wvtDataPtr;
                Marshal.StructureToPtr(wintrust_action_generic_verify_v2,
                    pGuid,
                    true);
                Marshal.StructureToPtr(data,
                    pData,
                    true);
                result = WinVerifyTrust(IntPtr.Zero,
                    pGuid,
                    pData);

            }
            return result;

        }
        
        private static bool IsTrusted(string fileName)
        {
            return WinVerifyTrust(fileName) == 0;
        }
    }
}