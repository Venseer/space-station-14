﻿using System;
using SS14.Client.GameObjects;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;

namespace SS14.Client.Player
{
    /// <summary>
    ///     Variables and functions that deal with the local client's session.
    /// </summary>
    public class LocalPlayer
    {
        private readonly IConfigurationManager _configManager;
        private readonly IClientNetManager _networkManager;

        /// <summary>
        ///     An entity has been attached to the local player.
        /// </summary>
        public event EventHandler EntityAttached;

        /// <summary>
        ///     An entity has been detached from the local player.
        /// </summary>
        public event EventHandler EntityDetached;

        /// <summary>
        ///     Game entity that the local player is controlling. If this is null, the player
        ///     is in free/spectator cam.
        /// </summary>
        public IEntity ControlledEntity { get; private set; }


        public NetSessionId SessionId { get; set; }

        /// <summary>
        ///     Session of the local client.
        /// </summary>
        public PlayerSession Session { get; set; }

        /// <summary>
        ///     OOC name of the local player.
        /// </summary>
        public string Name => SessionId.Username;

        /// <summary>
        ///     The client's entity has moved. This only is raised when the player is attached to an entity.
        /// </summary>
        public event EventHandler<MoveEventArgs> EntityMoved;

        /// <summary>
        ///     The status of the client's session has changed.
        /// </summary>
        public event EventHandler<StatusEventArgs> StatusChanged;

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="netMan"></param>
        /// <param name="configMan"></param>
        public LocalPlayer(IClientNetManager netMan, IConfigurationManager configMan)
        {
            _networkManager = netMan;
            _configManager = configMan;
        }

        /// <summary>
        ///     Attaches a client to an entity.
        /// </summary>
        /// <param name="entity">Entity to attach the client to.</param>
        public void AttachEntity(IEntity entity)
        {
            // Detach and cleanup first
            DetachEntity();

            ControlledEntity = entity;

            if (ControlledEntity.HasComponent<IMoverComponent>())
                ControlledEntity.RemoveComponent<IMoverComponent>();

            ControlledEntity.AddComponent<PlayerInputMoverComponent>();

            if (!ControlledEntity.TryGetComponent<EyeComponent>(out var eye))
            {
                eye = ControlledEntity.AddComponent<EyeComponent>();
            }
            eye.Current = true;

            var transform = ControlledEntity.Transform;
            transform.OnMove += OnPlayerMoved;

            EntityAttached?.Invoke(this, EventArgs.Empty);
            entity.SendMessage(null, new PlayerAttachedMsg());

            // notify ECS Systems
            ControlledEntity.EntityManager.RaiseEvent(this, new PlayerAttachSysMessage(ControlledEntity));
        }

        /// <summary>
        ///     Detaches the client from an entity.
        /// </summary>
        public void DetachEntity()
        {
            if (ControlledEntity != null && ControlledEntity.Initialized)
            {
                ControlledEntity.RemoveComponent<PlayerInputMoverComponent>();
                ControlledEntity.GetComponent<EyeComponent>().Current = false;
                var transform = ControlledEntity.Transform;
                if (transform != null)
                    transform.OnMove -= OnPlayerMoved;
                ControlledEntity.SendMessage(null, new PlayerDetachedMsg());

                // notify ECS Systems
                ControlledEntity.EntityManager.RaiseEvent(this, new PlayerAttachSysMessage(null));
            }
            ControlledEntity = null;

            EntityDetached?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayerMoved(object sender, MoveEventArgs args)
        {
            EntityMoved?.Invoke(sender, args);
        }

        /// <summary>
        ///     Changes the state of the session.
        /// </summary>
        public void SwitchState(SessionStatus newStatus)
        {
            var args = new StatusEventArgs(Session.Status, newStatus);
            Session.Status = newStatus;
            StatusChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    ///     Event arguments for when the status of a session changes.
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        /// <summary>
        ///     Status that the session switched from.
        /// </summary>
        public SessionStatus OldStatus { get; }

        /// <summary>
        ///     Status that the session switched to.
        /// </summary>
        public SessionStatus NewStatus { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public StatusEventArgs(SessionStatus oldStatus, SessionStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
