﻿using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    public readonly struct RayCastResults
    {
        /// <summary>
        ///     True if an object was indeed hit. False otherwise.
        /// </summary>
        public bool DidHitObject => HitEntity != null;

        /// <summary>
        ///     The entity that was hit. <see langword="null" /> if no entity was hit.
        /// </summary>
        public IEntity HitEntity { get; }

        /// <summary>
        ///     The point of contact where the entity was hit. Defaults to <see cref="Vector2.Zero"/> if no entity was hit.
        /// </summary>
        public Vector2 HitPos { get; }

        /// <summary>
        ///     The distance from point of origin to the context point. 0.0f if nothing was hit.
        /// </summary>
        public float Distance { get; }

        public RayCastResults(float distance, Vector2 hitPos, IEntity hitEntity)
        {
            Distance = distance;
            HitPos = hitPos;
            HitEntity = hitEntity;
        }
    }
}
