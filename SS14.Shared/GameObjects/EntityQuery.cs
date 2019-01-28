﻿using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    /// An entity query that checks based on the components of an entity.
    /// </summary>
    /// <remarks>
    /// The query can be controlled with <see cref="AllSet" />, <see cref="ExclusionSet" /> and <see cref="OneSet" />.
    /// If all of these are empty, the query is effectively equal to <see cref="AllEntityQuery" />.
    /// </remarks>
    public class ComponentEntityQuery : IEntityQuery
    {
        /// <summary>
        /// A list of component reference types, none of which an entity can have to pas.
        /// If this is empty, it's ignored.
        /// </summary>
        public IList<Type> AllSet { get; set; } = new List<Type>();
        /// <summary>
        /// A list of component reference types, all of which an entity must not have to pass.
        /// If this is empty, it's ignored.
        /// </summary>
        public IList<Type> ExclusionSet { get; set; } = new List<Type>();
        /// <summary>
        /// A list of component reference types, at least one of which the entity must have to pass.
        /// If this is empty, it's ignored.
        /// </summary>
        public IList<Type> OneSet { get; set; } = new List<Type>();

        public bool Match(IEntity entity)
        {
            Func<Type, bool> hasComponent = (t => entity.HasComponent(t));
            if (ExclusionSet.Any(hasComponent))
            {
                return false;
            }

            if (!AllSet.All(hasComponent))
            {
                return false;
            }

            if (OneSet.Count != 0 && !OneSet.Any(hasComponent))
            {
                return false;
            }

            // Nobody complained so we're good here!
            return true;
        }
    }

    /// <summary>
    /// An entity query that will let all entities pass.
    /// </summary>
    public class AllEntityQuery : IEntityQuery
    {
        public bool Match(IEntity entity) => true;
    }

    /// <summary>
    /// An entity query which will match entities based on a predicate.
    /// </summary>
    public class PredicateEntityQuery : IEntityQuery
    {
        public readonly Func<IEntity, bool> Predicate;
        public PredicateEntityQuery(Func<IEntity, bool> predicate)
        {
            Predicate = predicate;
        }

        public bool Match(IEntity entity) => Predicate(entity);
    }

    /// <summary>
    ///     An entity query that will match all of one type of component.
    /// </summary>
    public class TypeEntityQuery : IEntityQuery
    {
        public Type ComponentType { get; }

        public TypeEntityQuery(Type componentType)
        {
            ComponentType = componentType;
        }

        public bool Match(IEntity entity) => entity.HasComponent(ComponentType);
    }
}
