﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Class for managing the loading of assemblies into the engine.
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        ///     Run levels of the Content entry point.
        /// </summary>
        public enum RunLevel
        {
            Error = 0,
            Init = 1
        }

        /// <summary>
        ///     Levels at which point the content assemblies are getting updates.
        /// </summary>
        public enum UpdateLevel
        {
            /// <summary>
            ///     This update is called before the main state manager on process frames.
            /// </summary>
            PreEngine,

            /// <summary>
            ///     This update is called before the main state manager on render frames, thus only applies to the client.
            /// </summary>
            FramePreEngine,

            /// <summary>
            ///     This update is called after the main state manager on process frames.
            /// </summary>
            PostEngine,

            /// <summary>
            ///     This update is called after the main state manager on render frames, thus only applies to the client.
            /// </summary>
            FramePostEngine,
        }

        /// <summary>
        ///     Loaded assemblies.
        /// </summary>
        private static readonly List<ModInfo> _mods = new List<ModInfo>();

        static AssemblyLoader()
        {
            // Make it attempt to use already loaded assemblies,
            // as .NET doesn't do it automatically.
            // (It attempts to load dependencies from disk next to executable only)
            AppDomain.CurrentDomain.AssemblyResolve += ResolveMissingAssembly;
        }

        private static Assembly ResolveMissingAssembly(object sender, ResolveEventArgs args)
        {
            foreach (var mod in _mods)
            {
                if (mod.GameAssembly.FullName == args.Name)
                    return mod.GameAssembly;
            }
            return null;
        }

        /// <summary>
        ///     Gets an assembly by name from the given AppDomain.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Assembly GetAssemblyByName(this AppDomain domain, string name)
        {
            return domain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        /// <summary>
        ///     Loads an assembly into the current AppDomain.
        /// </summary>
        /// <typeparam name="T">The type of the entry point to search for.</typeparam>
        /// <param name="assembly">Byte array of the assembly.</param>
        /// <param name="symbols">Optional byte array of the debug symbols.</param>
        public static void LoadGameAssembly<T>(byte[] assembly, byte[] symbols = null)
            where T : GameShared
        {
            // TODO: Re-enable type check when it's not just a giant pain in the butt.
            // It slows down development too much and we need other things like System.Type fixed
            // before it can reasonably be re-enabled.
            AssemblyTypeChecker.DisableTypeCheck = true;
            AssemblyTypeChecker.DumpTypes = true;
            if (!AssemblyTypeChecker.CheckAssembly(assembly))
                return;

            var mod = new ModInfo();

            mod.GameAssembly = symbols != null
                ? AppDomain.CurrentDomain.Load(assembly, symbols)
                : AppDomain.CurrentDomain.Load(assembly);

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(mod.GameAssembly);

            var entryPoints = mod.GameAssembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).ToArray();

            if (entryPoints.Length == 0)
                Logger.WarningS("res", $"Assembly has no entry points: {mod.GameAssembly.FullName}");

            foreach (var entryPoint in entryPoints)
            {
                mod.EntryPoints.Add(Activator.CreateInstance(entryPoint) as T);
            }

            _mods.Add(mod);
        }

        /// <summary>
        ///     Broadcasts a run level change to all loaded entry point.
        /// </summary>
        /// <param name="level">New level</param>
        public static void BroadcastRunLevel(RunLevel level)
        {
            foreach (var mod in _mods)
            {
                foreach (var entry in mod.EntryPoints)
                {
                    switch (level)
                    {
                        case RunLevel.Init:
                            entry.Init();
                            break;
                        default:
                            Logger.ErrorS("res", $"Unknown RunLevel: {level}");
                            break;
                    }
                }
            }
        }

        public static void BroadcastUpdate(UpdateLevel level, float frameTime)
        {
            foreach (var entrypoint in _mods.SelectMany(m => m.EntryPoints))
            {
                entrypoint.Update(level, frameTime);
            }
        }

        /// <summary>
        ///     Holds info about a loaded assembly.
        /// </summary>
        private class ModInfo
        {
            public ModInfo()
            {
                EntryPoints = new List<GameShared>();
            }

            public Assembly GameAssembly { get; set; }
            public List<GameShared> EntryPoints { get; }
        }

        /// <summary>
        ///     Tries to load an assembly from a resource manager into the current appdomain.
        /// </summary>
        /// <typeparam name="T">The type of the entry point to search for.</typeparam>
        /// <param name="resMan">The assembly will be located inside of this resource manager.</param>
        /// <param name="assemblyName">File name of the assembly inside of the ./Assemblies folder in the resource manager.</param>
        /// <returns>If the assembly was successfully located and loaded.</returns>
        public static bool TryLoadAssembly<T>(IResourceManager resMan, string assemblyName)
            where T : GameShared
        {
            // get the assembly from the file system
            if (resMan.TryContentFileRead(new ResourcePath($@"/Assemblies/{assemblyName}.dll"), out var gameDll))
            {
                Logger.DebugS("srv", $"Loading {assemblyName} DLL");

                // see if debug info is present
                if (resMan.TryContentFileRead(new ResourcePath($@"/Assemblies/{assemblyName}.pdb") , out var gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("srv", $"Exception loading DLL {assemblyName}.dll: {e}");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        LoadGameAssembly<T>(gameDll.ToArray());
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("srv", $"Exception loading DLL {assemblyName}.dll: {e}");
                        return false;
                    }
                }
            }
            else
            {
                Logger.Warning("eng", $"Could not load {assemblyName} DLL.");
                return false;
            }
        }
    }
}
