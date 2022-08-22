using CalamityMod.Schematics;
using CalamityMod.Tiles.FurnitureProfaned;
using CalamityMod.Walls;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.Systems
{
    public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int floatingIslandIndex = tasks.FindIndex(g => g.Name == "Floating Islands");
            if (floatingIslandIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Desert Digout Area", GenerateUndergroundDesertArea));
            int finalCleanupIndex = tasks.FindIndex(g => g.Name == "Final Cleanup");
            if (finalCleanupIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Jungle Digout Area", GenerateUndergroundJungleArea));
            int finalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
            if (finalIndex != -1)
            {
                int currentFinalIndex = finalIndex;
                tasks.Insert(++currentFinalIndex, new PassLegacy("Prov Arena", (progress, config) =>
                {
                    progress.Message = "Constructing a temple for an ancient goddess";
                    GenerateProfanedArena(progress, config);
                }));
            }

            int astralChestIndex = tasks.FindIndex(genpass => genpass.Name.Equals("CalamityDungeonBiomeChests"));
            tasks.RemoveAt(astralChestIndex);
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

        public static void GenerateProfanedArena(GenerationProgress _, GameConfiguration _2)
        {
            bool _3 = false;
            Point bottomLeftOfWorld = new(Main.maxTilesX - 30, Main.maxTilesY - 42);
            PlaceSchematic<Action<Chest>>("Profaned Arena", bottomLeftOfWorld, SchematicAnchor.BottomRight, ref _3);
            SchematicMetaTile[,] schematic = TileMaps["Profaned Arena"];
            int width = schematic.GetLength(0);
            int height = schematic.GetLength(1);

            WorldSaveSystem.ProvidenceArena = new(bottomLeftOfWorld.X - width, bottomLeftOfWorld.Y - height, width, height);
            WorldSaveSystem.HasGeneratedProfanedShrine = true;
        }

        public static void GenerateProfanedShrinePillar(Point bottom, int topY)
        {
            ushort runicBrickWallID = (ushort)ModContent.WallType<RunicProfanedBrickWall>();
            ushort profanedSlabWallID = (ushort)ModContent.WallType<ProfanedSlabWall>();
            ushort profanedRockWallID = (ushort)ModContent.WallType<ProfanedRockWall>();

            int y = bottom.Y;

            while (y > topY)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = 0; dy < 6; dy++)
                    {
                        ushort wallID = profanedRockWallID;
                        if (Math.Abs(dx) >= 2 || dy <= 1 || dy == 4)
                            wallID = profanedSlabWallID;
                        if (Math.Abs(dx) <= 1 && dy >= 1 && dy <= 3)
                            wallID = runicBrickWallID;
                        if (Math.Abs(dx) == 2 || dy == 5)
                            wallID = profanedRockWallID;

                        int x = bottom.X + dx;
                        Main.tile[x, y + dy].WallType = wallID;
                        if (y + dy == (bottom.Y + topY) / 2 - 1 && dx == 0)
                        {
                            Main.tile[x, y + dy].TileType = TileID.Torches;
                            Main.tile[x, y + dy].Get<TileWallWireStateData>().HasTile = true;
                        }
                    }
                }
                y -= 6;
            }

            // Frame everything.
            for (y = topY; y < bottom.Y; y += 6)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = 0; dy < 6; dy++)
                    {
                        int x = bottom.X + dx;
                        WorldGen.SquareWallFrame(x, y + dy);
                    }
                }
            }
        }
    }
}