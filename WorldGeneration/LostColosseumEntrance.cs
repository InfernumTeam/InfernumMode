using CalamityMod.Schematics;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.WorldGeneration
{
    public static class LostColosseumEntrance
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            Point fuck = WorldGen.UndergroundDesertLocation.Center;
            fuck.Y -= 75;

            // As much as I would like to do so, I will resist the urge to write juvenile venting comments about my current frustrations with this
            // requested feature inside of a code comment.
            // This part creates a lumpy, circular layer of sandstone around the entrance.
            for (int i = 0; i < 5; i++)
            {
                Point lumpCenter = (fuck.ToVector2() + Main.rand.NextVector2Circular(15f, 15f)).ToPoint();
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
        }
    }
}
