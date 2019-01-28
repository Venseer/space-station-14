﻿using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgServerInfoReq : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgServerInfoReq);
        public MsgServerInfoReq(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            // Nothing
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            // Nothing
        }
    }
}
