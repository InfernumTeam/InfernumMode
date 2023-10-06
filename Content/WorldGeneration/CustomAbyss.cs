using CalamityMod;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Abyss.AbyssAmbient;
using CalamityMod.Tiles.Ores;
using CalamityMod.Walls;
using CalamityMod.World;
using InfernumMode.Common.Worldgen;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using static CalamityMod.World.SulphurousSea;

namespace InfernumMode.Content.WorldGeneration
{
    public static class CustomAbyss
    {
        #region Fields and Properties

        // Loop variables that are accessed via getter methods should be stored externally in local variables for performance reasons.
        public static int MinAbyssWidth => MaxAbyssWidth / 5;

        public static int MaxAbyssWidth => BiomeWidth + 130;

        public static int AbyssTop => YStart + BlockDepth - 44;

        public static int Layer2Top => (int)(Main.rockLayer + Main.maxTilesY * 0.084f);

        public static int Layer3Top => (int)(Main.rockLayer + Main.maxTilesY * 0.184f);

        public static int Layer4Top => (int)(Main.rockLayer + Main.maxTilesY * 0.33f);

        public static ref int AbyssBottom => ref Abyss.AbyssChasmBottom;

        // 0-1 value that determines the threshold for layer 1 spaghetti caves being carved out. At 0, no tiles are carved out, at 1, all tiles are carved out.
        // This is used in the formula 'abs(noise(x, y)) < r' to determine whether the cave should remove tiles.
        public static readonly float[] Layer1SpaghettiCaveCarveOutThresholds = new float[]
        {
            0.0396f,
            0.0506f,
            0.0521f
        };

        public const int Layer1SmallPlantCreationChance = 6;

        public const float Layer1ForestNoiseMagnificationFactor = 0.00181f;

        public const float Layer1ForestMinNoiseValue = 0.235f;

        public const int Layer1KelpCreationChance = 18;

        public static int Layer2TrenchCount => (int)Math.Sqrt(Main.maxTilesX / 176f);

        public const int MinStartingTrenchWidth = 5;

        public const int MaxStartingTrenchWidth = 8;

        public const int MinEndingTrenchWidth = 20;

        public const int MaxEndingTrenchWidth = 27;

        public const float TrenchTightnessFactor = 1.72f;

        public const float TrenchWidthNoiseMagnificationFactor = 0.00292f;

        public const float TrenchOffsetNoiseMagnificationFactor = 0.00261f;

        public const int MaxTrenchOffset = 28;

        public const int Layer2WildlifeSpawnAttempts = 35200;

        public const int Layer3CaveCarveoutSteps = 124;

        public const int MinLayer3CaveSize = 9;

        public const int MaxLayer3CaveSize = 16;

        public static readonly float[] Layer3SpaghettiCaveCarveOutThresholds = new float[]
        {
            0.1389f
        };

        public const float Layer3CrystalCaveMagnificationFactor = 0.00109f;

        public const float CrystalCaveNoiseThreshold = 0.58f;

        public const int Layer3VentCount = 10;

        public const int Layer3SquidDenOuterRadius = 60;

        public const int Layer3SquidDenInnerRadius = Layer3SquidDenOuterRadius - 16;

        public const int EidolistPedestalRadius = 20;

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
            GenerateLayer2(out List<Point> trenchBottoms);
            GenerateLayer3(trenchBottoms, out Point layer4ConvergencePoint);
            GenerateLayer4(layer4ConvergencePoint);
            GenerateVoidstone();
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
                    Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                    Main.tile[x, y].LiquidAmount = 255;
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
            int topOfLayer1 = AbyssTop;
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
                        noise = Lerp(noise, Math.Sign(noise), biasAwayFrom0Interpolant);

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
            GenerateLayer1SulphurousShale(layer1Area);

            // Generate scenic tiles.
            GeneratePostSkeletronTiles(layer1Area);
            GenerateAbyssalKelp(layer1Area);

            // Create random blobs of planty mush.
            GeneratePlantyMushClumps(layer1Area);

