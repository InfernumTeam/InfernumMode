using InfernumMode.Content.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.WorldGeneration
{
    public static class TabletPedestalGeneration
    {
        public static void Generate(GenerationProgress progress, GameConfiguration _2)
        {
            progress.Message = "Placing a tablet...";

            int pedestalID = ModContent.TileType<TabletPedestalTile>();
            for (int tries = 0; tries < 2500; tries++)
            {
                Point placementPosition = Utilities.GetGroundPositionFrom(new Point(WorldGen.genRand.Next(480, 960), (int)WorldGen.worldSurfaceLow - 20));
                if (WorldGen.genRand.NextBool())
                    placementPosition.X = Main.maxTilesX - placementPosition.X;

                if (WorldGen.PlaceTile(placementPosition.X, placementPosition.Y - 1, pedestalID))
                    break;
            }
        }
    }
}