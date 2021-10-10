using System;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace VTPatcherConsole
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                UnPatchGame();
            }
            catch (Exception e)
            {
                Console.WriteLine($"FAILED to unpatch game");
                Console.WriteLine(e);
            }
        }
        
        public static void UnPatchGame()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // GetCurrentDirectory() will return the VTOLVR_ModLoader folder
            DirectoryInfo vtolFolder = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;
            
            string assemlycsharp = Path.Combine(
                vtolFolder.FullName,
                "VTOLVR_Data",
                "Managed",
                "Assembly-CSharp.dll");

            if (!File.Exists(assemlycsharp))
            {
                Console.WriteLine($"For some reason, I couldn't find the Assembly-CSharp.dll at {assemlycsharp}");
                return;
            }
            
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(
                assemlycsharp, 
                new ReaderParameters() {ReadWrite = false, InMemory = true, ReadingMode = ReadingMode.Immediate});

            ModuleDefinition module = assembly.MainModule;

            const string gameVersion = "GameVersion";
            const string gameVersionMethod = "ConstructFromValue";
            const string splashSceneController = "SplashSceneController";
            const string splashSceneControllerMethod = "Start";
            bool unpatchedGameVersion = false;
            bool unPatchedEntry = false;
            bool changed = false;
            
            foreach (TypeDefinition type in module.Types)
            {
                /* Saving this for unpatching the multiplayer version
                if (!unpatchedGameVersion && type.Name.Equals(gameVersion))
                {
                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (!method.Name.Equals(gameVersionMethod))
                        {
                            continue;
                        }

                        // Hard Coded - Could easily break
                        // If BD changes the function `GameVersion.ConstructFromValue`
                        // The numbers 66 and 68 could be different and this would break

                        for (int i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            Console.WriteLine($"{i}|{method.Body.Instructions[i].OpCode}");
                        }
                        
                        var ilp = method.Body.GetILProcessor();
                        Instruction instruction = method.Body.Instructions[66];
                        ilp.Replace(instruction, Instruction.Create(OpCodes.Ldc_I4_0));
                        instruction = method.Body.Instructions[68];
                        ilp.Replace(instruction, Instruction.Create(OpCodes.Ldc_I4_1));
                            
                        unpatchedGameVersion = true;
                    }
                }
                */
                
                if (!unPatchedEntry && type.Name.Equals(splashSceneController))
                {
                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (!method.Name.Equals(splashSceneControllerMethod))
                        {
                            continue;
                        }
                        ILProcessor ilp = method.Body.GetILProcessor();
                        int calls = 0;
                        for (int i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            Instruction ins = method.Body.Instructions[i];
                            Console.WriteLine($"{i}|{ins.OpCode}");
                            if (ins.OpCode == OpCodes.Call)
                                calls++;

                            if (ins.OpCode != OpCodes.Ret)
                            {
                                continue;
                            }

                            // This means the start method only has the two functions,
                            // not the third modded one
                            if (calls == 4)
                                break;
                            
                            Console.WriteLine("Removing method");
                            Instruction target = ins.Previous;
                            ilp.Remove(target);
                            changed = true;
                            break;
                        }
                    }
                    
                }
            }

            if (changed)
            {
                Console.WriteLine("Writing Changes");
                assembly.Write(assemlycsharp);
            }
            Console.WriteLine("UnPatched Game");
            
            stopwatch.Stop();
            Console.WriteLine($"Finished in {stopwatch.Elapsed}");
        }
    }
}