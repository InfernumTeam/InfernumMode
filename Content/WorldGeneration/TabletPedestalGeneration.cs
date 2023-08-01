using CalamityMod;
using CalamityMod.Schematics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.WorldGeneration
{
    public static class TabletPedestalGeneration
    {
        public static void Generate(GenerationProgress progress, GameConfiguration _2)
        {
            progress.Message = "Placing a tablet...";

            for (int tries = 0; tries < 2500; tries++)
            {
                Point placementPosition = Utilities.GetGroundPositionFrom(new Point(WorldGen.genRand.Next(480, 960), (int)GenVars.worldSurfaceLow - 20));
                if (WorldGen.genRand.NextBool())
                    placementPosition.X = Main.maxTilesX - placementPosition.X;

                Tile t = CalamityUtils.ParanoidTileRetrieval(placementPosition.X, placementPosition.Y);
                if (t.TileType != TileID.Grass)
                    continue;

                // Make sure that the area is flat.
                Point left = placementPosition;
                left.X -= 18;
                Point right = placementPosition;
                right.X += 18;

                if (AverageElevation(left, right) >= 4f)
                    continue;

                placementPosition.Y = Math.Max(Utilities.GetGroundPositionFrom(left).Y, Utilities.GetGroundPositionFrom(right).Y);

                bool _ = false;
                SchematicManager.PlaceSchematic<Action<Chest>>("TabletPedestal", placementPosition, SchematicAnchor.BottomCenter, ref _);
                break;
            }
        }

        public static float AverageElevation(Point left, Point right)
        {
            int elevation = 0;
            for (int x = left.X; x <= right.X; x++)
            {
                Point p = new(x, left.Y);
                if (WorldGen.SolidTile(p))
                {
                    while (WorldGen.SolidTile(p))
                        p.Y--;
                }
                else
                    p = Utilities.GetGroundPositionFrom(p);

                elevation += p.Y - left.Y;
            }

            return elevation / (float)(right.X - left.X);
        }
    }
}