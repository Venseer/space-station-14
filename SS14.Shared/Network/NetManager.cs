﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;

namespace SS14.Shared.Network
{
    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage(NetMessage message);

    /// <summary>
    ///     Callback for registered NetMessages.
    /// </summary>
    /// <param name="message">The message received.</param>
    public delegate void ProcessMessage<in T>(T message) where T : NetMessage;

    /// <summary>
    ///     Manages all network connections and packet IO.
    /// </summary>
    public class NetManager : IClientNetManager, IServerNetManager, IDisposable
    {
        private readonly Dictionary<Type, ProcessMessage> _callbacks = new Dictionary<Type, ProcessMessage>();

        /// <summary>
        ///     Holds the synced lookup table of NetConnection -> NetChannel
        /// </summary>
        private readonly Dictionary<NetConnection, NetChannel> _channels = new Dictionary<NetConnection, NetChannel>();

        private readonly Dictionary<NetConnection, NetSessionId> _assignedSessions =
            new Dictionary<NetConnection, NetSessionId>();

        [Dependency] private readonly IConfigurationManager _config;

        /// <summary>
        ///     Holds lookup table for NetMessage.Id -> NetMessage.Type
        /// </summary>
        private readonly Dictionary<string, Type> _messages = new Dictionary<string, Type>();

        /// <summary>
        /// The StringTable for transforming packet Ids to Packet name.
        /// </summary>
        private readonly StringTable _strings = new StringTable();

        /// <summary>
        ///     The list of network peers we are listening on.
        /// </summary>
        private readonly List<NetPeer> _netPeers = new List<NetPeer>();

        /// <inheritdoc />
        public int Port => _config.GetCVar<int>("net.port");

        /// <inheritdoc />
        public bool IsServer { get; private set; }

        /// <inheritdoc />
        public bool IsClient => !IsServer;

        /// <inheritdoc />
        public bool IsConnected => _netPeers.Any(p => p.ConnectionsCount > 0);

        public bool IsRunning => _netPeers.Count != 0;

        public NetworkStats Statistics
        {
            get
            {
                var sentPackets = 0;
                var sentBytes = 0;
                var recvPackets = 0;
                var recvBytes = 0;

                foreach (var peer in _netPeers)
                {
                    var netPeerStatistics = peer.Statistics;
                    sentPackets += netPeerStatistics.SentPackets;
                    sentBytes += netPeerStatistics.SentBytes;
                    recvPackets += netPeerStatistics.ReceivedPackets;
                    recvBytes += netPeerStatistics.ReceivedBytes;
                }

                return new NetworkStats(sentBytes, recvBytes, sentPackets, recvPackets);
            }
        }

        /// <inheritdoc />
        public IEnumerable<INetChannel> Channels => _channels.Values;

        /// <inheritdoc />
        public int ChannelCount => _channels.Count;

        public IReadOnlyDictionary<Type, ProcessMessage> CallbackAudit => _callbacks;

        /// <inheritdoc />
        public INetChannel ServerChannel
        {
            get
            {
                DebugTools.Assert(IsClient);

                if (_netPeers.Count == 0)
                {
                    return null;
                }

                var peer = _netPeers[0];
                if (peer.ConnectionsCount == 0)
                {
                    return null;
                }

                return GetChannel(peer.Connections[0]);
            }
        }

        private bool _initialized;

        /// <inheritdoc />
        public void Initialize(bool isServer)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("NetManager has already been initialized.");
            }

            IsServer = isServer;

            _config.RegisterCVar("net.port", 1212, CVar.ARCHIVE);

