using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Core;
using ModLoader;
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
        private static bool _loadedModLoader = false;
        public static DirectoryInfo ModLoaderFolder;

        /// <summary>
        /// Entry Point from Doorstep
        /// </summary>
        public static void Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            if (!Startup())
            {
                return;
            }

            Logger.Log("Patching UnityEngine.Camera");

            try
            {
                RemoveCameraPatch();
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
                Logger.Error($"Failed to Patch VTOL VRs Code\n{e}");
                return;
            }

            stopwatch.Stop();
            Logger.Log($"Finished in {stopwatch.Elapsed}");
        }

        private static bool Startup()
        {
            SetFolder();
            Logger.CreateLogs();

            if (!LoadAssemblies())
            {
                Logger.Error($"Failed to load Assemblies!");
                return false;
            }

            if (!IsValidGame())
            {
                Logger.Log("Please use the steam version of VTOL to play with mods");
                return false;
            }

            return true;
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

        private static void RemoveCameraPatch()
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
            
            bool changed = false;
            if (cctor != null)
            {
                // The old version used to patch the camera constructor. 
                // This caused issues later on so I moved it to splash screen
                Logger.Log("There was a cctor method. Checking it");

                for (int i = 0; i < type.Methods.Count; i++)
                {
                    MethodDefinition method = type.Methods[i];
                    if (method.IsRuntimeSpecialName && method.Name == ".cctor")
                    {
                        type.Methods.RemoveAt(i);
                        changed = true;
                        continue;
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
        
        public static void LoadModLoader()
        {
            if (_loadedModLoader)
                return;
            
            _loadedModLoader = true;
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
                
                
                if (false) //!SteamAuthentication.IsTrusted(oldPath))
                {
                    Debug.LogError("Unexpected Error, please contact vtolvr-mods.com staff\nError code: 667970");
                    Debug.Log(oldPath);
                    Application.Quit();
                    return;
                }
                
                PlayerLogText();
                
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
                VirtualizeType(type, ref module);
            }

            module.Write(assemlycsharp);
            module.Dispose();
        }

        private static void VirtualizeType(TypeDefinition type, ref ModuleDefinition module)
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
                VirtualizeType(nestedType, ref module);
            }

            foreach (MethodDefinition method in type.Methods)
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

                if (type.Name.Equals(nameof(GameVersion)) && method.Name.Equals(nameof(GameVersion.ConstructFromValue)))
                {
                    // As of the official multiplayer we need to make sure modded players do not join
                    // non modded players. BD added GameVersion.ReleaseType.Modded. This section of code
                    // is hard coded to change everyone to that release type.
                    
                    // It was hard coded because I couldn't figure out when adding a new line 
                    // how to set the value type in IL Code

                    try
                    {
                        var ilp = method.Body.GetILProcessor();
                        Instruction instruction = method.Body.Instructions[66];
                        Instruction newInstruction  = Instruction.Create(OpCodes.Ldc_I4_2);
                        ilp.Replace(instruction, newInstruction);
                        instruction = method.Body.Instructions[68];
                        ilp.Replace(instruction, newInstruction);
                    }
                    catch
                    {
                        Logger.Log($"This game version isn't the public testing version");
                    }
                    
                }

                // Patching Splashscreen instead of Camera Constructor so it doesn't run every scene
                if (type.Name.Equals(nameof(SplashSceneController)) &&
                    method.Name.Equals("Start"))
                {
                    Logger.Log($"{type.Name}|{method.Name}");
                    MethodReference cbs = module.ImportReference(((Action)LoadModLoader).Method);
                    ILProcessor ilp = method.Body.GetILProcessor();
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        Instruction ins = method.Body.Instructions[i];
                        Logger.Log($"{i}|{ins.OpCode}");
                        if (ins.OpCode == OpCodes.Ret)
                        {
                            Logger.Log("Added Method before OpCode.Pop");
                            ilp.InsertBefore(ins, ilp.Create(OpCodes.Call, cbs));
                            break;
                        }
                    }
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
            return true;
            //return WinVerifyTrust(fileName) == 0;
        }
        
        // This is the entry point for the console and unpatching
        
    }
}