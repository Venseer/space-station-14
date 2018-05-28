﻿using System;
using SS14.Shared.Maths;
using SS14.Shared.Map;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a TransformComponent.
    /// </summary>
    [Serializable, NetSerializable]
    public class TransformComponentState : ComponentState
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public readonly EntityUid? ParentID;

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        public readonly Vector2 LocalPosition;

        public readonly GridId GridID;
        public readonly MapId MapID;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public readonly Angle Rotation;

        /// <summary>
        ///     Constructs a new state snapshot of a TransformComponent.
        /// </summary>
        /// <param name="position">Current position offset of the entity.</param>
        /// <param name="rotation">Current direction offset of the entity.</param>
        /// <param name="parentID">Current parent transform of this entity.</param>
        public TransformComponentState(Vector2 localPosition, GridId gridId, MapId mapId, Angle rotation, EntityUid? parentID)
            : base(NetIDs.TRANSFORM)
        {
            LocalPosition = localPosition;
            GridID = gridId;
            MapID = mapId;
            Rotation = rotation;
            ParentID = parentID;
        }
    }
}