            GenerateScenicTilesInArea(layer1Area, 10, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<AbyssGiantKelp1>(),
                (ushort)ModContent.TileType<AbyssGiantKelp2>(),
                (ushort)ModContent.TileType<AbyssGiantKelp3>(),
                (ushort)ModContent.TileType<AbyssGiantKelp4>()
            });
            GenerateScenicTilesInArea(layer1Area, 9, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<AbyssVent1>(),
                (ushort)ModContent.TileType<AbyssVent2>(),
                (ushort)ModContent.TileType<AbyssVent3>()
            });
            GenerateScenicTilesInArea(layer1Area, 7, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<GravelPile1>(),
                (ushort)ModContent.TileType<GravelPile2>(),
                (ushort)ModContent.TileType<GravelPile3>()
            });
            GenerateScenicTilesInArea(layer1Area, 20, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<PirateCrate1>(),
                (ushort)ModContent.TileType<PirateCrate2>(),
                (ushort)ModContent.TileType<PirateCrate3>(),
                (ushort)ModContent.TileType<PirateCrate4>(),
                (ushort)ModContent.TileType<PirateCrate5>(),
                (ushort)ModContent.TileType<PirateCrate6>()
            });
            GenerateScenicTilesInArea(layer1Area, 8, 1, new ushort[] { (ushort)ModContent.TileType<SulphurousShale>() }, new ushort[]
            {
                (ushort)ModContent.TileType<ShalePile1>(),
                (ushort)ModContent.TileType<ShalePile2>(),
                (ushort)ModContent.TileType<ShalePile3>()
            });
        }

        public static void GenerateLayer1SulphurousShale(Rectangle layer1Area)
        {
            int sandstoneSeed = WorldGen.genRand.Next();
            WorldSaveSystem.AbyssLayer1ForestSeed = WorldGen.genRand.Next();

            ushort abyssGravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort sulphuricShaleID = (ushort)ModContent.TileType<SulphurousShale>();

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

                    if (WorldGen.genRand.NextFloat() > sulphurousConvertChance || sulphurousConvertChance < 0.57f)
                        continue;

                    // Convert to sulphuric shale as necessary.
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (WorldGen.InWorld(x + dx, y + dy))
                            {
                                if (CalamityUtils.ParanoidTileRetrieval(x + dx, y + dy).TileType == abyssGravelID)
                                {
                                    Main.tile[x + dx, y + dy].TileType = sulphuricShaleID;

                                    // Encourage the growth of ground vines.
                                    if (InsideOfLayer1Forest(new(x + dx, y + dy)))
                                    {
                                        int vineHeight = WorldGen.genRand.Next(9, 12);
                                        Point vinePosition = new(x + dx, y + dy);

                                        for (int ddy = 0; ddy < vineHeight; ddy++)
                                        {
                                            if (ddy <= 0)
                                                TileLoader.RandomUpdate(vinePosition.X, vinePosition.Y - ddy, CalamityUtils.ParanoidTileRetrieval(vinePosition.X, vinePosition.Y - ddy).TileType);
                                            else
                                                SulphurousGroundVines.AttemptToGrowVine(new(vinePosition.X, vinePosition.Y - ddy));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GenerateAbyssalKelp(Rectangle layer1Area)
        {
            ushort kelpID = (ushort)ModContent.TileType<AbyssalKelp>();
            for (int i = layer1Area.Left; i < layer1Area.Right; i++)
            {
                for (int y = layer1Area.Top; y <= layer1Area.Bottom; y++)
                {
                    int x = GetActualX(i);
                    Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                    Tile above = CalamityUtils.ParanoidTileRetrieval(x, y - 1);

                    // Randomly create kelp upward.
                    if (WorldGen.SolidTile(t) && !above.HasTile && WorldGen.genRand.NextBool(Layer1KelpCreationChance / (InsideOfLayer1Forest(new(x, y)) ? 4 : 1)))
                    {
                        int kelpHeight = WorldGen.genRand.Next(6, 12);
                        bool areaIsOccupied = false;

                        // Check if the area where the kelp would be created is occupied.
                        for (int dy = 0; dy < kelpHeight + 4; dy++)
                        {
                            if (CalamityUtils.ParanoidTileRetrieval(x, y - dy - 1).HasTile)
                            {
                                areaIsOccupied = true;
                                break;
                            }
                        }

                        if (areaIsOccupied)
                            continue;

                        for (int dy = 0; dy < kelpHeight; dy++)
                        {
                            Main.tile[x, y - dy - 1].TileType = kelpID;
                            Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().TileFrameX = 0;

                            if (dy == 0)
                                Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().TileFrameY = 72;
                            else if (dy == kelpHeight - 1)
                                Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().TileFrameY = 0;
                            else
                                Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().TileFrameY = (short)(WorldGen.genRand.Next(1, 4) * 18);

                            Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().IsHalfBlock = false;
                            Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                            Main.tile[x, y - dy - 1].Get<TileWallWireStateData>().HasTile = true;
                        }
                    }
                }
            }
        }

        public static void GeneratePlantyMushClumps(Rectangle layer1Area)
        {
            for (int i = 0; i < 18; i++)
            {
                int x = GetActualX(WorldGen.genRand.Next(layer1Area.Left + 90, layer1Area.Right - 70));
                int y = WorldGen.genRand.Next(layer1Area.Top + 40, layer1Area.Bottom - 15);
                Point p = new(x, y);

                // Make the surrounding pyre molten.
                WorldUtils.Gen(p, new Shapes.Circle(WorldGen.genRand.Next(14, 18)), Actions.Chain(new GenAction[]
                {
                    new Modifiers.RadialDither(12f, 18f),
                    new Modifiers.Conditions(new Conditions.IsTile((ushort)ModContent.TileType<AbyssGravel>(), (ushort)ModContent.TileType<SulphurousShale>())),
                    new Actions.SetTile((ushort)ModContent.TileType<PlantyMush>()),
                    new Actions.PlaceWall((ushort)ModContent.WallType<AbyssGravelWall>()),
                }));
            }
        }

        public static void GeneratePostSkeletronTiles(Rectangle layer1Area)
        {
            Dictionary<int, Point> tilesLeft = new()
            {
                [ModContent.TileType<IronBootsSkeleton>()] = new(3, 2),
                [ModContent.TileType<StrangeOrbTile>()] = new(2, 2)
            };

            for (int i = 0; i < 72000; i++)
            {
                if (tilesLeft.Count <= 0)
                    break;

                int x = GetActualX(WorldGen.genRand.Next(layer1Area.Left + 72, layer1Area.Right - 72));
                int y = WorldGen.genRand.Next(layer1Area.Top + 48, layer1Area.Bottom - 40);

                bool areaIsOccupied = false;
                bool solidGround = true;

                // Check if the area where the kelp would be created is occupied.
                for (int dx = 0; dx < 5; dx++)
                {
                    for (int dy = 0; dy < 3; dy++)
                    {
                        if (CalamityUtils.ParanoidTileRetrieval(x + dx, y - dy).HasTile)
                        {
                            areaIsOccupied = true;
                            break;
                        }
                    }

                    if (!WorldGen.SolidTile(CalamityUtils.ParanoidTileRetrieval(x + dx, y + 1)) || !CalamityUtils.ParanoidTileRetrieval(x + dx, y + 1).HasTile)
                        solidGround = false;
                }

                if (areaIsOccupied || !solidGround)
                    continue;

                int tileID = tilesLeft.Last().Key;
                for (int dx = 0; dx < tilesLeft[tileID].X; dx++)
                {
                    for (int dy = 0; dy < tilesLeft[tileID].Y; dy++)
                    {
                        Main.tile[x + dx, y - dy].TileType = (ushort)tileID;
                        Main.tile[x + dx, y - dy].Get<TileWallWireStateData>().TileFrameX = (short)(dx * 18);
                        Main.tile[x + dx, y - dy].Get<TileWallWireStateData>().TileFrameY = (short)((tilesLeft[tileID].Y - dy) * 18 - 18);
                        Main.tile[x + dx, y - dy].Get<TileWallWireStateData>().IsHalfBlock = false;
                        Main.tile[x + dx, y - dy].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                        Main.tile[x + dx, y - dy].Get<TileWallWireStateData>().HasTile = true;
                    }
                }

                tilesLeft.Remove(tileID);
            }
        }

        public static void GenerateLayer2(out List<Point> trenchBottoms)
        {
            int trenchCount = Layer2TrenchCount;
            int topOfLayer2 = Layer2Top - 30;
            int bottomOfLayer2 = Layer3Top;
            int maxWidth = MaxAbyssWidth - WallThickness;

            // Initialize the trench list.
            List<Point> trenchTops = new();
            trenchBottoms = new();

            // Generate a bunch of preset trenches that reach down to the bottom of the layer. They are mostly vertical, but can wind a bit, and are filled with bioluminescent plants.
            for (int i = 0; i < trenchCount; i++)
            {
                int trenchX = (int)Lerp(104f, maxWidth * 0.7f, i / (float)(trenchCount - 1f)) + WorldGen.genRand.Next(-15, 15);
                int trenchY = topOfLayer2 - WorldGen.genRand.Next(25, 35);
                trenchTops.Add(new(GetActualX(trenchX), trenchY));
                trenchBottoms.Add(GenerateLayer2Trench(new(GetActualX(trenchX), trenchY), bottomOfLayer2 + 4));
            }

            // Create a connecting cavern between the left and rightmost trenches.
            GenerateLayer2ConnectingCavern(trenchTops.First(), trenchBottoms.Last());

            Rectangle layer2Area = new(1, Layer2Top, maxWidth, Layer3Top - Layer2Top);
            GenerateLayer2Wildlife(layer2Area);
        }

        public static Point GenerateLayer2Trench(Point start, int cutOffPoint)
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
                int width = (int)(Lerp(startingWidth, endingWidth, Pow(yCompletion, TrenchTightnessFactor)) + noiseWidthOffset);
                width = Utils.Clamp(width, startingWidth, endingWidth);

                // Calculate the horizontal offset of the current trench.
                int currentOffset = (int)(FractalBrownianMotion(currentPoint.X * TrenchOffsetNoiseMagnificationFactor, currentPoint.Y * TrenchOffsetNoiseMagnificationFactor, offsetSeed, 5) * MaxTrenchOffset);

                // Occasionally carve out lumenyl and voidstone shells at the edges of the current point.
                if (WorldGen.genRand.NextBool(50) && currentPoint.Y < cutOffPoint - width - 8 && currentPoint.Y >= start.Y + 30)
                {
                    int shellRadius = width + WorldGen.genRand.Next(4);
                    Point voidstoneShellCenter = new(currentPoint.X + currentOffset + WorldGen.genRand.NextBool().ToDirectionInt() * width / 2 + WorldGen.genRand.Next(-4, 4), currentPoint.Y + shellRadius + 1);
                    voidstoneShellCenter.X = Utils.Clamp(voidstoneShellCenter.X, 35, Main.maxTilesX - 35);

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

            // Return the end position of the trench. This is used by the layer 3 gen to determine where caves should begin being carved out.
            return currentPoint;
        }

        public static void GenerateLayer2ConnectingCavern(Point topLeft, Point bottomRight)
        {
            topLeft.Y += 24;
            bottomRight.Y -= 24;

            // Maintain coordinate consistency such that the top left point is actually to the left.
            if (topLeft.X > bottomRight.X)
                Utils.Swap(ref topLeft, ref bottomRight);

            // Generate a ragged cavern between the trenches.
            int width = WorldGen.genRand.Next(MinEndingTrenchWidth, MaxEndingTrenchWidth) / 3;
            int offsetSeed = WorldGen.genRand.Next();
            Point currentPoint = topLeft;

            while (!currentPoint.ToVector2().WithinRange(bottomRight.ToVector2(), 18f))
            {
                // Calculate the horizontal offset of the current trench.
                int currentOffset = (int)(FractalBrownianMotion(currentPoint.X * TrenchOffsetNoiseMagnificationFactor * 3f, currentPoint.Y * TrenchOffsetNoiseMagnificationFactor * 3f, offsetSeed, 5) * MaxTrenchOffset * 0.67f);

                for (int dx = -width / 2; dx < width / 2; dx++)
                {
                    for (int dy = 0; dy < 3; dy++)
                        ResetToWater(new(currentPoint.X + dx + currentOffset, currentPoint.Y + dy));
                }

                currentPoint = (currentPoint.ToVector2() + (bottomRight.ToVector2() - currentPoint.ToVector2()).SafeNormalize(Vector2.UnitY) * 2f).ToPoint();
            }
        }

        public static void GenerateLayer2Wildlife(Rectangle area)
        {
            List<int> wildlifeVariants = new()
            {
                ModContent.TileType<AbyssalCoral>(),
                ModContent.TileType<FluorescentPolyp>(),
                ModContent.TileType<HadalSeagrass>(),
                ModContent.TileType<LumenylPolyp>(),
            };

            for (int i = 0; i < Layer2WildlifeSpawnAttempts; i++)
            {
                Point potentialPosition = new(GetActualX(WorldGen.genRand.Next(area.Left + 30, area.Right - 30)), WorldGen.genRand.Next(area.Top, area.Bottom));
                WorldGen.PlaceTile(potentialPosition.X, potentialPosition.Y, WorldGen.genRand.Next(wildlifeVariants));
            }

            GenerateScenicTilesInArea(area, 10, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<AbyssGiantKelp1>(),
                (ushort)ModContent.TileType<AbyssGiantKelp2>(),
                (ushort)ModContent.TileType<AbyssGiantKelp3>(),
                (ushort)ModContent.TileType<AbyssGiantKelp4>()
            });
            GenerateScenicTilesInArea(area, 9, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<AbyssVent1>(),
                (ushort)ModContent.TileType<AbyssVent2>(),
                (ushort)ModContent.TileType<AbyssVent3>()
            });
            GenerateScenicTilesInArea(area, 7, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<BulbTree1>(),
                (ushort)ModContent.TileType<BulbTree2>(),
                (ushort)ModContent.TileType<BulbTree3>()
            });
            GenerateScenicTilesInArea(area, 7, 1, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<GravelPile1>(),
                (ushort)ModContent.TileType<GravelPile2>(),
                (ushort)ModContent.TileType<GravelPile3>()
            });
            GenerateScenicTilesInArea(area, 10, 5, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<TenebrisRemnant>()
            });
            GenerateScenicTilesInArea(area, 5, 16, new ushort[] { (ushort)ModContent.TileType<AbyssGravel>() }, new ushort[]
            {
                (ushort)ModContent.TileType<AbyssalKelp>()
            });
        }

        public static void GenerateLayer3(List<Point> trenchBottoms, out Point layer4ConvergencePoint)
        {
            int entireAbyssTop = AbyssTop;
            int entireAbyssBottom = AbyssBottom;
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();
            layer4ConvergencePoint = new(GetActualX((MaxAbyssWidth - WallThickness) / 2), Layer4Top + 5);
            List<int> caveSeeds = new();
            List<Vector2> caveNoisePositions = new();
            List<Point> caveEndPoints = new();

            // Initialize cave positions.
            for (int i = 0; i < trenchBottoms.Count; i++)
            {
                caveSeeds.Add(WorldGen.genRand.Next());
                caveNoisePositions.Add(WorldGen.genRand.NextVector2Unit());
                caveEndPoints.Add(trenchBottoms[i]);
            }

            // Carve out the base caves.
            for (int i = 0; i < trenchBottoms.Count; i++)
            {
                for (int j = 0; j < 196; j++)
                {
                    int carveOutArea = (int)Utils.Remap(caveNoisePositions[i].Y, Layer3Top, Layer4Top, MinLayer3CaveSize, MaxLayer3CaveSize);

                    // Slightly update the coordinates of the input value, in "noise space".
                    // This causes the worm's shape to be slightly different in the next frame.
                    // The x coordinate of the input value is shifted in a negative direction,
                    // which propagates the previous Perlin-noise values over to subsequent
                    // segments.  This produces a "slithering" effect.
                    caveNoisePositions[i] += new Vector2(-carveOutArea * 0.0002f, 0.0033f);

                    // Make caves converge towards a central pit the closer they are to reaching the 4th layer.
                    float convergenceInterpolant = Utils.GetLerpValue(Layer4Top - 75f, Layer4Top - 8f, caveEndPoints[i].Y, true);

                    // Make caves stay within the abyss.
                    float moveToEdgeInterpolant;
                    Vector2 edgeDirection;
                    if (Abyss.AtLeftSideOfWorld)
                    {
                        edgeDirection = -Vector2.UnitX;
                        moveToEdgeInterpolant = Utils.GetLerpValue(MaxAbyssWidth - WallThickness - 32f, MaxAbyssWidth - WallThickness, caveEndPoints[i].X, true);
                    }
                    else
                    {
                        edgeDirection = Vector2.UnitX;
                        moveToEdgeInterpolant = Utils.GetLerpValue(Main.maxTilesX - (MaxAbyssWidth - WallThickness), Main.maxTilesX - (MaxAbyssWidth - WallThickness - 32f), caveEndPoints[i].X, true);
                    }

                    // Make caves stay within layer 3.
                    float moveDownwardInterpolant = Utils.GetLerpValue(Layer3Top + 50f, Layer3Top + 20f, caveEndPoints[i].Y, true);

                    Vector2 directionToConvergencePoint = (layer4ConvergencePoint.ToVector2() - caveEndPoints[i].ToVector2()).SafeNormalize(Vector2.UnitY);
                    Vector2 caveMoveDirection = (TwoPi * FractalBrownianMotion(caveNoisePositions[i].X, caveNoisePositions[i].Y, caveSeeds[i], 5)).ToRotationVector2();
                    caveMoveDirection = Vector2.Lerp(caveMoveDirection, edgeDirection, moveToEdgeInterpolant);
                    caveMoveDirection = Vector2.Lerp(caveMoveDirection, Vector2.UnitY, moveDownwardInterpolant);
                    caveMoveDirection = Vector2.Lerp(caveMoveDirection, directionToConvergencePoint, convergenceInterpolant).SafeNormalize(Vector2.Zero);

                    Vector2 caveMoveOffset = caveMoveDirection * carveOutArea * 0.333f;
                    caveEndPoints[i] += caveMoveOffset.ToPoint();
                    caveEndPoints[i] = new(Utils.Clamp(caveEndPoints[i].X, 45, Main.maxTilesX - 45), caveEndPoints[i].Y);

                    WorldUtils.Gen(caveEndPoints[i], new Shapes.Circle(carveOutArea), Actions.Chain(new GenAction[]
                    {
                        new Actions.ClearTile(),
                        new Actions.PlaceWall(voidstoneWallID),
                        new Actions.SetLiquid(),
                        new Actions.Smooth()
                    }));
                }
            }

            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;

            // Carve out finer, spaghetti caves.
            for (int c = 0; c < Layer3SpaghettiCaveCarveOutThresholds.Length; c++)
            {
                int caveSeed = WorldGen.genRand.Next();
                for (int y = Layer3Top; y < Layer4Top - 14; y++)
                {
                    float yCompletion = Utils.GetLerpValue(entireAbyssTop, entireAbyssBottom, y, true);
                    int width = GetWidth(yCompletion, minWidth, maxWidth) - WallThickness;
                    for (int i = 2; i < width; i++)
                    {
                        // Initialize variables for the cave.
                        int x = GetActualX(i);
                        float noise = FractalBrownianMotion(i * SpaghettiCaveMagnification, y * SpaghettiCaveMagnification, caveSeed, 3);

                        // Bias noise away from 0, effectively making caves less likely to appear, based on how close it is to the edges and bottom.
                        float biasAwayFrom0Interpolant = Utils.GetLerpValue(width - 24f, width - 9f, i, true) * 0.4f;
                        biasAwayFrom0Interpolant += Utils.GetLerpValue(Layer4Top - 24f, Layer4Top - 10f, y, true) * 0.4f;

                        // If the noise is less than 0, bias to -1, if it's greater than 0, bias away to 1.
                        // This is done instead of biasing to -1 or 1 without exception to ensure that in doing so the noise does not cross into the
                        // cutout threshold near 0 as it interpolates.
                        noise = Lerp(noise, Math.Sign(noise), biasAwayFrom0Interpolant);

                        if (Math.Abs(noise) < Layer3SpaghettiCaveCarveOutThresholds[c])
                        {
                            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                            Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                            Main.tile[x, y].WallType = voidstoneWallID;
                            Main.tile[x, y].LiquidAmount = 255;
                            Tile.SmoothSlope(x, y);
                        }
                    }
                }
            }

            // Carve out a large area at the layer 4 entrance.
            WorldUtils.Gen(layer4ConvergencePoint, new Shapes.Circle(72), Actions.Chain(new GenAction[]
            {
                new Actions.ClearTile(),
                new Actions.PlaceWall(voidstoneWallID),
                new Actions.SetLiquid(),
                new Actions.Smooth()
            }));

            // Clear out any stray tiles created by the cave generation.
            Rectangle layer3Area = new(1, Layer3Top, maxWidth - WallThickness, Layer4Top - Layer3Top);
            ClearOutStrayTiles(layer3Area);

            WorldSaveSystem.AbyssLayer3CavernSeed = WorldGen.genRand.Next();

            // Generate pyre mantle in the hydrothermic zone.
            GenerateLayer3PyreMantle(layer3Area);

            // Generate scenic hydrothermal vents.
            GenerateLayer3Vents(layer3Area);

            // Scatter crystals. This encompasses the creation of the lumenyl zone.
            GenerateLayer3LumenylCrystals(layer3Area);

            // Generate the squid den.
            GenerateLayer3SquidDen(layer3Area);

            // Create scenic tiles.
            GenerateScenicTilesInArea(layer3Area, 12, 1, new ushort[] { (ushort)ModContent.TileType<PyreMantle>(), (ushort)ModContent.TileType<PyreMantleMolten>() }, new ushort[]
            {
                (ushort)ModContent.TileType<SpiderCoral1>(),
                (ushort)ModContent.TileType<SpiderCoral2>(),
                (ushort)ModContent.TileType<SpiderCoral3>(),
                (ushort)ModContent.TileType<SpiderCoral4>(),
                (ushort)ModContent.TileType<SpiderCoral5>()
            });
            GenerateScenicTilesInArea(layer3Area, 14, 1, new ushort[] { (ushort)ModContent.TileType<PyreMantle>(), (ushort)ModContent.TileType<PyreMantleMolten>() }, new ushort[]
            {
                (ushort)ModContent.TileType<ThermalVent1>(),
                (ushort)ModContent.TileType<ThermalVent2>(),
                (ushort)ModContent.TileType<ThermalVent3>()
            });
        }

        public static void GenerateLayer3Vents(Rectangle area)
        {
            ushort gravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort scoriaOre = (ushort)ModContent.TileType<ScoriaOre>();
            List<Point> ventPositions = new();

            int tries = 0;
            for (int i = 0; i < Layer3VentCount; i++)
            {
                tries++;
                if (tries >= 20000)
                    break;

                Point potentialVentPosition = new(GetActualX(WorldGen.genRand.Next(area.Left + 30, area.Right - 30)), WorldGen.genRand.Next(area.Top, area.Bottom));
                Tile t = CalamityUtils.ParanoidTileRetrieval(potentialVentPosition.X, potentialVentPosition.Y);

                // Ignore placement positions that are already occupied.
                if (t.HasTile)
                {
                    i--;
                    continue;
                }

                // Ignore positions that are close to an existing vent.
                Point floor = Utilities.GetGroundPositionFrom(potentialVentPosition);
                if (ventPositions.Any(p => p.ToVector2().Distance(floor.ToVector2()) < 24f))
                {
                    i--;
                    continue;
                }

                Point floorLeft = Utilities.GetGroundPositionFrom(new Point(potentialVentPosition.X - 3, potentialVentPosition.Y));
                Point floorRight = Utilities.GetGroundPositionFrom(new Point(potentialVentPosition.X + 3, potentialVentPosition.Y));
                Point ceiling = Utilities.GetGroundPositionFrom(potentialVentPosition, new Searches.Up(9001));

                // Ignore cramped spaces.
                if (floor.Y - ceiling.Y < 10)
                {
                    i--;
                    continue;
                }

                // Ignore steep spaces.
                float averageY = Math.Abs(floorLeft.Y + floor.Y + floorRight.Y) / 3f;
                if (Distance(averageY, floor.Y) >= 4f)
                {
                    i--;
                    continue;
                }

                // Ignore points outside of the hydrothermal zone.
                int zeroBiasedX = floor.X;
                if (zeroBiasedX >= Main.maxTilesX / 2)
                    zeroBiasedX = Main.maxTilesX - zeroBiasedX;

                if (!InsideOfLayer3HydrothermalZone(new(zeroBiasedX, floor.Y)))
                {
                    i--;
                    continue;
                }

                // Make the surrounding pyre molten.
                WorldUtils.Gen(floor, new Shapes.Circle(WorldGen.genRand.Next(15, 22)), Actions.Chain(new GenAction[]
                {
                    new Modifiers.Conditions(new Conditions.IsTile((ushort)ModContent.TileType<PyreMantle>())),
                    new Actions.SetTile((ushort)ModContent.TileType<PyreMantleMolten>()),

                    new Actions.PlaceWall((ushort)ModContent.WallType<PyreMantleWall>())
                }));

                // Generate a stand of scoria.
                int moundHeight = WorldGen.genRand.Next(4, 9);
                int scoriaGroundSize = WorldGen.genRand.Next(5, 7);
                WorldUtils.Gen(new(floor.X, floor.Y + scoriaGroundSize / 2), new Shapes.Slime(scoriaGroundSize), Actions.Chain(new GenAction[]
                {
                    new Actions.SetTile(scoriaOre, true),
                }));
                WorldUtils.Gen(floor, new Shapes.Mound(5, moundHeight), Actions.Chain(new GenAction[]
                {
                    new Actions.SetTile(gravelID, true),
                }));

                ushort ventID = (ushort)WorldGen.genRand.Next(new int[]
                {
                    ModContent.TileType<ThermalVent1>(),
                    ModContent.TileType<ThermalVent2>(),
                    ModContent.TileType<ThermalVent3>()
                });
                if (Distance(Utilities.GetGroundPositionFrom(new Point(floor.X, floor.Y - moundHeight), new Searches.Up(50)).Y, floor.Y - moundHeight) >= 7f)
                    PlaceObjectWithGoddamnForce(floor.X, floor.Y - moundHeight, ventID, 0);

                ventPositions.Add(floor);
            }
        }

        public static void GenerateLayer3LumenylCrystals(Rectangle area)
        {
            TryToGenerateLumenylCrystals(area, 80, false);
            TryToGenerateLumenylCrystals(area, 700, true, (x, y) => InsideOfLayer3LumenylZone(new(x, y)));
            TryToGenerateLumenylCrystals(area, 3000, false, (x, y) => InsideOfLayer3LumenylZone(new(x, y)));
        }

        public static void TryToGenerateLumenylCrystals(Rectangle area, int placementCount, bool largeCrystals, Func<int, int, bool> extraCondition = null)
        {
            ushort lumenylID = (ushort)ModContent.TileType<LumenylCrystals>();
            if (largeCrystals)
                lumenylID = (ushort)ModContent.TileType<LargeLumenylCrystal>();

            var fakeItem = new Item();
            fakeItem.SetDefaults(ItemID.StoneBlock);

            int tries = 0;

            for (int i = 0; i < placementCount; i++)
            {
                // Give up once enough tries have been attempted.
                tries++;
                if (tries >= 32000)
                    break;

                Point potentialCrystalPosition = new(GetActualX(WorldGen.genRand.Next(area.Left + 30, area.Right - 30)), WorldGen.genRand.Next(area.Top, area.Bottom));
                Tile t = CalamityUtils.ParanoidTileRetrieval(potentialCrystalPosition.X, potentialCrystalPosition.Y);

                // Ignore placement positions that are already occupied.
                if (t.HasTile)
                {
                    i--;
                    continue;
                }

                // Ignore placement positions with nothing to attach to.
                Tile left = CalamityUtils.ParanoidTileRetrieval(potentialCrystalPosition.X - 1, potentialCrystalPosition.Y);
                Tile right = CalamityUtils.ParanoidTileRetrieval(potentialCrystalPosition.X + 1, potentialCrystalPosition.Y);
                Tile top = CalamityUtils.ParanoidTileRetrieval(potentialCrystalPosition.X, potentialCrystalPosition.Y - 1);
                Tile bottom = CalamityUtils.ParanoidTileRetrieval(potentialCrystalPosition.X, potentialCrystalPosition.Y + 1);
                if (!left.HasTile && !right.HasTile && !top.HasTile && !bottom.HasTile)
                {
                    i--;
                    continue;
                }
                if (!WorldGen.SolidTile(left) && !WorldGen.SolidTile(right) && !WorldGen.SolidTile(top) && !WorldGen.SolidTile(bottom))
                {
                    i--;
                    continue;
                }

                // Ignore placement positions that violate the extra condition, if it exists.
                if (!extraCondition?.Invoke(potentialCrystalPosition.X, potentialCrystalPosition.Y) ?? false)
                {
                    i--;
                    continue;
                }

                t.TileType = lumenylID;
                t.HasTile = true;
                t.IsHalfBlock = false;
                t.Get<TileWallWireStateData>().Slope = SlopeType.Solid;

                t.TileFrameX = (short)(WorldGen.genRand.Next(18) * 18);

                bool invalidPlacement = false;

                if (bottom.HasTile && Main.tileSolid[bottom.TileType] && bottom.Slope == 0 && !bottom.IsHalfBlock)
                {
                    t.TileFrameY = 0;
                    if (largeCrystals && (CalamityUtils.DistanceToTileCollisionHit(potentialCrystalPosition.ToWorldCoordinates(), -Vector2.UnitY, 25) ?? 500f) < 64f)
                        invalidPlacement = true;
                }
                else if (top.HasTile && Main.tileSolid[top.TileType] && top.Slope == 0 && !top.IsHalfBlock)
                {
                    t.TileFrameY = 18;
                    if (largeCrystals && (CalamityUtils.DistanceToTileCollisionHit(potentialCrystalPosition.ToWorldCoordinates(), Vector2.UnitY, 25) ?? 500f) < 64f)
                        invalidPlacement = true;
                }
                else if (right.HasTile && Main.tileSolid[right.TileType] && right.Slope == 0 && !right.IsHalfBlock)
                {
                    t.TileFrameY = 36;
                    if (largeCrystals && (CalamityUtils.DistanceToTileCollisionHit(potentialCrystalPosition.ToWorldCoordinates(), -Vector2.UnitX, 25) ?? 500f) < 64f)
                        invalidPlacement = true;
                }
                else if (left.HasTile && Main.tileSolid[left.TileType] && left.Slope == 0 && !left.IsHalfBlock)
                {
                    t.TileFrameY = 54;
                    if (largeCrystals && (CalamityUtils.DistanceToTileCollisionHit(potentialCrystalPosition.ToWorldCoordinates(), Vector2.UnitX, 25) ?? 500f) < 64f)
                        invalidPlacement = true;
                }

                if (invalidPlacement)
                {
                    i--;
                    t.HasTile = false;
                }
            }
        }

        public static void GenerateLayer3PyreMantle(Rectangle area)
        {
            int top = Layer3Top - 10;
            int wallSeed = WorldGen.genRand.Next();
            ushort gravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort pyreID = (ushort)ModContent.TileType<PyreMantle>();
            ushort pyreWallID = (ushort)ModContent.WallType<PyreMantleWall>();
            FastRandom rng = new(WorldGen.genRand.Next());

            for (int i = area.Left; i < area.Right + 36; i++)
            {
                int x = GetActualX(i);
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    if (Main.tile[x, y].TileType != gravelID || !Main.tile[x, y].HasTile)
                        continue;

                    if (!InsideOfLayer3HydrothermalZone(new(i, y)))
                        continue;

                    float ditherChance = Utils.GetLerpValue(top, top + 50f, y, true) * Utils.GetLerpValue(area.Right - 25f, area.Right - 60f, i, true);

                    // Perform dithering.
                    if (rng.NextFloat() > ditherChance)
                        continue;

                    Main.tile[x, y].TileType = pyreID;

                    if (FractalBrownianMotion(x * Layer3CrystalCaveMagnificationFactor * 4f, y * Layer3CrystalCaveMagnificationFactor * 4f, wallSeed, 5) >= 0f)
                        Main.tile[x, y].WallType = pyreWallID;
                }
            }
        }

        public static void GenerateLayer3SquidDen(Rectangle area)
        {
            ushort voidstoneID = (ushort)ModContent.TileType<Voidstone>();
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();

            // Generate a voidstone circle for the den and then cut out most of its insides to create a shell.
            WorldSaveSystem.SquidDenCenter = new(GetActualX(area.Left + Layer3SquidDenOuterRadius + 15), area.Top + Layer3SquidDenOuterRadius);
            WorldUtils.Gen(WorldSaveSystem.SquidDenCenter, new Shapes.Circle(Layer3SquidDenOuterRadius), Actions.Chain(new GenAction[]
            {
                new Modifiers.Blotches(3),
                new Actions.SetTile(voidstoneID),
                new Actions.PlaceWall(voidstoneWallID)
            }));
            WorldUtils.Gen(WorldSaveSystem.SquidDenCenter, new Shapes.Circle(Layer3SquidDenInnerRadius), Actions.Chain(new GenAction[]
            {
                new Modifiers.Blotches(4),
                new Actions.ClearTile(),
                new Modifiers.Blotches(4),
                new Actions.SetLiquid()
            }));

            // Carve out caves that cut through the shell.
            Point caveStart = new(WorldSaveSystem.SquidDenCenter.X + Abyss.AtLeftSideOfWorld.ToDirectionInt() * Layer3SquidDenOuterRadius, WorldSaveSystem.SquidDenCenter.Y + Layer3SquidDenOuterRadius);
            for (int i = 0; i < 50; i++)
            {
                float cavePerpendicularAngle = Sin(i * 0.39f);
                Point currentCavePosition = Vector2.Lerp(caveStart.ToVector2(), WorldSaveSystem.SquidDenCenter.ToVector2(), i / 49f).ToPoint();
                Vector2 directionToCenter = (WorldSaveSystem.SquidDenCenter.ToVector2() - caveStart.ToVector2()).SafeNormalize(Vector2.Zero);
                currentCavePosition += (directionToCenter.RotatedBy(cavePerpendicularAngle) * 8f).ToPoint();

                WorldUtils.Gen(currentCavePosition, new Shapes.Circle(3), Actions.Chain(new GenAction[]
                {
                    new Actions.ClearTile(),
                    new Actions.SetLiquid()
                }));
            }

            // Create a lot of crystals inside of the squid den.
            Point zeroBiasedDenCenter = new(area.Left + Layer3SquidDenOuterRadius + 15, WorldSaveSystem.SquidDenCenter.Y);
            Rectangle denArea = Utils.CenteredRectangle(zeroBiasedDenCenter.ToVector2(), Vector2.One * Layer3SquidDenOuterRadius * 2.4f);
            TryToGenerateLumenylCrystals(denArea, 200, false);
            TryToGenerateLumenylCrystals(denArea, 300, true);
        }

        public static void GenerateLayer4(Point pedestalPosition)
        {
            int minWidth = MinAbyssWidth;
            int maxWidth = MaxAbyssWidth;
            int entireAbyssTop = AbyssTop;
            int top = Layer4Top;
            int bottom = AbyssBottom;
            int offsetSeed = WorldGen.genRand.Next();
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWallUnsafe>();

            for (int i = 1; i < maxWidth; i++)
            {
                int x = GetActualX(i);
                int yOffset = (int)Math.Abs(FractalBrownianMotion(x / 276f, 0.4f, offsetSeed, 5) * 40f);
                for (int y = top - yOffset; y < bottom - WallThickness + yOffset / 3; y++)
                {
                    // Decide whether to cut off due to a Y point being far enough.
                    int xOffset = (int)Math.Abs(FractalBrownianMotion(1.34f, y / 209f, offsetSeed, 5) * 20f);
                    float yCompletion = Utils.GetLerpValue(entireAbyssTop, bottom - 1f, y, true);
                    if (i >= GetWidth(yCompletion, minWidth, maxWidth) - WallThickness + xOffset)
                        continue;

                    // Otherwise, clear out water.
                    Main.tile[x, y].WallType = voidstoneWallID;
                    ResetToWater(new(x, y));
                }
            }

            // Create the eidolist pedestal.
            GenerateEidolistPedestal(new(pedestalPosition.X, pedestalPosition.Y - 8));

            // Place the Terminus tile.
            GenerateTerminusTile(new(pedestalPosition.X, pedestalPosition.Y + 50));
        }

        public static void GenerateEidolistPedestal(Point pedestalCenter)
        {
            WorldSaveSystem.EidolistWorshipPedestalCenter = new(pedestalCenter.X, pedestalCenter.Y - 2);

            ushort voidstoneID = (ushort)ModContent.TileType<Voidstone>();
            WorldUtils.Gen(pedestalCenter, new CustomInfernumShapes.HalfCircle(EidolistPedestalRadius), Actions.Chain(new GenAction[]
            {
                new Modifiers.Blotches(2, 0.36),
                new Actions.SetTile(voidstoneID)
            }));
        }

        public static void GenerateTerminusTile(Point searchPosition)
        {
            int terminusTileID = ModContent.TileType<TerminusTile>();
            //int dylanShellID = ModContent.TileType<AbyssFossilTile>();

            for (int tries = 0; tries < 180; tries++)
            {
                Point groundPosition = Utilities.GetGroundPositionFrom(searchPosition);
                groundPosition.Y--;

                if (WorldGen.PlaceTile(groundPosition.X, groundPosition.Y, terminusTileID))
                    break;

                // Slide to the right for a different search position if the current one didn't find a valid spot.
                searchPosition.X++;
            }

            // TODO: Figure out how  to properly do this because I do not know worldgen code.
            //// Also place down Dylan's dev item tile, as it otherwise would not be obtainable.
            //for (int tries = 0; tries < 180; tries++)
            //{
            //    Point groundPosition = Utilities.GetGroundPositionFrom(searchPosition);
            //    groundPosition.X++;

            //    if (WorldGen.PlaceTile(groundPosition.X, groundPosition.Y, dylanShellID))
            //        break;

            //    // Slide down for a different search position if the current one didn't find a valid spot.
            //    searchPosition.X++;
            //}
        }

        public static void GenerateVoidstone()
        {
            int top = Layer3Top - 10;
            int bottom = AbyssBottom + 10;
            ushort gravelID = (ushort)ModContent.TileType<AbyssGravel>();
            ushort gravelWallID = (ushort)ModContent.WallType<AbyssGravelWall>();
            ushort voidstoneID = (ushort)ModContent.TileType<Voidstone>();
            ushort voidstoneWallID = (ushort)ModContent.WallType<VoidstoneWall>();
            ushort pyreID = (ushort)ModContent.TileType<PyreMantle>();
            FastRandom rng = new(WorldGen.genRand.Next());

            for (int y = top; y < bottom; y++)
            {
                float ditherChance = Utils.GetLerpValue(top, top + 16f, y, true);
                for (int i = 0; i < MaxAbyssWidth; i++)
                {
                    Tile t = CalamityUtils.ParanoidTileRetrieval(GetActualX(i), y);

                    // Don't convert tiles that aren't abyss gravel in some way.
                    if ((t.TileType != gravelID || !t.HasTile) && t.WallType != gravelWallID)
                        continue;

                    // Don't convert pyre mantle.
                    if (t.TileType == pyreID)
                        continue;

                    // Perform dithering.
                    if (rng.NextFloat() > ditherChance)
                        continue;

                    t.TileType = voidstoneID;
                    t.WallType = voidstoneWallID;
                }
            }
        }
        #endregion Generation Functions

        #region Utilities

        public static int GetWidth(float yCompletion, int minWidth, int maxWidth)
        {
            return (int)Lerp(minWidth, maxWidth, Pow(yCompletion * Utils.GetLerpValue(1f, 0.81f, yCompletion, true), 0.13f));
        }

        public static void ClearOutStrayTiles(Rectangle area)
        {
            int width = BiomeWidth;
            int depth = BlockDepth;
            List<ushort> blockTileTypes = new()
            {
                (ushort)ModContent.TileType<AbyssGravel>(),
                (ushort)ModContent.TileType<Voidstone>()
            };

            void getAttachedPoints(int x, int y, List<Point> points)
            {
                Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                Point p = new(x, y);
                if (!blockTileTypes.Contains(t.TileType) || !t.HasTile || points.Count > 672 || points.Contains(p))
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

                    if (chunkPoints.Count is >= 2 and < 672)
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
            WorldGen.KillTile(p.X, p.Y);

            Main.tile[p].Get<TileWallWireStateData>().HasTile = false;
            Main.tile[p].Get<LiquidData>().LiquidType = LiquidID.Water;
            Main.tile[p].LiquidAmount = 255;

            if (p.X >= 5 && p.Y < Main.maxTilesX - 5)
                Tile.SmoothSlope(p.X, p.Y);
        }

        public static void GenerateScenicTilesInArea(Rectangle area, int chance, int variants, ushort[] groundTiles, ushort[] tileVariants)
        {
            for (int i = area.Left; i < area.Right; i++)
            {
                int x = GetActualX(i);
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                    Tile below = CalamityUtils.ParanoidTileRetrieval(x, y + 1);
                    if (!WorldGen.SolidTile(below) || t.HasTile)
                        continue;

                    if (!WorldGen.genRand.NextBool(chance))
                        continue;

                    if (!groundTiles.Contains(below.TileType))
                        continue;

                    ushort tileID = WorldGen.genRand.Next(tileVariants);
                    PlaceObjectWithGoddamnForce(x, y, tileID, WorldGen.genRand.Next(variants));
                }
            }
        }

        public static void PlaceObjectWithGoddamnForce(int x, int y, ushort tileID, int variant)
        {
            if (!TileObject.CanPlace(x, y, tileID, variant, 0, out _, true))
                return;

            int width = 1;
            int height = 1;
            var tileData = TileObjectData.GetTileData(tileID, variant);
            if (tileData is not null)
            {
                width = tileData.CoordinateFullWidth / 18;
                height = tileData.CoordinateFullHeight / 18;
            }

            for (int i = 0; i < width; i++)
            {
                if (!WorldGen.SolidTile(x + i, y + 1))
                    return;
            }

            // Clear out tiles that were in the area previously, to prevent overlap.
            for (int i = x; i < x + width; i++)
            {
                for (int j = 0; j < height; j++)
                    WorldGen.KillTile(i, y - j);
            }

            for (int i = x; i < x + width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Main.tile[i, y - j].TileType = tileID;
                    Main.tile[i, y - j].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[i, y - j].Get<TileWallWireStateData>().TileFrameX = (short)((i - x + variant * width) * 18);
                    Main.tile[i, y - j].Get<TileWallWireStateData>().TileFrameY = (short)((height - j - 1) * 18);
                    Main.tile[i, y - j].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[i, y - j].Get<TileWallWireStateData>().IsHalfBlock = false;
                }
            }
        }

        public static bool InsideOfLayer1Forest(Point p)
        {
            int x = p.X;
            if (x >= Main.maxTilesX / 2)
                x = Main.maxTilesX - x;

            if (x >= BiomeWidth - WallThickness + 20)
                return false;

            if (p.Y < AbyssTop + 25 || p.Y >= Layer2Top - 5)
                return false;

            float forestNoise = FractalBrownianMotion(p.X * Layer1ForestNoiseMagnificationFactor, p.Y * Layer1ForestNoiseMagnificationFactor, WorldSaveSystem.AbyssLayer1ForestSeed, 5) * 0.5f + 0.5f;
            return forestNoise > Layer1ForestMinNoiseValue;
        }

        public static bool InsideOfLayer3LumenylZone(Point p)
        {
            int x = p.X;
            if (x >= Main.maxTilesX / 2)
                x = Main.maxTilesX - x;

            if (x >= MaxAbyssWidth - WallThickness + 20)
                return false;

            if (p.Y < Layer3Top + 1 || p.Y >= Layer4Top - 5)
                return false;

            // The squid den is always considered a part of the lumenyl zone.
            if (InsideOfLayer3SquidDen(p))
                return false;

            float verticalOffset = FractalBrownianMotion(p.X * Layer3CrystalCaveMagnificationFactor, p.Y * Layer3CrystalCaveMagnificationFactor, WorldSaveSystem.AbyssLayer3CavernSeed, 4) * 45f;

            // Bias towards crystal caves as they reach the fourth layer.
            return Utils.Remap(p.Y + verticalOffset, Layer4Top - 126f, Layer4Top - 104f, 1f, 0f) < CrystalCaveNoiseThreshold;
        }

        public static bool InsideOfLayer3HydrothermalZone(Point p)
        {
            int x = p.X;
            if (x >= Main.maxTilesX / 2)
                x = Main.maxTilesX - x;

            if (x >= MaxAbyssWidth - WallThickness + 1)
                return false;

            if (p.Y < Layer3Top + 1 || p.Y >= Layer4Top - 5)
                return false;

            // The squid den is always considered a part of the lumenyl zone.
            if (InsideOfLayer3SquidDen(p))
                return true;

            float verticalOffset = FractalBrownianMotion(p.X * Layer3CrystalCaveMagnificationFactor, p.Y * Layer3CrystalCaveMagnificationFactor, WorldSaveSystem.AbyssLayer3CavernSeed, 4) * 45f;

            // Bias towards crystal caves as they reach the fourth layer.
            return Utils.Remap(p.Y + verticalOffset, Layer4Top - 126f, Layer4Top - 104f, 1f, 0f) >= CrystalCaveNoiseThreshold;
        }

        public static bool InsideOfLayer3SquidDen(Point p)
        {
            if (p.X < Main.maxTilesX / 2)
                p.X = Main.maxTilesX - p.X;

            return p.ToVector2().WithinRange(WorldSaveSystem.SquidDenCenter.ToVector2(), Layer3SquidDenOuterRadius);
        }
        #endregion Utilities
    }
}
