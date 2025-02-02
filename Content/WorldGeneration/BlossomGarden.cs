using System;
using CalamityMod;
using CalamityMod.Schematics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.Content.WorldGeneration
{
    public class BlossomGarden
    {
        public static void Generate(GenerationProgress progress, GameConfiguration _2)
        {
            progress.Message = Language.GetTextValue("Mods.InfernumMode.WorldGen.BlossomGarden");

            var schematic = TileMaps["BlossomGarden"];
            var schematicAnchor = SchematicAnchor.Center;
            // This is the same code the vernal pass uses to generate, with a little more horizontal variation.
            int placementPositionX = WorldGen.genRand.Next(GenVars.tLeft - 30, GenVars.tRight + 30);
            int placementPositionY = GenVars.tTop < Main.rockLayer - 10 ? GenVars.tBottom + 120 : GenVars.tTop - 120;
            // Shove it upwards from the pass.
            placementPositionY -= 200;
            var placementPoint = new Point(placementPositionX, placementPositionY);
            Rectangle protectionArea = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, schematicAnchor);

            // Attempt to find a valid position.
            for (int i = 0; i < 50; i++)
            {
                // Randomly pick whether it should be above or below the Vernal Pass. Used to dodge placing the structure right on top of it.
                bool above = WorldGen.genRand.NextBool();
                int randY = WorldGen.genRand.Next(0, 400) * (above ? -1 : 1);

                // Randomly offset the point.
                var attemptPlacementPoint = placementPoint + new Point(WorldGen.genRand.Next(-400, 400), randY);
                if (!above)
                    attemptPlacementPoint.Y += 400;
                // Check if the new position is valid.

                var attemptProtectionArea = CalamityUtils.GetSchematicProtectionArea(schematic, attemptPlacementPoint, schematicAnchor);
                // Check if all of the corners are close to jungle (jungle grass)
                bool cornersJungle =
                    CheckJungle(attemptProtectionArea.TopLeft().ToPoint()) && CheckJungle(attemptProtectionArea.TopRight().ToPoint()) &&
                    CheckJungle(attemptProtectionArea.BottomLeft().ToPoint()) && CheckJungle(attemptProtectionArea.BottomRight().ToPoint()) &&
                    CheckJungle(attemptProtectionArea.Top().ToPoint()) && CheckJungle(attemptProtectionArea.Bottom().ToPoint());

                if (attemptPlacementPoint.Y > Main.worldSurface + 150 && cornersJungle)
                {
                    // success
                    placementPoint = attemptPlacementPoint;
                    protectionArea = attemptProtectionArea;
                    break;
                }

            }
            
            WorldSaveSystem.BlossomGardenCenter = placementPoint;

            bool _ = false;
            PlaceSchematic<Action<Chest>>("BlossomGarden", placementPoint, schematicAnchor, ref _, chest =>
            {
                for (int i = 0; i < 11; i++)
                {   
                    int chestItemIndex = WorldGen.genRand.Next(20);
                    int oldStack = chest.item[chestItemIndex].stack;
                    chest.item[chestItemIndex].SetDefaults(ItemID.OrangeTorch);
                    chest.item[chestItemIndex].stack = oldStack + 1;
                }
            });

            GenVars.structures.AddProtectedStructure(protectionArea, 16);
        }

        public static bool CheckJungle(Point point)
        {
            for (int x = -7; x <= 7; x++)
            {
                for (int y = -7; y <= 7; y++)
                {
                    Point p = point + new Point(x, y);
                    if (!WorldGen.InWorld(p.X, p.Y))
                        continue;
                    Tile checkTile = Main.tile[p];
                    if (checkTile.TileType == TileID.JungleGrass)
                        return true;
                    if (checkTile.TileType == TileID.LihzahrdBrick || checkTile.WallType == WallID.LihzahrdBrickUnsafe)
                        return false;
                }
            }
            return false;
        }
    }
}
