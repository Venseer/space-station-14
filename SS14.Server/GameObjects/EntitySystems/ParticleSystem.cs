﻿using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class ParticleSystem : EntitySystem
    {
        public ParticleSystem()
        {
            EntityQuery = new ComponentEntityQuery()
            {
                OneSet = new List<Type>()
                {
                    typeof(ParticleSystemComponent),
                },
            };
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();
        }

        public override void Update(float frametime)
        {
            //TODO: Figure out what to do with this
        }
    }
}
