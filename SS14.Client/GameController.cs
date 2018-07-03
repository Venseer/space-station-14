﻿using SS14.Client.Console;
using SS14.Client.GodotGlue;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Log;
using SS14.Client.State.States;
using SS14.Shared.ContentPack;
using SS14.Shared.Input;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using SS14.Shared.Interfaces.Resources;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    public sealed partial class GameController : ClientEntryPoint, IGameController
    {
        /// <summary>
        ///     QueueFreeing a Godot node during finalization can cause segfaults.
        ///     As such, this var is set as soon as we tell Godot to shut down proper.
        /// </summary>
        public static bool ShuttingDownHard { get; private set; } = false;

        [Dependency]
        readonly IConfigurationManager _configurationManager;
        [Dependency]
        readonly IResourceCache _resourceCache;
        [Dependency]
        readonly ISceneTreeHolder _sceneTreeHolder;
        [Dependency]
        readonly IResourceManager _resourceManager;
        [Dependency]
        readonly ISS14Serializer _serializer;
        [Dependency]
        readonly IPrototypeManager _prototypeManager;
        [Dependency]
        readonly IClientTileDefinitionManager _tileDefinitionManager;
        [Dependency]
        readonly IClientNetManager _networkManager;
        [Dependency]
        readonly IMapManager _mapManager;
        [Dependency]
        readonly IStateManager _stateManager;
        [Dependency]
        readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly IBaseClient _client;
        [Dependency]
        readonly IInputManager inputManager;
        [Dependency]
        readonly IClientChatConsole _console;
        [Dependency]
        readonly ILightManager lightManager;
        [Dependency]
        readonly IDisplayManager displayManager;
        [Dependency]
        readonly ITimerManager _timerManager;
        [Dependency]
        readonly IClientEntityManager _entityManager;
        [Dependency]
        readonly IEyeManager eyeManager;
        [Dependency]
        readonly GameTiming gameTiming;
        [Dependency]
        readonly IPlacementManager placementManager;
        [Dependency]
        readonly IClientGameStateManager gameStateManager;
        [Dependency]
        readonly IOverlayManager overlayManager;

        public override void Main(Godot.SceneTree tree)
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif

            PreInitIoC();
            IoCManager.Resolve<ISceneTreeHolder>().Initialize(tree);
            InitIoC();
            Godot.OS.SetWindowTitle("Space Station 14");
            SetupLogging();

            tree.SetAutoAcceptQuit(false);

            // Load config.
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            displayManager.Initialize();

            // Init resources.
            // Doesn't do anything right now because TODO Godot asset management is a bit ad-hoc.
            _resourceCache.LoadBaseResources();
            _resourceCache.LoadLocalResources();

            //identical code for server in baseserver
            if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resourceManager, $"Content.Shared")
                && !AssemblyLoader.TryLoadAssembly<GameShared>(_resourceManager, $"Sandbox.Shared"))
            {
                Logger.Warning($"[ENG] Could not load any Shared DLL.");
            }

            if (!AssemblyLoader.TryLoadAssembly<GameClient>(_resourceManager, $"Content.Client")
                && !AssemblyLoader.TryLoadAssembly<GameClient>(_resourceManager, $"Sandbox.Client"))
            {
                Logger.Warning($"[ENG] Could not load any Client DLL.");
            }

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            eyeManager.Initialize();
            _serializer.Initialize();
            _userInterfaceManager.Initialize();
            _tileDefinitionManager.Initialize();
            _networkManager.Initialize(false);
            inputManager.Initialize();
            _console.Initialize();
            _prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes/"));
            _prototypeManager.Resync();
            _mapManager.Initialize();
            placementManager.Initialize();
            lightManager.Initialize();
            _entityManager.Initialize();
            gameStateManager.Initialize();
            overlayManager.Initialize();

            _client.Initialize();

            _stateManager.RequestStateChange<MainScreen>();
        }

        public override void QuitRequest()
        {
            Shutdown("OS quit request");
        }

        public void Shutdown(string reason = null)
        {
            if (reason != null)
            {
                Logger.Info($"Shutting down! Reason: {reason}");
            }
            else
            {
                Logger.Info("Shutting down!");
            }
            Logger.Debug("Goodbye");
            IoCManager.Clear();
            ShuttingDownHard = true;
            _sceneTreeHolder.SceneTree.Quit();
        }

        public override void PhysicsProcess(float delta)
        {
            // Can't be too certain.
            gameTiming.InSimulation = true;
            gameTiming._tickRemainderTimer.Restart();
            try
            {
                if (!gameTiming.Paused)
                {
                    gameTiming.CurTick++;
                    _networkManager.ProcessPackets();
                    var eventArgs = new ProcessFrameEventArgs(delta);
                    AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, eventArgs.Elapsed);
                    _timerManager.UpdateTimers(delta);
                    _userInterfaceManager.Update(eventArgs);
                    _stateManager.Update(eventArgs);
                    AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, eventArgs.Elapsed);
                }
            }
            finally
            {
                gameTiming.InSimulation = false;
            }
        }

        public override void FrameProcess(float delta)
        {
            gameTiming.InSimulation = false; // Better safe than sorry.
            gameTiming.RealFrameTime = TimeSpan.FromSeconds(delta);
            gameTiming.TickRemainder = gameTiming._tickRemainderTimer.Elapsed;

            var eventArgs = new RenderFrameEventArgs(delta);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePreEngine, eventArgs.Elapsed);
            lightManager.FrameUpdate(eventArgs);
            _stateManager.FrameUpdate(eventArgs);
            overlayManager.FrameUpdate(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePostEngine, eventArgs.Elapsed);
        }

        private void SetupLogging()
        {
            var mgr = IoCManager.Resolve<ILogManager>();
            mgr.RootSawmill.AddHandler(new GodotLogHandler());
            mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
        }
    }
}
