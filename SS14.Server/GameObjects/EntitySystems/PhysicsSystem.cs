﻿using SS14.Server.Interfaces.Timing;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        private IPauseManager _pauseManager;
        private const float Epsilon = 1.0e-6f;
        private const float GlobalFriction = 0.01f;

        public PhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(PhysicsComponent));
        }

        public override void Initialize()
        {
            base.Initialize();
           
            _pauseManager = IoCManager.Resolve<IPauseManager>();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                DoMovement(entity, frameTime);
            }
        }

        private static void DoMovement(IEntity entity, float frameTime)
        {
            var transform = entity.Transform;
            var velocity = entity.GetComponent<PhysicsComponent>();

            if (velocity.AngularVelocity == 0 && velocity.LinearVelocity == Vector2.Zero)
            {
                return;
            }

            //rotate entity
            float angImpulse = 0;
            if (velocity.AngularVelocity > Epsilon)
                angImpulse = velocity.AngularVelocity * frameTime;

            transform.LocalRotation += angImpulse;

            //"space friction"
            if (velocity.LinearVelocity.LengthSquared > Epsilon)
                velocity.LinearVelocity -= velocity.LinearVelocity * (frameTime * GlobalFriction);
            else
                velocity.LinearVelocity = Vector2.Zero;

            var movement = velocity.LinearVelocity * frameTime;

            //Check for collision
            if (movement.LengthSquared > Epsilon && entity.TryGetComponent(out CollidableComponent collider))
            {
                var collided = collider.TryCollision(movement, true);

                if (collided)
                {
                    if (velocity.EdgeSlide)
                    {
                        //Slide along the blockage in the non-blocked direction
                        var xBlocked = collider.TryCollision(new Vector2(movement.X, 0));
                        var yBlocked = collider.TryCollision(new Vector2(0, movement.Y));

                        var v = velocity.LinearVelocity;
                        velocity.LinearVelocity = new Vector2(xBlocked ? 0 : v.X, yBlocked ? 0 : v.Y);
                    }
                    else
                    {
                        //Stop movement entirely at first blockage
                        velocity.LinearVelocity = new Vector2(0, 0);
                    }

                    movement = velocity.LinearVelocity * frameTime;
                }
            }

            //Apply velocity
            transform.WorldPosition += movement;
        }
    }
}
