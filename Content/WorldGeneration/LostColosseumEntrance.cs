using CalamityMod;
using CalamityMod.Schematics;
using CalamityMod.World;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.Content.WorldGeneration
{
    public static class LostColosseumEntrance
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            Point placementPosition = GenVars.UndergroundDesertLocation.Center;
            placementPosition.Y -= 75;

            // Use the sunken sea lab as a reference if not in the middle of worldgen, since the underground desert location rectangle is discarded after initial world-gen, meaning
            // that it won't contain anything useful.
            if (NPC.downedGolemBoss)
            {
                placementPosition = CalamityWorld.SunkenSeaLabCenter.ToTileCoordinates();
                placementPosition.Y -= 280;
            }

            // If for some reason you STILL don't have a valid placement position, just go searching for some hardened sand.
            if (placementPosition.X <= 50 || placementPosition.Y <= 50)
            {
                for (int i = 0; i < 10000; i++)
                {
                    Point p = new(Main.rand.Next(400, Main.maxTilesX - 400), Main.rand.Next(500, Main.maxTilesY - 560));
                    Tile t = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);
                    if (!t.HasTile || t.TileType != TileID.HardenedSand || t.HasActuator)
                        continue;

                    placementPosition = p;
                    break;
                }
            }

            // If EVEN THEN there's no valid point, you get nothing. Goodbye.
            if (placementPosition.X <= 50 || placementPosition.Y <= 50)
                return;

            // As much as I would like to do so, I will resist the urge to write juvenile venting comments about my current frustrations with this
            // requested feature inside of a code comment.
            // This part creates a lumpy, circular layer of sandstone around the entrance.
            for (int i = 0; i < 5; i++)
            {
                Point lumpCenter = (placementPosition.ToVector2() + WorldGen.genRand.NextVector2Circular(15f, 15f)).ToPoint();
                WorldUtils.Gen(lumpCenter, new Shapes.Circle(88, 50), Actions.Chain(new GenAction[]
                {
                    new Modifiers.RadialDither(82f, 88f),
                    new Actions.SetTile(TileID.Sandstone, true)
                }));
            }

            // Carve a cave through the sandstone.
            int caveSeed = WorldGen.genRand.Next();
            Point cavePosition = placementPosition;
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

            bool _3 = false;
            placementPosition.X += 32;
            PlaceSchematic<Action<Chest>>("LostColosseumEntrance", placementPosition, SchematicAnchor.Center, ref _3);

            // Sync the tile changes in case they were done due to a boss kill effect.
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = placementPosition.X - 124; i < placementPosition.X + 124; i++)
                {
                    for (int j = placementPosition.Y - 100; j < placementPosition.Y + 100; j++)
                        NetMessage.SendTileSquare(-1, i, j);
                }
            }

            WorldSaveSystem.HasGeneratedColosseumEntrance = true;
        }
    }
}
