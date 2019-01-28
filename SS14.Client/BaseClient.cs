﻿using System;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.State;
using SS14.Client.Player;
using SS14.Client.State.States;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace SS14.Client
{
    /// <inheritdoc />
    public class BaseClient : IBaseClient
    {
        [Dependency]
        private readonly IClientNetManager _net;

        [Dependency]
        private readonly IPlayerManager _playMan;

        [Dependency]
        private readonly IStateManager _stateManager;

        [Dependency]
        private readonly IConfigurationManager _configManager;

        [Dependency] private readonly IClientEntityManager _entityManager;

        [Dependency] private readonly IMapManager _mapManager;

        /// <inheritdoc />
        public ushort DefaultPort { get; } = 1212;

        /// <inheritdoc />
        public ClientRunLevel RunLevel { get; private set; }

        /// <inheritdoc />
        public ServerInfo GameInfo { get; private set; }

        /// <inheritdoc />
        public string PlayerNameOverride { get; set; }

        /// <inheritdoc />
        public void Initialize()
        {
            _net.RegisterNetMessage<MsgServerInfo>(MsgServerInfo.NAME, HandleServerInfo);

            _net.Connected += OnConnected;
            _net.ConnectFailed += OnConnectFailed;
            _net.Disconnect += OnNetDisconnect;

            _playMan.Initialize();

            Reset();
        }

        /// <inheritdoc />
        public void ConnectToServer(string ip, ushort port)
        {
            if (RunLevel == ClientRunLevel.Connecting)
            {
                _net.Shutdown("Client mashing that connect button.");
                Reset();
            }
            DebugTools.Assert(RunLevel < ClientRunLevel.Connecting);
            DebugTools.Assert(!_net.IsConnected);

            OnRunLevelChanged(ClientRunLevel.Connecting);
            _net.ClientConnect(ip, port, PlayerNameOverride ?? _configManager.GetCVar<string>("player.name"));
        }

        /// <inheritdoc />
        public void DisconnectFromServer(string reason)
        {
            DebugTools.Assert(RunLevel > ClientRunLevel.Initialize);
            DebugTools.Assert(_net.IsConnected);

            // run level changed in OnNetDisconnect()
            // are both of these *really* needed?
            _net.ClientDisconnect(reason);
        }

        /// <inheritdoc />
        public event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;

        public event EventHandler<PlayerEventArgs> PlayerJoinedServer;
        public event EventHandler<PlayerEventArgs> PlayerJoinedGame;
        public event EventHandler<PlayerEventArgs> PlayerLeaveServer;

        private void OnConnected(object sender, NetChannelArgs args)
        {
            // request base info about the server
            var msgInfo = _net.CreateNetMessage<MsgServerInfoReq>();
            _net.ClientSendMessage(msgInfo);
        }

        /// <summary>
        ///     Player session is fully built, player is an active member of the server. Player is prepared to start
        ///     receiving states when they join the lobby.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedServer(PlayerSession session)
        {
            DebugTools.Assert(RunLevel < ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.Connected);

            _entityManager.Startup();
            _mapManager.Startup();

            PlayerJoinedServer?.Invoke(this, new PlayerEventArgs(session));
        }

        /// <summary>
        ///     Player is joining the game
        /// </summary>
        /// <param name="session">Session of the player.</param>
        private void OnPlayerJoinedGame(PlayerSession session)
        {
            DebugTools.Assert(RunLevel >= ClientRunLevel.Connected);
            OnRunLevelChanged(ClientRunLevel.InGame);

            PlayerJoinedGame?.Invoke(this, new PlayerEventArgs(session));
        }

        private void Reset()
        {
            OnRunLevelChanged(ClientRunLevel.Initialize);
        }

        private void OnConnectFailed(object sender, NetConnectFailArgs args)
        {
            DebugTools.Assert(RunLevel == ClientRunLevel.Connecting);
            Reset();
        }

        private void OnNetDisconnect(object sender, NetChannelArgs args)
        {
            DebugTools.Assert(RunLevel > ClientRunLevel.Initialize);

            PlayerLeaveServer?.Invoke(this, new PlayerEventArgs(_playMan.LocalPlayer.Session));

            _stateManager.RequestStateChange<MainScreen>();

            _playMan.Shutdown();
            _entityManager.Shutdown();
            _mapManager.Shutdown();
            Reset();
        }

        private void HandleServerInfo(MsgServerInfo msg)
        {
            if (GameInfo == null)
                GameInfo = new ServerInfo();

            var info = GameInfo;

            info.ServerName = msg.ServerName;
            info.ServerMaxPlayers = msg.ServerMaxPlayers;
            info.SessionId = msg.PlayerSessionId;

            // start up player management
            _playMan.Startup(_net.ServerChannel);

            _playMan.LocalPlayer.SessionId = info.SessionId;

            _playMan.LocalPlayer.StatusChanged += OnLocalStatusChanged;
        }

        private void OnLocalStatusChanged(object obj, StatusEventArgs eventArgs)
        {
            // player finished fully connecting to the server.
            if (eventArgs.OldStatus == SessionStatus.Connecting)
            {
                OnPlayerJoinedServer(_playMan.LocalPlayer.Session);
            }

            if (eventArgs.NewStatus == SessionStatus.InGame)
            {
                _stateManager.RequestStateChange<GameScreen>();

                OnPlayerJoinedGame(_playMan.LocalPlayer.Session);
            }
        }

        private void OnRunLevelChanged(ClientRunLevel newRunLevel)
        {
            Logger.Debug($"[ENG] Runlevel changed to: {newRunLevel}");
            var args = new RunLevelChangedEventArgs(RunLevel, newRunLevel);
            RunLevel = newRunLevel;
            RunLevelChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    ///     Enumeration of the run levels of the BaseClient.
    /// </summary>
    public enum ClientRunLevel
    {
        Error = 0,
        Initialize,
        Connecting,
        Connected,
        InGame,
    }

    /// <summary>
    ///     Event arguments for when something changed with the player.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        /// <summary>
        ///     The session that triggered the event.
        /// </summary>
        private PlayerSession Session { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public PlayerEventArgs(PlayerSession session)
        {
            Session = session;
        }
    }

    /// <summary>
    ///     Event arguments for when the RunLevel has changed in the BaseClient.
    /// </summary>
    public class RunLevelChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     RunLevel that the BaseClient switched from.
        /// </summary>
        public ClientRunLevel OldLevel { get; }

        /// <summary>
        ///     RunLevel that the BaseClient switched to.
        /// </summary>
        public ClientRunLevel NewLevel { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public RunLevelChangedEventArgs(ClientRunLevel oldLevel, ClientRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    /// <summary>
    ///     Info about the server and player that is sent to the client while connecting.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        ///     Current name of the server.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     Max number of players that are allowed in the server at one time.
        /// </summary>
        public int ServerMaxPlayers { get; set; }

        public NetSessionId SessionId { get; set; }
    }
}
