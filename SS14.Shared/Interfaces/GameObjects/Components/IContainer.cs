﻿using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.GameObjects
{
    /// <summary>
    /// A container is a way to "contain" entities inside other entities, in a logical way.
    /// This is alike BYOND's <c>contents</c> system, except more advanced.
    /// </summary>
    /// <remarks>
    /// <p>
    /// Containers are logical separations of entities contained inside another entity.
    /// for example, a crate with two separated compartments would have two separate containers.
    /// If an entity inside compartment A drops something,
    /// the dropped entity would be placed in compartment A too,
    /// and compartment B would be completely untouched.
    /// </p>
    /// <p>
    /// Containers are managed by an entity's <see cref="IContainerManager" />,
    /// and have an ID to be referenced by.
    /// </p>
    /// </remarks>
    /// <seealso cref="IContainerManager" />
    public interface IContainer
    {
        /// <summary>
        /// The container manager owning this container.
        /// </summary>
        IContainerManager Manager { get; }

        /// <summary>
        /// The ID of this container.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The entity owning this container.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        /// True if the container has been shut down via <see cref="Shutdown" />
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        /// Readonly collection of all the entities contained within this specific container
        /// </summary>
        IReadOnlyCollection<IEntity> ContainedEntities { get; }

        /// <summary>
        /// Checks if the entity can be inserted into this container.
        /// </summary>
        /// <param name="toinsert">The entity to attempt to insert.</param>
        /// <returns>True if the entity can be inserted, false otherwise.</returns>
        bool CanInsert(IEntity toinsert);

        /// <summary>
        /// Attempts to insert the entity into this container.
        /// </summary>
        /// <param name="toinsert">The entity to insert.</param>
        /// <returns>False if the entity could not be inserted.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this container is a child of the entity,
        /// which would cause infinite loops.
        /// </exception>
        bool Insert(IEntity toinsert);

        /// <summary>
        /// Checks if the entity can be removed from this container.
        /// </summary>
        /// <param name="toremove">The entity to check.</param>
        /// <returns>True if the entity can be removed, false otherwise.</returns>
        bool CanRemove(IEntity toremove);

        /// <summary>
        /// Attempts to remove the entity from this container.
        /// </summary>
        /// <param name="toremove">The entity to attempt to remove.</param>
        /// <returns>True if the entity was removed, false otherwise.</returns>
        bool Remove(IEntity toremove);

        /// <summary>
        /// Checks if the entity is contained in this container.
        /// This is not recursive, so containers of children are not checked.
        /// </summary>
        /// <param name="contained">The entity to check.</param>
        /// <returns>True if the entity is immediately contained in this container, false otherwise.</returns>
        bool Contains(IEntity contained);

        /// <summary>
        /// Clears the container and marks it as deleted.
        /// </summary>
        void Shutdown();
    }
}
