﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    [TestOf(typeof(ComponentManager))]
    class ComponentManager_Test
    {
        private const uint CompNetId = 3;

#region Component Management

        [Test]
        public void AddComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;

            // Act
            manager.AddComponent(entity, component);

            // Assert
            var result = manager.GetComponent<DummyComponent>(entity.Uid);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void AddComponentOverwriteTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, new DummyComponent
            {
                Owner = entity
            });

            // Act
            manager.AddComponent(entity, component, true);

            // Assert
            var result = manager.GetComponent<DummyComponent>(entity.Uid);
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void HasComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.HasComponent<DummyComponent>(entity.Uid);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.HasComponent(entity.Uid, CompNetId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponent(entity.Uid, CompNetId);

            // Assert
            Assert.That(result, Is.EqualTo(component));
        }

        [Test]
        public void TryGetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.TryGetComponent<DummyComponent>(entity.Uid, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void TryGetNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.TryGetComponent(entity.Uid, CompNetId, out var comp);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(comp, Is.EqualTo(component));
        }

        [Test]
        public void RemoveComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            manager.RemoveComponent<DummyComponent>(entity.Uid);
            manager.CullRemovedComponents();

            // Assert
            Assert.That(manager.HasComponent(entity.Uid, component.GetType()), Is.False);
        }

        [Test]
        public void RemoveNetComponentTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            manager.RemoveComponent(entity.Uid, CompNetId);
            manager.CullRemovedComponents();

            // Assert
            Assert.That(manager.HasComponent(entity.Uid, component.GetType()), Is.False);
        }

        [Test]
        public void GetComponentsTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponents<DummyComponent>(entity.Uid);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentsTest()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetAllComponents<DummyComponent>();

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        [Test]
        public void GetAllComponentInstances()
        {
            // Arrange
            var manager = ManagerFactory(out var entityManager);
            var entity = EntityFactory(entityManager);
            var component = new DummyComponent();
            component.Owner = entity;
            manager.AddComponent(entity, component);

            // Act
            var result = manager.GetComponentInstances(entity.Uid);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(component));
        }

        #endregion

        // mimics the IoC system.
        private static IComponentManager ManagerFactory(out IEntityManager entityManager)
        {
            var manager = new ComponentManager();

            // set up the registration
            var mockRegistration = new Mock<IComponentRegistration>();
            mockRegistration.SetupGet(x => x.References).Returns(new List<Type> { typeof(DummyComponent) });

            // setup the comp factory
            var mockFactory = new Mock<IComponentFactory>();
            mockFactory.Setup(x => x.GetRegistration(It.IsAny<IComponent>())).Returns(mockRegistration.Object);
            mockFactory.Setup(x => x.GetRegistration(It.IsAny<Type>())).Returns(mockRegistration.Object);
            mockFactory.Setup(x => x.GetComponent<DummyComponent>()).Returns(new DummyComponent());

            // set up the entity manager
            var mockEntMan = new Mock<IEntityManager>();

            // Inject the dependency into manager
            foreach (var field in GetDepFields(typeof(ComponentManager)))
            {
                if (field.FieldType.IsAssignableFrom(typeof(IComponentFactory)))
                {
                    field.SetValue(manager, mockFactory.Object);
                }
                else if (field.FieldType.IsAssignableFrom(typeof(IEntityManager)))
                {
                    field.SetValue(manager, mockEntMan.Object);
                }
            }

            entityManager = mockEntMan.Object;
            return manager;
        }

        private static IEnumerable<FieldInfo> GetDepFields(Type targetType)
        {
            return targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => Attribute.GetCustomAttribute(p, typeof(DependencyAttribute)) != null);
        }

        private class DummyComponent : Component, ICompType1, ICompType2
        {
            public override string Name => null;
            public override uint? NetID => CompNetId;
        }

        private static IEntity EntityFactory(IEntityManager entityManager)
        {
            var mockEnt = new Mock<IEntity>();
            mockEnt.SetupGet(x => x.EntityManager).Returns(entityManager);
            mockEnt.SetupGet(x => x.Uid).Returns(new EntityUid(7));
            mockEnt.Setup(x => x.IsValid()).Returns(true);
            return mockEnt.Object;
        }

        private interface ICompType1 { }

        private interface ICompType2 { }
    }
}
