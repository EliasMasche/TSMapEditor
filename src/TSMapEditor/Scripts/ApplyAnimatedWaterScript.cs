﻿using Rampastring.Tools;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.Scripts
{
    public class ApplyAnimatedWaterScript
    {
        private TileSet waterTileSet;

        private const string AnimatedWaterTileSetName = "Animated Water";
        private const string WaterTileSetName = "Water";

        public static bool IsAvailable(Map map)
        {
            if (map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == AnimatedWaterTileSetName) == null)
            {
                Logger.Log("TileSet for animated water not found!");
                return false;
            }

            if (map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == WaterTileSetName) == null)
            {
                Logger.Log("TileSet for regular (non-animated) water not found!");
                return false;
            }

            return true;
        }

        public void Perform(Map map)
        {
            var animatedWaterTileSet = map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == AnimatedWaterTileSetName);
            if (animatedWaterTileSet == null)
            {
                Logger.Log("TileSet for animated water not found!");
                return;
            }

            waterTileSet = map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == WaterTileSetName);
            if (waterTileSet == null)
            {
                Logger.Log("TileSet for regular (non-animated) water not found!");
                return;
            }

            // Specifies the tiles to pick from on each round.
            // If multiple tiles are specified for a round, one of them is selected
            // with RNG (there are multiple kinds of 2x2 animated water tiles).
            int[][] tileIndexesToPickFrom = new int[][] 
            { 
                new int[] { 9 },             // Big 10x10 animated water tile
                new int[] { 7, 8 },          // 5x5 animated water tiles
                new int[] { 6 },             // 4x4
                new int[] { 5 },             // 3x3
                new int[] { 0, 1, 2, 3, 4 }, // 2x2 tiles
                new int[] { 10, 11, 12, 13 } // 1x1 tiles
            };

            Random random = new Random();

            // 1st loop - loops through animated water tiles of different sizes
            for (int sizeTypeIndex = 0; sizeTypeIndex < tileIndexesToPickFrom.Length; sizeTypeIndex++)
            {
                // Fetch the animated water tile so we can fetch its size

                ITileImage tileImage = map.TheaterInstance.GetTile(animatedWaterTileSet.StartTileIndex + tileIndexesToPickFrom[sizeTypeIndex][0]);
                int tileWidth = tileImage.Width;
                int tileHeight = tileImage.Height;

                map.DoForAllValidTiles(mapCell =>
                {
                    const int margin = 4;

                    // As an animation, animated water slows down the game, so don't place it outside of the visible map area
                    var pixelCoords = CellMath.CellTopLeftPointFromCellCoords(mapCell.CoordsToPoint(), map);
                    if (pixelCoords.Y < Constants.CellSizeY * margin || pixelCoords.Y > map.Size.Y * Constants.CellSizeY - (Constants.CellSizeY * margin))
                        return;

                    if (pixelCoords.X < Constants.CellSizeX * margin || pixelCoords.X > map.Size.X * Constants.CellSizeX - (Constants.CellSizeX * margin))
                        return;

                    // Check whether this cell contains water
                    if (!IsWaterTile(mapCell.TileIndex))
                        return;

                    // We know that we're on a water cell, check if we can fit the animated water tile here
                    if (!CanFitAnimatedWaterTileHere(map, mapCell.X, mapCell.Y, tileWidth, tileHeight))
                        return;

                    // If we can fit the animated water tile here, then proceed to
                    // randomly select a tile from the list of animated water tiles
                    // of the current size
                    int[] potentialTileIndexes = tileIndexesToPickFrom[sizeTypeIndex];
                    tileImage = map.TheaterInstance.GetTile(animatedWaterTileSet.StartTileIndex + potentialTileIndexes[random.Next(potentialTileIndexes.Length)]);

                    // Place the tile!
                    map.PlaceTerrainTileAt(tileImage, mapCell.CoordsToPoint());
                });
            }
        }

        private bool IsWaterTile(int tileIndex)
        {
            if (tileIndex < waterTileSet.StartTileIndex || tileIndex >= waterTileSet.StartTileIndex + waterTileSet.TilesInSet)
                return false;

            return true;
        }

        private bool CanFitAnimatedWaterTileHere(Map map, int x, int y, int tileWidth, int tileHeight)
        {
            for (int cy = 0; cy < tileHeight; cy++)
            {
                for (int cx = 0; cx < tileWidth; cx++)
                {
                    var mapCell = map.GetTile(x + cx, y + cy);
                    if (mapCell == null)
                        return false;

                    if (!IsWaterTile(mapCell.TileIndex))
                        return false;
                }
            }

            return true;
        }
    }
}