﻿using SS14.Shared.Map;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileAny : PlacementMode
    {
        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public AlignTileAny(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            float tileSize = MouseCoords.Grid.TileSize; //convert from ushort to float
            GridDistancing = tileSize;

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new GridLocalCoordinates(CurrentTile.X + tileSize / 2,
                    CurrentTile.Y + tileSize / 2,
                    MouseCoords.Grid);
            }
            else
            {
                MouseCoords = new GridLocalCoordinates(CurrentTile.X + tileSize / 2 + pManager.CurrentPrototype.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.CurrentPrototype.PlacementOffset.Y,
                    MouseCoords.Grid);
            }
        }

        public override bool IsValidPosition(GridLocalCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
