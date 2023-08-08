using InfernumMode.Content.Tiles.Wishes;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.WorldGeneration
{
    public static class EggShrineGeneration
    {
        public static void Generate(GenerationProgress progress, GameConfiguration _2)
        {
            progress.Message = "Hiding eggs...";

            // Generate eight shrines near the horizontal center of the world.
            int tries = 0;
            for (int i = 0; i < 8; i++)
            {
                if (tries >= 40000)
                    break;

                int x = WorldGen.genRand.Next(Main.maxTilesX / 2 - 150, Main.maxTilesX / 2 + 150);
                int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, Main.maxTilesY - 350);
                if (Main.tile[x, y].TileType != TileID.LargePiles)
                {
                    i--;
                    tries++;
                    continue;
                }

                if ((Main.tile[x, y].TileFrameX % 54 == 18 && Main.tile[x, y].TileFrameY == 0) || (Main.tile[x, y].TileFrameX % 54 == 45 && Main.tile[x, y].TileFrameY == 0))
                {
                    for (int dx = -1; dx < 2; dx++)
                    {
                        for (int dy = -1; dy < 2; dy++)
                            Main.tile[x + dx, y + dy].Get<TileWallWireStateData>().HasTile = false;
                    }

                    WorldGen.PlaceTile(x, y + 1, ModContent.TileType<EggSwordShrine>());
                }
                else
                {
                    i--;
                    tries++;
                    continue;
                }
            }
        }
    }
}