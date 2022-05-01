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
            int finalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
            if (finalIndex != -1    )
            {
                int currentFinalIndex = finalIndex;
                tasks.Insert(++currentFinalIndex, new PassLegacy("Prov Arena", (progress, config) =>
                {
                    progress.Message = "Constructing a temple for an ancient goddess";
                    GenerateProfanedShrine(progress, config);
                }));
            }
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

        public static void GenerateProfanedShrine(GenerationProgress progress, GameConfiguration config)
        {
            int width = 250;
            int height = 125;
            ushort slabID = (ushort)ModContent.TileType<ProfanedSlab>();
            ushort runeID = (ushort)ModContent.TileType<RunicProfanedBrick>();
            ushort provSummonerID = (ushort)ModContent.TileType<ProvidenceSummoner>();

            int left = Main.maxTilesX - width - Main.offLimitBorderTiles;
            int top = Main.UnderworldLayer + 20;
            int bottom = top + height;
            int centerX = left + width / 2;

            // Define the arena area.
            WorldSaveSystem.ProvidenceArena = new(left, top, width, height);

            // Clear out the entire area where the shrine will be made.
            for (int x = left; x < left + width; x++)
            {
                for (int y = top; y < bottom; y++)
                {
                    Main.tile[x, y].LiquidAmount = 0;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                }
            }

            // Create the floor and ceiling.
            for (int x = left; x < left + width; x++)
            {
                int y = bottom - 1;
                while (!Main.tile[x, y].HasTile)
                {
                    Main.tile[x, y].LiquidAmount = 0;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[x, y].TileType = WorldGen.genRand.NextBool(5) ? runeID : slabID;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;

                    y++;

                    if (y >= Main.maxTilesY)
                        break;
                }

                y = top + 1;
                while (!Main.tile[x, y].HasTile)
                {
                    Main.tile[x, y].LiquidAmount = 0;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[x, y].TileType = WorldGen.genRand.NextBool(5) ? runeID : slabID;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;

                    y--;

                    if (y < top - 40)
                        break;
                }
            }

            // Create the right wall.
            for (int y = top; y < bottom + 2; y++)
            {
                int x = left + width - 1;
                Main.tile[x, y].LiquidAmount = 0;
                Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                Main.tile[x, y].TileType = WorldGen.genRand.NextBool(5) ? runeID : slabID;
                Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;
            }

            // Find the vertical point at which stairs should be placed.
            int stairLeft = left - 1;
            int stairTop = bottom - 1;
            while (Main.tile[stairLeft, stairTop].LiquidAmount > 0 || Main.tile[stairLeft, stairTop].HasTile)
                stairTop--;

            // Create stairs until a bottom is reached.
            int stairWidth = bottom - stairTop;
            for (int x = stairLeft - 3; x < stairLeft + stairWidth; x++)
            {
                int stairHeight = stairWidth - (x - stairLeft);
                if (x < stairLeft)
                    stairHeight = stairWidth;

                for (int y = -stairHeight; y < 0; y++)
                {
                    Main.tile[x, y + bottom].LiquidAmount = 0;
                    Main.tile[x, y + bottom].TileType = WorldGen.genRand.NextBool(5) ? runeID : slabID;
                    Main.tile[x, y + bottom].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[x, y + bottom].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y + bottom].Get<TileWallWireStateData>().IsHalfBlock = false;
                    WorldGen.TileFrame(x, y + bottom);
                }
            }

            // Settle liquids.
            Liquid.QuickWater(3);
            WorldGen.WaterCheck();

            Liquid.quickSettle = true;
            for (int i = 0; i < 10; i++)
            {
                while (Liquid.numLiquid > 0)
                    Liquid.UpdateLiquid();
                WorldGen.WaterCheck();
            }
            Liquid.quickSettle = false;

            // Clear out any liquids.
            for (int x = left - 20; x < Main.maxTilesX; x++)
            {
                for (int y = top - 15; y < bottom + 8; y++)
                    Main.tile[x, y].LiquidAmount = 0;
            }

            // Create the Providence altar.
            short frameX = 0;
            short frameY;
            for (int x = centerX; x < centerX + ProvidenceSummoner.Width; x++)
            {
                frameY = 0;
                for (int y = bottom - 3; y < bottom + ProvidenceSummoner.Height - 3; y++)
                {
                    Main.tile[x, y].LiquidAmount = 0;
                    Main.tile[x, y].TileType = provSummonerID;
                    Main.tile[x, y].TileFrameX = frameX;
                    Main.tile[x, y].TileFrameY = frameY;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;

                    frameY += 18;
                }
                frameX += 18;
            }
        }
    }
}