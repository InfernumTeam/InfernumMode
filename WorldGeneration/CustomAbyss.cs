using CalamityMod;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Walls;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.World.SulphurousSea;

namespace InfernumMode.WorldGeneration
{
    public static class CustomAbyss
    {
        #region Fields and Properties

        // Loop variables that are accessed via getter methods should be stored externally in local variables for performance reasons.
        public static int MinAbyssWidth => MaxAbyssWidth / 6;

        public static int MaxAbyssWidth => BiomeWidth + 64;

        public static int AbyssTop => YStart + BlockDepth - 36;

        public static int Layer4Top => (int)(Main.rockLayer + Main.maxTilesY * 0.26);

        public static ref int AbyssBottom => ref Abyss.AbyssChasmBottom;

        #endregion Fields and Properties

        #region Placement Methods

        public static void Generate()
        {
            // Define the bottom of the abyss first and foremost.
            AbyssBottom = Main.UnderworldLayer - 42;

            GenerateGravelBlock();
            GenerateLayer4();
        }
        #endregion Placement Methods

        #region Generation Functions
        
        public static void GenerateGravelBlock()
        {
            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;
            int top = AbyssTop;
            int bottom = AbyssBottom;
            ushort gravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort gravelWallID = (ushort)ModContent.WallType<AbyssGravelWall>();

            for (int i = 1; i < maxWidth; i++)
            {
                int x = GetActualX(i);
                for (int y = top; y < bottom; y++)
                {
                    // Decide whether to cut off due to a Y point being far enough.
                    float yCompletion = Utils.GetLerpValue(top, bottom - 1f, y, true);
                    if (i >= GetWidth(yCompletion, minWidth, maxWidth))
                        continue;

                    // Otherwise, lay down gravel.
                    Main.tile[x, y].TileType = gravelID;
                    Main.tile[x, y].WallType = gravelWallID;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[x, y].LiquidAmount = 0;
                }
            }
        }

        public static void GenerateLayer4()
        {
            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;
            int entireAbyssTop = AbyssTop;
            int top = Layer4Top;
            int bottom = AbyssBottom;
            int wallWidth = 68;
            float topOffsetPhaseShift = WorldGen.genRand.NextFloat(MathHelper.TwoPi);
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();

            for (int i = 1; i < maxWidth; i++)
            {
                int x = GetActualX(i);
                float xCompletion = i / (float)maxWidth;
                int yOffset = (int)(CalamityUtils.Convert01To010(xCompletion) * (float)Math.Sin(MathHelper.Pi * xCompletion + topOffsetPhaseShift * 6f) * 20f);
                for (int y = top - yOffset; y < bottom - wallWidth + yOffset / 3; y++)
                {
                    // Decide whether to cut off due to a Y point being far enough.
                    float yCompletion = Utils.GetLerpValue(entireAbyssTop, bottom - 1f, y, true);
                    if (i >= GetWidth(yCompletion, minWidth, maxWidth) - wallWidth)
                        continue;

                    // Otherwise, clear out water.
                    Main.tile[x, y].WallType = voidstoneWallID;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                    Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                    Main.tile[x, y].LiquidAmount = 255;
                }
            }
        }
        #endregion Generation Functions

        #region Utilities

        public static int GetWidth(float yCompletion, int minWidth, int maxWidth)
        {
            return (int)MathHelper.Lerp(minWidth, maxWidth, (float)Math.Pow(yCompletion * Utils.GetLerpValue(1f, 0.81f, yCompletion, true), 0.13));
        }
        #endregion Utilities
    }
}