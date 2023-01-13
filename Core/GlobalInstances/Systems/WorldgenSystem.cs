using CalamityMod;
using CalamityMod.Schematics;
using CalamityMod.World;
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

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int finalCleanupIndex = tasks.FindIndex(g => g.Name == "Final Cleanup");
            if (finalCleanupIndex != -1)
            {
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Jungle Digout Area", GenerateUndergroundJungleArea));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Dungeon Digout Area", GenerateDungeonArea));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Prov Arena", (progress, config) =>
                {
                    progress.Message = "Constructing a temple for an ancient goddess";
                    GenerateProfanedArena(progress, config);
                }));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Desert Digout Area", GenerateLostColosseumEntrance));
            }
        }

        public static void GenerateLostColosseumEntrance(GenerationProgress progress, GameConfiguration config)
        {
            Point fuck = WorldGen.UndergroundDesertLocation.Center;
            fuck.Y -= 75;

            // Use the sunken sea lab as a reference if not in the middle of worldgen, since the underground desert location rectangle is discarded after initial world-gen, meaning
            // that it won't contain anything useful.
            if (NPC.downedGolemBoss)
            {
                fuck = CalamityWorld.SunkenSeaLabCenter.ToTileCoordinates();
                fuck.Y -= 280;
            }

            // If for some reason you STILL don't have a valid placement position, just go searching for some hardened sand.
            if (fuck.X <= 50 || fuck.Y <= 50)
            {
                for (int i = 0; i < 10000; i++)
                {
                    Point p = new(Main.rand.Next(400, Main.maxTilesX - 400), Main.rand.Next(500, Main.maxTilesY - 560));
                    Tile t = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);
                    if (!t.HasTile || t.TileType != TileID.HardenedSand || t.HasActuator)
                        continue;

                    fuck = p;
                    break;
                }
            }

            // If EVEN THEN there's no valid point, you get nothing. Goodbye.
            if (fuck.X <= 50 || fuck.Y <= 50)
                return;

            // As much as I would like to do so, I will resist the urge to write juvenile venting comments about my current frustrations with this
            // requested feature inside of a code comment.
            // This part creates a lumpy, circular layer of sandstone around the entrance.
            for (int i = 0; i < 5; i++)
            {
                Point lumpCenter = (fuck.ToVector2() + WorldGen.genRand.NextVector2Circular(15f, 15f)).ToPoint();
                WorldUtils.Gen(lumpCenter, new Shapes.Circle(88, 65), Actions.Chain(new GenAction[]
                {
                    new Modifiers.RadialDither(82f, 88f),
                    new Actions.SetTile(TileID.Sandstone, true)
                }));
            }

            // Carve a cave through the sandstone.
            int caveSeed = WorldGen.genRand.Next();
            Point cavePosition = fuck;
            cavePosition.X -= 12;
            cavePosition.Y += 10;

            for (int dx = 0; dx < 24; dx++)
            {
                int perlinOffset = (int)(SulphurousSea.FractalBrownianMotion(cavePosition.X * 0.16f, cavePosition.Y * 0.16f, caveSeed, 4) * 6f);
                cavePosition.Y += perlinOffset;

                WorldUtils.Gen(cavePosition, new Shapes.Rectangle(9, 9), Actions.Chain(new GenAction[]
                {
                    new Actions.ClearTile(),
                    new Actions.PlaceWall(WallID.Sandstone)
                }));
                cavePosition.X -= 4;
            }

            bool _ = false;
            fuck.X += 32;
            PlaceSchematic<Action<Chest>>("LostColosseumEntrance", fuck, SchematicAnchor.Center, ref _);

            // Sync the tile changes in case they were done due to a boss kill effect.
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = fuck.X - 124; i < fuck.X + 124; i++)
                {
                    for (int j = fuck.Y - 100; j < fuck.Y + 100; j++)
                        NetMessage.SendTileSquare(-1, i, j);
                }
            }

            WorldSaveSystem.HasGeneratedColosseumEntrance = true;
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

        public static void GenerateDungeonArea(GenerationProgress progress, GameConfiguration configuration)
        {
            int boxArea = 95;
            ushort dungeonWallID = 0;
            ushort dungeonTileID = 0;
            switch (WorldGen.crackedType)
            {
                case TileID.CrackedBlueDungeonBrick:
                    dungeonWallID = WallID.BlueDungeonSlabUnsafe;
                    dungeonTileID = TileID.BlueDungeonBrick;
                    break;
                case TileID.CrackedGreenDungeonBrick:
                    dungeonWallID = WallID.GreenDungeonSlabUnsafe;
                    dungeonTileID = TileID.GreenDungeonBrick;
                    break;
                case TileID.CrackedPinkDungeonBrick:
                    dungeonWallID = WallID.PinkDungeonSlabUnsafe;
                    dungeonTileID = TileID.PinkDungeonBrick;
                    break;
            }

            int index = WorldGen.numDungeonPlatforms / 2 + 1;
            Point dungeonCenter = new(WorldGen.dungeonPlatformX[index], WorldGen.dungeonPlatformY[index]);
            WorldUtils.Gen(dungeonCenter, new Shapes.Rectangle(boxArea, boxArea), Actions.Chain(
                new Actions.SetTile(dungeonTileID, true)));
            WorldUtils.Gen(new(dungeonCenter.X + 2, dungeonCenter.Y + 2), new Shapes.Rectangle(boxArea - 2, boxArea - 2), Actions.Chain(
                new Actions.ClearTile(),
                new Actions.PlaceWall(dungeonWallID)));
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

            // Sync the tile changes in case they were done due to a boss kill effect.
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = bottomLeftOfWorld.X - width; i < bottomLeftOfWorld.X; i++)
                {
                    for (int j = bottomLeftOfWorld.Y - height; j < bottomLeftOfWorld.Y; j++)
                        NetMessage.SendTileSquare(-1, i, j);
                }
            }

            WorldSaveSystem.HasGeneratedProfanedShrine = true;
        }
    }
}