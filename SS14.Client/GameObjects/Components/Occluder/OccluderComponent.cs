﻿using System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Client.Graphics.Lighting;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Client.Graphics.ClientEye;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.ViewVariables;

namespace SS14.Client.GameObjects
{
    public class OccluderComponent : Component, IComponentDebug
    {
        public override string Name => "Occluder";

        [ViewVariables]
        public Box2 BoundingBox { get; private set; } = new Box2(-16, -16, 16, 16);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateEnabled();
            }
        }

        // TODO: Unhardcode SideSize. Should be based on grid size.
        const float SideSize = 1;
        bool _enabled = true;
        private IOccluder[] occluders = new IOccluder[4];
        private OccluderComponent[] neighbors = new OccluderComponent[4];
        private SnapGridComponent SnapGrid;
        [Dependency]
        private readonly ILightManager lightManager;

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            var transform = Owner.Transform;
            SnapGrid = Owner.GetComponent<SnapGridComponent>();
            SnapGrid.OnPositionChanged += SnapGridPositionChanged;

            const float halfSize = (-SideSize / 2) * EyeManager.PIXELSPERMETER;
            var ne = new Vector2(halfSize, -halfSize);
            var se = new Vector2(halfSize, halfSize);
            var sw = new Vector2(-halfSize, halfSize);
            var nw = new Vector2(-halfSize, -halfSize);

            // North occluder.
            var occluder = lightManager.MakeOccluder();
            occluder.CullMode = OccluderCullMode.Clockwise;
            occluder.SetPolygon(new Vector2[] { nw, ne });
            occluders[(int)OccluderDir.North] = occluder;
            occluder.ParentTo(transform);

            // East occluder.
            occluder = lightManager.MakeOccluder();
            occluder.CullMode = OccluderCullMode.Clockwise;
            occluder.SetPolygon(new Vector2[] { ne, se });
            occluders[(int)OccluderDir.East] = occluder;
            occluder.ParentTo(transform);

            // South occluder.
            occluder = lightManager.MakeOccluder();
            occluder.CullMode = OccluderCullMode.Clockwise;
            occluder.SetPolygon(new Vector2[] { se, sw });
            occluders[(int)OccluderDir.South] = occluder;
            occluder.ParentTo(transform);

            // West occluder.
            occluder = lightManager.MakeOccluder();
            occluder.CullMode = OccluderCullMode.Clockwise;
            occluder.SetPolygon(new Vector2[] { sw, nw });
            occluders[(int)OccluderDir.West] = occluder;
            occluder.ParentTo(transform);

            UpdateConnections(true);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            foreach (var occluder in occluders)
            {
                occluder.Dispose();
            }

            occluders = null;
            SnapGrid.OnPositionChanged -= SnapGridPositionChanged;
            SayGoodbyes();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _enabled, "enabled", true);
        }

        void UpdateEnabled()
        {
            occluders[0].Enabled = Enabled && neighbors[0] == null;
            occluders[1].Enabled = Enabled && neighbors[1] == null;
            occluders[2].Enabled = Enabled && neighbors[2] == null;
            occluders[3].Enabled = Enabled && neighbors[3] == null;
        }

        void SnapGridPositionChanged()
        {
            SayGoodbyes();
            UpdateConnections(true);
        }

        void SayGoodbyes()
        {
            foreach (var neighbor in neighbors)
            {
                // Goodbye neighbor.
                neighbor?.UpdateConnections(false);
            }
        }

        void UpdateConnections(bool propagate)
        {
            neighbors[0] = neighbors[1] = neighbors[2] = neighbors[3] = null;

            if (Deleted)
            {
                return;
            }

            void checkDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in SnapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out OccluderComponent comp))
                    {
                        neighbors[(int)oclDir] = comp;
                        if (propagate)
                        {
                            comp.UpdateConnections(false);
                        }
                        break;
                    }
                }
            }

            checkDir(Direction.North, OccluderDir.North);
            checkDir(Direction.East, OccluderDir.East);
            checkDir(Direction.South, OccluderDir.South);
            checkDir(Direction.West, OccluderDir.West);

            UpdateEnabled();
        }

        string IComponentDebug.GetDebugString()
        {
            return string.Format("N/S/E/W: {0}/{1}/{2}/{3}",
                neighbors[(int)OccluderDir.North]?.Owner.Uid,
                neighbors[(int)OccluderDir.South]?.Owner.Uid,
                neighbors[(int)OccluderDir.East]?.Owner.Uid,
                neighbors[(int)OccluderDir.West]?.Owner.Uid
            );
        }

        enum OccluderDir : byte
        {
            North = 0,
            East = 1,
            South = 2,
            West = 3,
        }
    }
}
