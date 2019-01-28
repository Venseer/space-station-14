﻿using System.IO;
using NUnit.Framework;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    [TestOf(typeof(TransformComponent))]
    class Transform_Test : SS14UnitTest
    {
        private IServerEntityManager EntityManager;
        private IMapManager MapManager;

        const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform
";

        private IMap MapA;
        private IMapGrid GridA;
        private IMap MapB;
        private IMapGrid GridB;

        [OneTimeSetUp]
        public void Setup()
        {
            EntityManager = IoCManager.Resolve<IServerEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            // build the net dream
            MapA = MapManager.CreateMap();
            GridA = MapA.CreateGrid();

            MapB = MapManager.CreateMap();
            GridB = MapB.CreateGrid();

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [Test]
        public void ParentMapSwitchTest()
        {
            // two entities
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");

            var parentTrans = parent.Transform;
            var childTrans = child.Transform;

            // that are not on the same map
            parentTrans.GridPosition = new GridCoordinates(5, 5, GridA);
            childTrans.GridPosition = new GridCoordinates(4, 4, GridB);

            // if they are parented, the child keeps its world position, but moves to the parents map
            childTrans.AttachParent(parentTrans);

            Assert.That(childTrans.MapID, Is.EqualTo(parentTrans.MapID));
            Assert.That(childTrans.GridID, Is.EqualTo(parentTrans.GridID));
            Assert.That(childTrans.GridPosition, Is.EqualTo(new GridCoordinates(4, 4, GridA)));
            Assert.That(childTrans.WorldPosition, Is.EqualTo(new Vector2(4, 4)));

            // move the parent, and the child should move with it
            childTrans.WorldPosition = new Vector2(6, 6);
            parentTrans.WorldPosition += new Vector2(-7, -7);

            Assert.That(childTrans.WorldPosition, Is.EqualTo(new Vector2(-1, -1)));

            // if we detach parent, the child should be left where it was, still relative to parents grid
            var oldLpos = childTrans.GridPosition;
            var oldWpos = childTrans.WorldPosition;

            childTrans.DetachParent();

            Assert.That(childTrans.GridPosition, Is.EqualTo(oldLpos));
            Assert.That(childTrans.WorldPosition, Is.EqualTo(oldWpos));
        }

        /// <summary>
        ///     Tests that a child entity does not move when attaching to a parent.
        /// </summary>
        [Test]
        public void ParentAttachMoveTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.Transform;
            var childTrans = child.Transform;
            parentTrans.WorldPosition = new Vector2(5, 5);
            childTrans.WorldPosition = new Vector2(6, 6);

            // Act
            var oldWpos = childTrans.WorldPosition;
            childTrans.AttachParent(parentTrans);
            var newWpos = childTrans.WorldPosition;

            // Assert
            Assert.That(oldWpos == newWpos);
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates.
        /// </summary>
        [Test]
        public void ParentRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.Transform;
            var childTrans = child.Transform;
            parentTrans.WorldPosition = new Vector2(0, 0);
            childTrans.WorldPosition = new Vector2(2, 0);
            childTrans.AttachParent(parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = childTrans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, 0), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 2), result.ToString);
        }

        /// <summary>
        ///     Tests that the entity orbits properly when the parent rotates and is not at the origin.
        /// </summary>
        [Test]
        public void ParentTransRotateTest()
        {
            // Arrange
            var parent = EntityManager.SpawnEntity("dummy");
            var child = EntityManager.SpawnEntity("dummy");
            var parentTrans = parent.Transform;
            var childTrans = child.Transform;
            parentTrans.WorldPosition = new Vector2(1, 1);
            childTrans.WorldPosition = new Vector2(2, 1);
            childTrans.AttachParent(parentTrans);

            //Act
            parentTrans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = childTrans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, 1), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 2), result.ToString);
        }

        /// <summary>
        ///     Tests to see if parenting multiple entities with WorldPosition places the leaf properly.
        /// </summary>
        [Test]
        public void PositionCompositionTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");
            var node4 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;
            var node4Trans = node4.Transform;

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);
            node4Trans.WorldPosition = new Vector2(0, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);
            node4Trans.AttachParent(node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 2);

            //Assert
            var result = node4Trans.WorldPosition;
            Assert.That(FloatMath.CloseTo(result.X, -2), result.ToString);
            Assert.That(FloatMath.CloseTo(result.Y, 0), result.ToString);
        }

        /// <summary>
        ///     Tests to see if setting the world position of a child causes position rounding errors.
        /// </summary>
        [Test]
        public void ParentWorldPositionRoundingErrorTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            // Act
            var oldWpos = node3Trans.WorldPosition;

            for (var i = 0; i < 10000; i++)
            {
                var dx = i % 2 == 0 ? 5 : -5;
                node1Trans.WorldPosition += new Vector2(dx, dx);
                node2Trans.WorldPosition += new Vector2(dx, dx);
                node3Trans.WorldPosition += new Vector2(dx, dx);
            }

            var newWpos = node3Trans.WorldPosition;

            // Assert
            Assert.That(FloatMath.CloseTo(oldWpos.X, newWpos.Y), newWpos.ToString);
            Assert.That(FloatMath.CloseTo(oldWpos.Y, newWpos.Y), newWpos.ToString);
        }

        /// <summary>
        ///     Tests to see if rotating a parent causes major child position rounding errors.
        /// </summary>
        [Test]
        public void ParentRotationRoundingErrorTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            // Act
            var oldWpos = node3Trans.WorldPosition;

            for (var i = 0; i < 100; i++)
            {
                node1Trans.LocalRotation += new Angle(MathHelper.Pi);
                node2Trans.LocalRotation += new Angle(MathHelper.Pi);
                node3Trans.LocalRotation += new Angle(MathHelper.Pi);
            }

            var newWpos = node3Trans.WorldPosition;

            //NOTE: Yes, this does cause a non-zero error

            // Assert
            Assert.That(FloatMath.CloseTo(oldWpos.X, newWpos.Y), newWpos.ToString);
            Assert.That(FloatMath.CloseTo(oldWpos.Y, newWpos.Y), newWpos.ToString);
        }

        /// <summary>
        ///     Tests that the world and inverse world transforms are built properly.
        /// </summary>
        [Test]
        public void TreeComposeWorldMatricesTest()
        {
            // Arrange
            var control = Matrix3.Identity;

            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");
            var node4 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;
            var node4Trans = node4.Transform;

            node1Trans.WorldPosition = new Vector2(0, 0);
            node2Trans.WorldPosition = new Vector2(1, 1);
            node3Trans.WorldPosition = new Vector2(2, 2);
            node4Trans.WorldPosition = new Vector2(0, 2);

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);
            node4Trans.AttachParent(node3Trans);

            //Act
            node1Trans.LocalRotation = new Angle(MathHelper.Pi / 6.37);
            node1Trans.WorldPosition = new Vector2(1, 1);

            var worldMat = node4Trans.WorldMatrix;
            var invWorldMat = node4Trans.InvWorldMatrix;

            Matrix3.Multiply(ref worldMat, ref invWorldMat, out var leftVerifyMatrix);
            Matrix3.Multiply(ref invWorldMat, ref worldMat, out var rightVerifyMatrix);

            //Assert
            // these should be the same (A × A-1 = A-1 × A = I)
            Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(rightVerifyMatrix));

            // verify matrix == identity matrix (or very close to because float precision)
            Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(control));
        }

        /// <summary>
        ///     Tests that world rotation is built properly
        /// </summary>
        [Test]
        public void WorldRotationTest()
        {
            // Arrange
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            node1Trans.LocalRotation = Angle.FromDegrees(0);
            node2Trans.LocalRotation = Angle.FromDegrees(45);
            node3Trans.LocalRotation = Angle.FromDegrees(45);

            // Act
            node1Trans.LocalRotation = Angle.FromDegrees(135);

            // Assert (135 + 45 + 45 = 225)
            var result = node3Trans.WorldRotation;
            Assert.That(result, new ApproxEqualityConstraint(Angle.FromDegrees(225)));
        }

        /// <summary>
        ///     Test that, in a chain A -> B -> C, if A is moved C's world position correctly updates.
        /// </summary>
        [Test]
        public void MatrixUpdateTest()
        {
            var node1 = EntityManager.SpawnEntity("dummy");
            var node2 = EntityManager.SpawnEntity("dummy");
            var node3 = EntityManager.SpawnEntity("dummy");

            var node1Trans = node1.Transform;
            var node2Trans = node2.Transform;
            var node3Trans = node3.Transform;

            node2Trans.AttachParent(node1Trans);
            node3Trans.AttachParent(node2Trans);

            node3Trans.LocalPosition = new Vector2(5, 5);
            node2Trans.LocalPosition = new Vector2(5, 5);
            node1Trans.LocalPosition = new Vector2(5, 5);

            Assert.That(node3Trans.WorldPosition, new ApproxEqualityConstraint(new Vector2(15, 15)));
        }
    }
}
