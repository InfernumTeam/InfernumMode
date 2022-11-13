using CalamityMod;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Walls;
using CalamityMod.World;
using InfernumMode.Systems;
using InfernumMode.Tiles.Abyss;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using static CalamityMod.World.SulphurousSea;

namespace InfernumMode.WorldGeneration
{
    public static class CustomAbyss
    {
        #region Fields and Properties

        // Loop variables that are accessed via getter methods should be stored externally in local variables for performance reasons.
        public static int MinAbyssWidth => MaxAbyssWidth / 6;

        public static int MaxAbyssWidth => BiomeWidth + 42;

        public static int AbyssTop => YStart + BlockDepth - 64;

        public static int Layer2Top => (int)(Main.rockLayer + Main.maxTilesY * 0.084f);

        public static int Layer3Top => (int)(Main.rockLayer + Main.maxTilesY * 0.17f);

        public static int Layer4Top => (int)(Main.rockLayer + Main.maxTilesY * 0.29f);

        public static ref int AbyssBottom => ref Abyss.AbyssChasmBottom;

        // 0-1 value that determines the threshold for layer 1 spaghetti caves being carved out. At 0, no tiles are carved out, at 1, all tiles are carved out.
        // This is used in the formula 'abs(noise(x, y)) < r' to determine whether the cave should remove tiles.
        public static readonly float[] Layer1SpaghettiCaveCarveOutThresholds = new float[]
        {
            0.0382f,
            0.0497f
        };

        public static int Layer2TrenchCount => (int)Math.Sqrt(Main.maxTilesX / 176f);

        public const int MinStartingTrenchWidth = 5;

        public const int MaxStartingTrenchWidth = 8;

        public const int MinEndingTrenchWidth = 20;

        public const int MaxEndingTrenchWidth = 27;

        public const float TrenchTightnessFactor = 1.72f;
        
        public const float TrenchWidthNoiseMagnificationFactor = 0.00292f;

        public const float TrenchOffsetNoiseMagnificationFactor = 0.00261f;

        public const int MaxTrenchOffset = 28;

        // How thick walls should be between the insides of the abyss and the outside. This should be relatively high, since you don't want the boring
        // vanilla caverns to be visible from within the abyss, for immersion reasons.
        public const int WallThickness = 70;

        #endregion Fields and Properties

        #region Placement Methods

