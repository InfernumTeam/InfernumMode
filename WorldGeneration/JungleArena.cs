using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace InfernumMode.WorldGeneration
{
    public static class JungleArena
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            for (int j = 0; j < 5000; j++)
            {
                int x = WorldGen.genRand.Next(Main.maxTilesX / 10, Main.maxTilesX * 9 / 10);
                int y = WorldGen.genRand.Next((int)Main.rockLayer, Main.maxTilesY - 740);

                if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.JungleGrass && Main.tile[x, y].WallType != WallID.LihzahrdBrick)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        x += WorldGen.genRand.Next(-15, 15);
                        y += WorldGen.genRand.Next(-15, 15);
                        WorldUtils.Gen(new Point(x, y), new Shapes.Circle(70), Actions.Chain(
                            new Modifiers.Blotches(12),
                            new Modifiers.SkipTiles(TileID.LihzahrdBrick),
                            new Actions.ClearTile(),
                            new Actions.PlaceWall(WallID.MudUnsafe)
                            ));
                    }
                    break;
                }
            }
        }
    }
}