            if (!isServer)
            {
                _config.RegisterCVar("net.server", "127.0.0.1", CVar.ARCHIVE);
                _config.RegisterCVar("net.updaterate", 20, CVar.ARCHIVE);
                _config.RegisterCVar("net.cmdrate", 30, CVar.ARCHIVE);
                _config.RegisterCVar("net.interpolation", 0.1f, CVar.ARCHIVE);
                _config.RegisterCVar("net.rate", 10240, CVar.REPLICATED | CVar.ARCHIVE);
            }
            else
            {
                // That's comma-separated, btw.
                _config.RegisterCVar("net.bindto", "0.0.0.0,::", CVar.ARCHIVE);
            }

#if DEBUG
            _config.RegisterCVar("net.fakelag", false, CVar.CHEAT);
            _config.RegisterCVar("net.fakeloss", 0.0f, CVar.CHEAT);
            _config.RegisterCVar("net.fakelagmin", 0.0f, CVar.CHEAT);
            _config.RegisterCVar("net.fakelagrand", 0.0f, CVar.CHEAT);
#endif

            _strings.Initialize(this, () => { OnConnected(ServerChannel); });

            _initialized = true;
        }

        public void StartServer()
        {
            DebugTools.Assert(IsServer);
            DebugTools.Assert(!IsRunning);

            var binds = _config.GetCVar<string>("net.bindto").Split(',');

            foreach (var bindAddress in binds)
            {
                if (!IPAddress.TryParse(bindAddress.Trim(), out var address))
                {
                    throw new InvalidOperationException("Not a valid IPv4 or IPv6 address");
                }

                var config = _getBaseNetPeerConfig();
                config.LocalAddress = address;
                config.Port = Port;
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

                var peer = new NetPeer(config);
                peer.Start();
                _netPeers.Add(peer);
            }

            if (_netPeers.Count == 0)
            {
                Logger.WarningS("net",
                    "Exactly 0 addresses have been bound to, nothing will be able to connect to the server.");
            }
        }

        public void Dispose()
        {
            Shutdown("Network manager getting disposed.");
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            foreach (var kvChannel in _channels)
                DisconnectChannel(kvChannel.Value, reason);

            // request shutdown of the netPeer
            _netPeers.ForEach(p => p.Shutdown(reason));
            _netPeers.Clear();

            // wait for the network thread to finish its work (like flushing packets and gracefully disconnecting)
            // Lidgren does not expose the thread, so we can't join or or anything
            // pretty much have to poll every so often and wait for it to finish before continuing
            // when the network thread is finished, it will change status from ShutdownRequested to NotRunning
            while (_netPeers.Any(p => p.Status == NetPeerStatus.ShutdownRequested))
            {
                // sleep the thread for an arbitrary length so it isn't spinning in the while loop as much
                Thread.Sleep(50);
            }

            _strings.Reset();
        }

        public void ProcessPackets()
        {
            foreach (var peer in _netPeers)
            {
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                            Logger.DebugS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            Logger.InfoS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.WarningMessage:
                            Logger.WarningS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.ErrorMessage:
                            Logger.ErrorS("net", "{0}: {1}", peer.Configuration.LocalAddress, msg.ReadString());
                            break;

                        case NetIncomingMessageType.ConnectionApproval:
                            HandleApproval(msg);
                            break;

                        case NetIncomingMessageType.Data:
                            DispatchNetMessage(msg);
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            HandleStatusChanged(msg);
                            break;

                        default:
                            Logger.WarningS("net",
                                "{0}: Unhandled incoming packet type from {1}: {2}",
                                peer.Configuration.LocalAddress,
                                msg.SenderConnection.RemoteEndPoint,
                                msg.MessageType);
                            break;
                    }

                    peer.Recycle(msg);
                }
            }
        }

        /// <inheritdoc />
        public void ClientConnect(string host, int port, string userNameRequest)
        {
            DebugTools.Assert(!IsServer, "Should never be called on the server.");
            DebugTools.Assert(!IsConnected);

            if (IsRunning)
            {
                ClientDisconnect("Client left server.");
            }

            // Set up NetPeer.
            var endPoint = NetUtility.Resolve(host, port);

            Logger.InfoS("net", $"Connecting to {endPoint}...");

            var config = _getBaseNetPeerConfig();
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                config.LocalAddress = IPAddress.IPv6Any;
            }
            else
            {
                config.LocalAddress = IPAddress.Any;
            }

            var peer = new NetPeer(config);
            peer.Start();
            _netPeers.Add(peer);
            var hail = peer.CreateMessage();
            hail.Write(userNameRequest);
            peer.Connect(host, port, hail);
        }

        /// <inheritdoc />
        public void ClientDisconnect(string reason)
        {
            DebugTools.Assert(IsClient, "Should never be called on the server.");
            Disconnect?.Invoke(this, new NetChannelArgs(ServerChannel));
            Shutdown(reason);
        }

        private NetPeerConfiguration _getBaseNetPeerConfig()
        {
            var netConfig = new NetPeerConfiguration("SS14_NetTag");

#if DEBUG
            //Simulate Latency
            if (_config.GetCVar<bool>("net.fakelag"))
            {
                netConfig.SimulatedLoss = _config.GetCVar<float>("net.fakeloss");
                netConfig.SimulatedMinimumLatency = _config.GetCVar<float>("net.fakelagmin");
                netConfig.SimulatedRandomLatency = _config.GetCVar<float>("net.fakelagrand");
            }

            netConfig.ConnectionTimeout = 30000f;
#endif
            return netConfig;
        }

        /// <summary>
        ///     Gets the NetChannel of a peer NetConnection.
        /// </summary>
        /// <param name="connection">The raw connection of the peer.</param>
        /// <returns>The NetChannel of the peer.</returns>
        private INetChannel GetChannel(NetConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (_channels.TryGetValue(connection, out NetChannel channel))
                return channel;

            throw new NetManagerException("There is no NetChannel for this NetConnection.");
        }

        private void HandleStatusChanged(NetIncomingMessage msg)
        {
            var sender = msg.SenderConnection;
            Logger.DebugS("net", $"{sender.RemoteEndPoint}: Status changed to {sender.Status}");

            switch (sender.Status)
            {
                case NetConnectionStatus.Connected:
                    HandleConnected(sender);
                    break;

                case NetConnectionStatus.Disconnected:
                    if (_channels.ContainsKey(sender))
                        HandleDisconnect(msg);
                    else if (sender.RemoteUniqueIdentifier == 0
                    ) // is this the best way to detect an unsuccessful connect?
                    {
                        Logger.InfoS("net", $"{sender.RemoteEndPoint}: Failed to connect");
                        OnConnectFailed();
                    }

                    break;
            }
        }

        private void HandleApproval(NetIncomingMessage message)
        {
            var sender = message.SenderConnection;
            var ip = sender.RemoteEndPoint;
            var name = message.ReadString();
            var origName = name;
            var iterations = 1;

            if (!UsernameHelpers.IsNameValid(name))
            {
                sender.Deny("Username is invalid (contains illegal characters/too long).");
            }

            while (_assignedSessions.Values.Any(u => u.Username == name))
            {
                // This is shit but I don't care.
                name = $"{origName}_{++iterations}";
            }

            var session = new NetSessionId(name);

            if (OnConnecting(ip, session))
            {
                _assignedSessions.Add(sender, session);
                var msg = message.SenderConnection.Peer.CreateMessage();
                msg.Write(name);
                sender.Approve(msg);
            }
            else
            {
                sender.Deny("Server is full.");
            }
        }

        private void HandleConnected(NetConnection sender)
        {
            NetSessionId session;
            if (IsClient)
            {
                session = new NetSessionId(sender.RemoteHailMessage.ReadString());
            }
            else
            {
                session = _assignedSessions[sender];
            }

            var channel = new NetChannel(this, sender, session);
            _channels.Add(sender, channel);

            _strings.SendFullTable(channel);

            Logger.InfoS("net", $"{channel.RemoteEndPoint}: Connected");

            // client is connected after string packet get received
            if (IsServer)
                OnConnected(channel);
        }

        private void HandleDisconnect(NetIncomingMessage message)
        {
            string reason;
            try
            {
                message.ReadByte(); // status
                reason = message.ReadString();
            }
            catch (NetException)
            {
                reason = String.Empty;
            }

            var conn = message.SenderConnection;
            var channel = _channels[conn];

            Logger.InfoS("net", $"{channel.RemoteEndPoint}: Disconnected ({reason})");
            _assignedSessions.Remove(conn);

            OnDisconnected(channel);
            _channels.Remove(conn);

            if (IsClient)
                _strings.Reset();
        }

        /// <inheritdoc />
        public void DisconnectChannel(INetChannel channel, string reason)
        {
            channel.Disconnect(reason);
        }

        private void DispatchNetMessage(NetIncomingMessage msg)
        {
            var peer = msg.SenderConnection.Peer;
            if (peer.Status == NetPeerStatus.ShutdownRequested)
                return;

            if (peer.Status == NetPeerStatus.NotRunning)
                return;

            if (!IsConnected)
                return;

            if (msg.LengthBytes < 1)
            {
                Logger.WarningS("net", $"{msg.SenderConnection.RemoteEndPoint}: Received empty packet.");
                return;
            }

            var id = msg.ReadByte();

            if (!_strings.TryGetString(id, out string name))
            {
                Logger.WarningS("net", $"{msg.SenderConnection.RemoteEndPoint}:  No string in table with ID {id}.");
                return;
            }

            if (!_messages.TryGetValue(name, out Type packetType))
            {
                Logger.WarningS("net", $"{msg.SenderConnection.RemoteEndPoint}: No message with Name {name}.");
                return;
            }

            var channel = GetChannel(msg.SenderConnection);
            var instance = (NetMessage) Activator.CreateInstance(packetType, channel);
            instance.MsgChannel = channel;

            try
            {
                instance.ReadFromBuffer(msg);
            }
            catch (Exception e) // yes, we want to catch ALL exeptions for security
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Failed to deserialize {packetType.Name} packet: {e.Message}");
            }

            if (!_callbacks.TryGetValue(packetType, out ProcessMessage callback))
            {
                Logger.WarningS("net",
                    $"{msg.SenderConnection.RemoteEndPoint}: Received packet {id}:{name}, but callback was not registered.");
                return;
            }

            callback?.Invoke(instance);
        }

        #region NetMessages

        /// <inheritdoc />
        public void RegisterNetMessage<T>(string name, ProcessMessage<T> rxCallback = null)
            where T : NetMessage
        {
            _strings.AddString(name);

            _messages.Add(name, typeof(T));

            if (rxCallback != null)
                _callbacks.Add(typeof(T), msg => rxCallback((T) msg));
        }

        /// <inheritdoc />
        public T CreateNetMessage<T>()
            where T : NetMessage
        {
            return (T) Activator.CreateInstance(typeof(T), (INetChannel) null);
        }

        private NetOutgoingMessage BuildMessage(NetMessage message, NetPeer peer)
        {
            var packet = peer.CreateMessage(4);

            if (!_strings.TryFindStringId(message.MsgName, out int msgId))
                throw new NetManagerException(
                    $"[NET] No string in table with name {message.MsgName}. Was it registered?");

            packet.Write((byte) msgId);
            message.WriteToBuffer(packet);
            return packet;
        }

        /// <inheritdoc />
        public void ServerSendToAll(NetMessage message)
        {
            DebugTools.Assert(IsServer);

            if (!IsConnected)
                return;

            foreach (var peer in _netPeers)
            {
                var packet = BuildMessage(message, peer);
                var method = GetMethod(message.MsgGroup);
                if (peer.ConnectionsCount == 0)
                {
                    continue;
                }
                peer.SendMessage(packet, peer.Connections, method, 0);
            }
        }

        /// <inheritdoc />
        public void ServerSendMessage(NetMessage message, INetChannel recipient)
        {
            DebugTools.Assert(IsServer);
            if (!(recipient is NetChannel channel))
                throw new ArgumentException($"Not of type {typeof(NetChannel).FullName}", nameof(recipient));

            var peer = channel.Connection.Peer;
            var packet = BuildMessage(message, peer);
            peer.SendMessage(packet, channel.Connection, NetDeliveryMethod.ReliableOrdered);
        }

        /// <inheritdoc />
        public void ServerSendToMany(NetMessage message, List<INetChannel> recipients)
        {
            DebugTools.Assert(IsServer);
            if (!IsConnected)
                return;

            foreach (var channel in recipients)
            {
                ServerSendMessage(message, channel);
            }
        }

        /// <inheritdoc />
        public void ClientSendMessage(NetMessage message)
        {
            DebugTools.Assert(IsClient);

            // not connected to a server, so a message cannot be sent to it.
            if (!IsConnected)
                return;

            DebugTools.Assert(_netPeers.Count == 1);
            DebugTools.Assert(_netPeers[0].ConnectionsCount == 1);

            var peer = _netPeers[0];
            var packet = BuildMessage(message, peer);
            var method = GetMethod(message.MsgGroup);
            peer.SendMessage(packet, peer.Connections[0], method);
        }

        #endregion NetMessages

        #region Events

        protected virtual bool OnConnecting(IPEndPoint ip, NetSessionId sessionId)
        {
            var args = new NetConnectingArgs(sessionId, ip);
            Connecting?.Invoke(this, args);
            return !args.Deny;
        }

        protected virtual void OnConnectFailed()
        {
            var args = new NetConnectFailArgs();
            ConnectFailed?.Invoke(this, args);
        }

        protected virtual void OnConnected(INetChannel channel)
        {
            Connected?.Invoke(this, new NetChannelArgs(channel));
        }

        protected virtual void OnDisconnected(INetChannel channel)
        {
            Disconnect?.Invoke(this, new NetChannelArgs(channel));
        }

        /// <inheritdoc />
        public event EventHandler<NetConnectingArgs> Connecting;

        /// <inheritdoc />
        public event EventHandler<NetConnectFailArgs> ConnectFailed;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Connected;

        /// <inheritdoc />
        public event EventHandler<NetChannelArgs> Disconnect;

        #endregion Events

        private static NetDeliveryMethod GetMethod(MsgGroups group)
        {
            switch (group)
            {
                case MsgGroups.Entity:
                    return NetDeliveryMethod.Unreliable;
                case MsgGroups.Core:
                case MsgGroups.String:
                case MsgGroups.Command:
                    return NetDeliveryMethod.ReliableUnordered;
                default:
                    throw new ArgumentOutOfRangeException(nameof(group), group, null);
            }
        }
    }

    /// <summary>
    ///     Generic exception thrown by the NetManager class.
    /// </summary>
    public class NetManagerException : Exception
    {
        public NetManagerException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    ///     Traffic statistics for a NetChannel.
    /// </summary>
    public struct NetworkStats
    {
        /// <summary>
        ///     Total sent bytes.
        /// </summary>
        public readonly int SentBytes;

        /// <summary>
        ///     Total received bytes.
        /// </summary>
        public readonly int ReceivedBytes;

        /// <summary>
        ///     Total sent packets.
        /// </summary>
        public readonly int SentPackets;

        /// <summary>
        ///     Total received packets.
        /// </summary>
        public readonly int ReceivedPackets;

        public NetworkStats(int sentBytes, int receivedBytes, int sentPackets, int receivedPackets)
        {
            SentBytes = sentBytes;
            ReceivedBytes = receivedBytes;
            SentPackets = sentPackets;
            ReceivedPackets = receivedPackets;
        }

        /// <summary>
        ///     Creates an instance of this object.
        /// </summary>
        public NetworkStats(NetPeerStatistics statistics)
        {
            SentBytes = statistics.SentBytes;
            ReceivedBytes = statistics.ReceivedBytes;
            SentPackets = statistics.SentPackets;
            ReceivedPackets = statistics.ReceivedPackets;
        }
    }
}