        public static void Generate()
        {
            // Define the bottom of the abyss first and foremost.
            AbyssBottom = Main.UnderworldLayer - 42;

            // Mark this world as being made in the AEW update.
            WorldSaveSystem.InPostAEWUpdateWorld = true;

            GenerateGravelBlock();
            GenerateSulphurousSeaCut();
            GenerateLayer1();
            GenerateLayer2();
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

        public static void GenerateSulphurousSeaCut()
        {
            int topOfSulphSea = YStart - 10;
            int bottomOfSulphSea = AbyssTop + 25;
            int centerX = GetActualX(BiomeWidth / 2 - 32);
            for (int y = topOfSulphSea; y < bottomOfSulphSea; y++)
            {
                float yCompletion = Utils.GetLerpValue(topOfSulphSea, bottomOfSulphSea - 1f, y, true);
                int width = (int)Utils.Remap(yCompletion, 0f, 0.33f, 1f, 36f) + WorldGen.genRand.Next(-1, 2);

                // Carve out water through the sulph sea.
                for (int dx = -width; dx < width; dx++)
                {
                    int x = centerX + dx;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                    Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                    Main.tile[x, y].LiquidAmount = 255;
                    Tile.SmoothSlope(x, y);
                }
            }
        }

        public static void GenerateLayer1()
        {
            int topOfLayer1 = AbyssTop - 4;
            int bottomOfLayer1 = Layer2Top;
            int entireAbyssTop = AbyssTop;
            int entireAbyssBottom = AbyssBottom;
            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;

            for (int c = 0; c < Layer1SpaghettiCaveCarveOutThresholds.Length; c++)
            {
                int caveSeed = WorldGen.genRand.Next();
                for (int y = topOfLayer1; y < bottomOfLayer1; y++)
                {
                    float yCompletion = Utils.GetLerpValue(entireAbyssTop, entireAbyssBottom, y, true);
                    int width = GetWidth(yCompletion, minWidth, maxWidth) - WallThickness;
                    for (int i = 2; i < width; i++)
                    {
                        // Initialize variables for the cave.
                        int x = GetActualX(i);
                        float noise = FractalBrownianMotion(i * SpaghettiCaveMagnification, y * SpaghettiCaveMagnification, caveSeed, 5);

                        // Bias noise away from 0, effectively making caves less likely to appear, based on how close it is to the edges and bottom.
                        float biasAwayFrom0Interpolant = Utils.GetLerpValue(topOfLayer1 + 12f, topOfLayer1, y, true) * 0.2f;
                        biasAwayFrom0Interpolant += Utils.GetLerpValue(width - 24f, width - 9f, i, true) * 0.4f;
                        biasAwayFrom0Interpolant += Utils.GetLerpValue(bottomOfLayer1 - 16f, bottomOfLayer1 - 3f, y, true) * 0.4f;

                        // If the noise is less than 0, bias to -1, if it's greater than 0, bias away to 1.
                        // This is done instead of biasing to -1 or 1 without exception to ensure that in doing so the noise does not cross into the
                        // cutout threshold near 0 as it interpolates.
                        noise = MathHelper.Lerp(noise, Math.Sign(noise), biasAwayFrom0Interpolant);
                        
                        if (Math.Abs(noise) < Layer1SpaghettiCaveCarveOutThresholds[c])
                        {
                            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                            Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                            Main.tile[x, y].LiquidAmount = 255;
                            Tile.SmoothSlope(x, y);
                        }
                    }
                }
            }

            // Clear out any stray tiles created by the cave generation.
            Rectangle layer1Area = new(1, topOfLayer1, maxWidth - WallThickness, bottomOfLayer1 - topOfLayer1);
            ClearOutStrayTiles(layer1Area);

            // Generate sulphurous gravel on the cave walls.
            GenerateLayer1SulphurousGravel(layer1Area);
        }

        public static void GenerateLayer1SulphurousGravel(Rectangle layer1Area)
        {
            int sandstoneSeed = WorldGen.genRand.Next();
            ushort abyssGravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort sulphuricGravelID = (ushort)ModContent.TileType<SulphurousGravel>();

            // Edge score evaluation function that determines the propensity a tile has to become sandstone.
            // This is based on how much nearby empty areas there are, allowing for "edges" to appear.
            static int getEdgeScore(int x, int y)
            {
                int edgeScore = 0;
                for (int dx = x - 6; dx <= x + 6; dx++)
                {
                    if (dx == x)
                        continue;

                    if (!CalamityUtils.ParanoidTileRetrieval(dx, y).HasTile)
                        edgeScore++;
                }
                for (int dy = y - 6; dy <= y + 6; dy++)
                {
                    if (dy == y)
                        continue;

                    if (!CalamityUtils.ParanoidTileRetrieval(x, dy).HasTile)
                        edgeScore++;
                }
                return edgeScore;
            }

            for (int i = layer1Area.Left; i < layer1Area.Right; i++)
            {
                for (int y = layer1Area.Top; y <= layer1Area.Bottom; y++)
                {
                    int x = GetActualX(i);
                    float sulphurousConvertChance = FractalBrownianMotion(i * SandstoneEdgeNoiseMagnification, y * SandstoneEdgeNoiseMagnification, sandstoneSeed, 7) * 0.5f + 0.5f;

                    // Make the sandstone appearance chance dependant on the edge score.
                    sulphurousConvertChance *= Utils.GetLerpValue(4f, 11f, getEdgeScore(x, y), true);
                    
                    if (WorldGen.genRand.NextFloat() > sulphurousConvertChance || sulphurousConvertChance < 0.5f)
                        continue;

                    // Convert to sulphuric gravel as necessary.
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (WorldGen.InWorld(x + dx, y + dy))
                            {
                                if (CalamityUtils.ParanoidTileRetrieval(x + dx, y + dy).TileType == abyssGravelID)
                                {
                                    Main.tile[x + dx, y + dy].TileType = sulphuricGravelID;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GenerateLayer2()
        {
            int trenchCount = Layer2TrenchCount;
            int topOfLayer2 = Layer2Top - 30;
            int bottomOfLayer2 = Layer3Top;
            int width = MaxAbyssWidth - WallThickness;

            // Generate a bunch of preset trenches that reach down to the bottom of the layer. They are mostly vertical, but can wind a bit, and are filled with bioluminescent plants.
            for (int i = 0; i < trenchCount; i++)
            {
                int trenchX = (int)MathHelper.Lerp(48f, width - 108f, i / (float)(trenchCount - 1f)) + WorldGen.genRand.Next(-20, 20);
                int trenchY = topOfLayer2 - WorldGen.genRand.Next(8);
                GenerateLayer2Trench(new(GetActualX(trenchX), trenchY), bottomOfLayer2 + 4);
            }
        }

        public static void GenerateLayer2Trench(Point start, int cutOffPoint)
        {
            Point currentPoint = start;
            ushort voidstoneID = (ushort)ModContent.TileType<Voidstone>();
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();

            // Descend downward, carving out gravel.
            int startingWidth = WorldGen.genRand.Next(MinStartingTrenchWidth, MaxStartingTrenchWidth);
            int endingWidth = WorldGen.genRand.Next(MinEndingTrenchWidth, MaxEndingTrenchWidth);
            int offsetSeed = WorldGen.genRand.Next();
            int widthSeed = WorldGen.genRand.Next();
            while (currentPoint.Y < cutOffPoint)
            {
                float yCompletion = Utils.GetLerpValue(start.Y, cutOffPoint, currentPoint.Y, true);
                float noiseWidthOffset = FractalBrownianMotion(currentPoint.X * TrenchWidthNoiseMagnificationFactor, currentPoint.Y * TrenchWidthNoiseMagnificationFactor, widthSeed, 5) * endingWidth * 0.2f;
                int width = (int)(MathHelper.Lerp(startingWidth, endingWidth, (float)Math.Pow(yCompletion, TrenchTightnessFactor)) + noiseWidthOffset);
                width = Utils.Clamp(width, startingWidth, endingWidth);

                // Calculate the horizontal offset of the current trench.
                int currentOffset = (int)(FractalBrownianMotion(currentPoint.X * TrenchOffsetNoiseMagnificationFactor, currentPoint.Y * TrenchOffsetNoiseMagnificationFactor, offsetSeed, 5) * MaxTrenchOffset);

                // Occasionally carve out lumenyl and voidstone shells at the edges of the current point.
                if (WorldGen.genRand.NextBool(50) && currentPoint.Y < cutOffPoint - width - 8 && currentPoint.Y >= start.Y + 30)
                {
                    int shellRadius = width + WorldGen.genRand.Next(4);
                    Point voidstoneShellCenter = new(currentPoint.X + currentOffset + WorldGen.genRand.NextBool().ToDirectionInt() * width / 2 + WorldGen.genRand.Next(-4, 4), currentPoint.Y + shellRadius + 1);
                    WorldUtils.Gen(voidstoneShellCenter, new Shapes.Circle(shellRadius), Actions.Chain(new GenAction[]
                    {
                        new Modifiers.Blotches(),
                        new Actions.SetTile(voidstoneID),
                        new Actions.PlaceWall(voidstoneWallID),
                    }));

                    // Carve out the inside of the shell.
                    WorldUtils.Gen(voidstoneShellCenter, new Shapes.Circle(shellRadius - 2), Actions.Chain(new GenAction[]
                    {
                        new Actions.ClearTile(true),
                        new Actions.PlaceWall(voidstoneWallID),
                        new Actions.SetLiquid()
                    }));
                }

                for (int dx = -width / 2; dx < width / 2; dx++)
                    ResetToWater(new(currentPoint.X + dx + currentOffset, currentPoint.Y));

                currentPoint.Y++;
            }
        }

        public static void GenerateLayer4()
        {
            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;
            int entireAbyssTop = AbyssTop;
            int top = Layer4Top;
            int bottom = AbyssBottom;
            float topOffsetPhaseShift = WorldGen.genRand.NextFloat(MathHelper.TwoPi);
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();

            for (int i = 1; i < maxWidth; i++)
            {
                int x = GetActualX(i);
                float xCompletion = i / (float)maxWidth;
                int yOffset = (int)(CalamityUtils.Convert01To010(xCompletion) * (float)Math.Sin(MathHelper.Pi * xCompletion + topOffsetPhaseShift * 6f) * 20f);
                for (int y = top - yOffset; y < bottom - WallThickness + yOffset / 3; y++)
                {
                    // Decide whether to cut off due to a Y point being far enough.
                    float yCompletion = Utils.GetLerpValue(entireAbyssTop, bottom - 1f, y, true);
                    if (i >= GetWidth(yCompletion, minWidth, maxWidth) - WallThickness)
                        continue;

                    // Otherwise, clear out water.
                    Main.tile[x, y].WallType = voidstoneWallID;
                    ResetToWater(new(x, y));
                }
            }
        }
        #endregion Generation Functions

        #region Utilities

        public static int GetWidth(float yCompletion, int minWidth, int maxWidth)
        {
            return (int)MathHelper.Lerp(minWidth, maxWidth, (float)Math.Pow(yCompletion * Utils.GetLerpValue(1f, 0.81f, yCompletion, true), 0.13));
        }

        public static void ClearOutStrayTiles(Rectangle area)
        {
            int width = BiomeWidth;
            int depth = BlockDepth;
            List<ushort> blockTileTypes = new()
            {
                (ushort)ModContent.TileType<AbyssGravel>(),
                (ushort)ModContent.TileType<Voidstone>(),
            };

            void getAttachedPoints(int x, int y, List<Point> points)
            {
                Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                Point p = new(x, y);
                if (!blockTileTypes.Contains(t.TileType) || !t.HasTile || points.Count > 432 || points.Contains(p))
                    return;

                points.Add(p);

                getAttachedPoints(x + 1, y, points);
                getAttachedPoints(x - 1, y, points);
                getAttachedPoints(x, y + 1, points);
                getAttachedPoints(x, y - 1, points);
            }

            // Clear out stray chunks created by caverns.
            for (int i = area.Left; i < area.Right; i++)
            {
                int x = GetActualX(i);
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    List<Point> chunkPoints = new();
                    getAttachedPoints(x, y, chunkPoints);

                    if (chunkPoints.Count is >= 2 and < 432)
                    {
                        foreach (Point p in chunkPoints)
                            ResetToWater(p);
                    }

                    // Clear any tiles that have no nearby tiles.
                    if (!CalamityUtils.ParanoidTileRetrieval(x - 1, y).HasTile &&
                        !CalamityUtils.ParanoidTileRetrieval(x + 1, y).HasTile &&
                        !CalamityUtils.ParanoidTileRetrieval(x, y - 1).HasTile &&
                        !CalamityUtils.ParanoidTileRetrieval(x, y + 1).HasTile)
                    {
                        ResetToWater(new(x, y));
                    }
                }
            }
        }

        public static void ResetToWater(Point p)
        {
            Main.tile[p].Get<TileWallWireStateData>().HasTile = false;
            Main.tile[p].Get<LiquidData>().LiquidType = LiquidID.Water;
            Main.tile[p].LiquidAmount = 255;

            if (p.X >= 5 && p.Y < Main.maxTilesX - 5)
                Tile.SmoothSlope(p.X, p.Y);
        }
        #endregion Utilities
    }
}