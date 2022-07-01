using CalamityMod.Tiles.FurnitureProfaned;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Systems
{
    public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int floatingIslandIndex = tasks.FindIndex(g => g.Name == "Floating Islands");
            if (floatingIslandIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Desert Digout Area", GenerateUndergroundDesertArea));
            int jungleTreesIndex = tasks.FindIndex(g => g.Name == "Jungle Trees");
            if (jungleTreesIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Jungle Digout Area", GenerateUndergroundJungleArea));
        }

        public static void GenerateUndergroundDesertArea(GenerationProgress progress, GameConfiguration config)
        {
            Vector2 cutoutAreaCenter = WorldGen.UndergroundDesertLocation.Center.ToVector2();

            for (int i = 0; i < 4; i++)
            {
                cutoutAreaCenter += WorldGen.genRand.NextVector2Circular(15f, 15f);
                WorldUtils.Gen(cutoutAreaCenter.ToPoint(), new Shapes.Mound(75, 48), Actions.Chain(
                    new Modifiers.Blotches(12),
                    new Actions.ClearTile(),
                    new Actions.PlaceWall(WallID.Sandstone)
                    ));
            }
        }

        public static void GenerateUndergroundJungleArea(GenerationProgress progress, GameConfiguration configuration)
        {
            for (int j = 0; j < 5000; j++)
            {
                int x;
                if (WorldGen.dungeonX < Main.maxTilesX / 2)
                    x = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.6), (int)(Main.maxTilesX * 0.85));
                else
                    x = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.15), (int)(Main.maxTilesX * 0.5));

                int y = WorldGen.genRand.Next((int)Main.rockLayer, Main.maxTilesY - 840);

                if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.JungleGrass && Main.tile[x, y].WallType != WallID.LihzahrdBrick)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        x += WorldGen.genRand.Next(-15, 15);
                        y += WorldGen.genRand.Next(-15, 15);
                        WorldUtils.Gen(new Point(x, y), new Shapes.Circle(70), Actions.Chain(
                            new Modifiers.Blotches(12),
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