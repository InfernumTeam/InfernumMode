using CalamityMod;
using CalamityMod.Schematics;
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
    public class BlossomGarden
    {
        public static void Generate(GenerationProgress progress, GameConfiguration _2)
        {
            progress.Message = "Growing a garden...";

            SchematicMetaTile[,] schematic = TileMaps["BlossomGarden"];
            Point placementPoint = default;
            Rectangle protectionArea = default;
            SchematicAnchor schematicAnchor = SchematicAnchor.Center;
            for (int i = 0; i < 20000; i++)
            {
                int placementPositionX = WorldGen.genRand.Next(450, Main.maxTilesX - 450);
                int placementPositionY = (int)Main.rockLayer + WorldGen.genRand.Next(100, 1000);
                placementPoint = new(placementPositionX, placementPositionY);
                Rectangle area = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, schematicAnchor);

                // Check if the spot is valid.
                if (CalamityUtils.ParanoidTileRetrieval(area.Center.X, area.Center.Y).WallType == WallID.HiveUnsafe && GenVars.structures.CanPlace(area, 10))
                {
                    protectionArea = area;
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
    }
}
