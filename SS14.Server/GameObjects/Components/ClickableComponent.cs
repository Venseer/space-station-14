﻿using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case ClientEntityClickMsg msg:
                    var type = msg.Click;
                    var uid = msg.Uid;

                    Owner.RaiseEvent(new ClickedOnEntityMessage { Clicked = Owner.Uid, Owner = uid, MouseButton = type });
                    break;
            }
        }
    }
}
