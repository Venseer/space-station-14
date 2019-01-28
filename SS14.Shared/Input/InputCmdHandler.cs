﻿using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Players;

namespace SS14.Shared.Input
{
    public delegate void StateInputCmdDelegate(ICommonSession session);

    public abstract class InputCmdHandler
    {
        public virtual void Enabled(ICommonSession session)
        {
        }

        public virtual void Disabled(ICommonSession session)
        {
        }

        public abstract bool HandleCmdMessage(ICommonSession session, InputCmdMessage message);

        /// <summary>
        ///     Makes a quick input command from enabled and disabled delegates.
        /// </summary>
        /// <param name="enabled">The delegate to be ran when this command is enabled.</param>
        /// <param name="disabled">The delegate to be ran when this command is disabled.</param>
        /// <returns>The new input command.</returns>
        public static InputCmdHandler FromDelegate(StateInputCmdDelegate enabled = null,
            StateInputCmdDelegate disabled = null)
        {
            return new StateInputCmdHandler
            {
                EnabledDelegate = enabled,
                DisabledDelegate = disabled,
            };
        }

        private class StateInputCmdHandler : InputCmdHandler
        {
            public StateInputCmdDelegate EnabledDelegate;
            public StateInputCmdDelegate DisabledDelegate;

            public override void Enabled(ICommonSession session)
            {
                EnabledDelegate?.Invoke(session);
            }

            public override void Disabled(ICommonSession session)
            {
                DisabledDelegate?.Invoke(session);
            }

            public override bool HandleCmdMessage(ICommonSession session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage msg))
                    return false;

                switch (msg.State)
                {
                    case BoundKeyState.Up:
                        Disabled(session);
                        return true;
                    case BoundKeyState.Down:
                        Enabled(session);
                        return true;
                }

                //Client Sanitization: unknown key state, just ignore
                return false;
            }
        }
    }

    public delegate void PointerInputCmdDelegate(ICommonSession session, GridCoordinates coords, EntityUid uid);

    public delegate void PointerInputCmdDelegate2(in PointerInputCmdHandler.PointerInputCmdArgs args);

    public class PointerInputCmdHandler : InputCmdHandler
    {
        private PointerInputCmdDelegate2 _callback;

        public PointerInputCmdHandler(PointerInputCmdDelegate callback) : this((in PointerInputCmdArgs args) =>
            callback(args.Session, args.Coordinates, args.EntityUid))
        {
        }

        public PointerInputCmdHandler(PointerInputCmdDelegate2 callback)
        {
            _callback = callback;
        }

        public override bool HandleCmdMessage(ICommonSession session, InputCmdMessage message)
        {
            if (!(message is FullInputCmdMessage msg) || msg.State != BoundKeyState.Down)
                return false;

            _callback?.Invoke(new PointerInputCmdArgs(session, msg.Coordinates, msg.ScreenCoordinates, msg.Uid));

            return true;
        }

        public readonly struct PointerInputCmdArgs
        {
            public readonly ICommonSession Session;
            public readonly GridCoordinates Coordinates;
            public readonly ScreenCoordinates ScreenCoordinates;
            public readonly EntityUid EntityUid;

            public PointerInputCmdArgs(ICommonSession session, GridCoordinates coordinates,
                ScreenCoordinates screenCoordinates, EntityUid entityUid)
            {
                Session = session;
                Coordinates = coordinates;
                ScreenCoordinates = screenCoordinates;
                EntityUid = entityUid;
            }
        }
    }

    public class PointerStateInputCmdHandler : InputCmdHandler
    {
        private PointerInputCmdDelegate _enabled;
        private PointerInputCmdDelegate _disabled;

        public PointerStateInputCmdHandler(PointerInputCmdDelegate enabled, PointerInputCmdDelegate disabled)
        {
            _enabled = enabled;
            _disabled = disabled;
        }

        /// <inheritdoc />
        public override bool HandleCmdMessage(ICommonSession session, InputCmdMessage message)
        {
            if (!(message is FullInputCmdMessage msg))
                return false;

            switch (msg.State)
            {
                case BoundKeyState.Up:
                    _disabled?.Invoke(session, msg.Coordinates, msg.Uid);
                    return true;
                case BoundKeyState.Down:
                    _enabled?.Invoke(session, msg.Coordinates, msg.Uid);
                    return true;
            }

            //Client Sanitization: unknown key state, just ignore
            return false;
        }
    }
}
