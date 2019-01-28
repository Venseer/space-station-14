﻿using System;
using SS14.Client.Utility;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Client.Graphics.ClientEye;

namespace SS14.Client.Placement.Modes
{
    public class SnapgridCenter : PlacementMode
    {
        bool onGrid;
        float snapSize;

        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridCenter(PlacementManager pMan) : base(pMan) { }

        public override void Render()
        {
            if (GameController.OnGodot && onGrid)
            {
                const int ppm = EyeManager.PIXELSPERMETER;
                var viewportSize = pManager.sceneTree.SceneTree.Root.Size.Convert();
                var position = pManager.eyeManager.ScreenToWorld(Vector2.Zero);
                var gridstart = pManager.eyeManager.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                    (float)(Math.Round(position.X / snapSize - 0.5f, MidpointRounding.AwayFromZero) + 0.5f) * snapSize,
                    (float)(Math.Round(position.Y / snapSize - 0.5f, MidpointRounding.AwayFromZero) + 0.5f) * snapSize));
                var flip = new Godot.Vector2(1, -1);
                for (var a = gridstart.X; a < viewportSize.X; a += snapSize * 32) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0)).Convert() * ppm * flip;
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y)).Convert() * ppm * flip;
                    pManager.DrawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += snapSize * 32)
                {
                    var from = ScreenToWorld(new Vector2(0, a)).Convert() * ppm * flip;
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a)).Convert() * ppm * flip;
                    pManager.DrawNode.DrawLine(from, to, new Godot.Color(0, 0, 1), 0.5f);
                }
            }

            // Draw grid BELOW the ghost thing.
            base.Render();
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            snapSize = MouseCoords.Grid.SnapSize; //Find snap size.
            GridDistancing = snapSize;
            onGrid = true;

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(Math.Round((MouseCoords.Position.X / (double)snapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * snapSize,
                (float)(Math.Round((MouseCoords.Position.Y / (double)snapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * snapSize);

            //Adjust mouseCoords to new calculated position
            MouseCoords = new GridCoordinates(mouseLocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y), MouseCoords.Grid);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
