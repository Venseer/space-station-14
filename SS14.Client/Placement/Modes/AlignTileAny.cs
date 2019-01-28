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
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2,
                    CurrentTile.Y + tileSize / 2,
                    MouseCoords.Grid);
            }
            else
            {
                MouseCoords = new GridCoordinates(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                    CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y,
                    MouseCoords.Grid);
            }
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
