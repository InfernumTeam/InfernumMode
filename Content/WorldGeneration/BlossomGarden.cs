using System;
using CalamityMod;
using CalamityMod.Schematics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
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

            var schematic = TileMaps["BlossomGarden"];
            var schematicAnchor = SchematicAnchor.Center;
            // This is the same code the vernal pass uses to generate, with a little more horizontal variation.
            int placementPositionX = WorldGen.genRand.Next(GenVars.tLeft - 30, GenVars.tRight + 30);
            int placementPositionY = GenVars.tTop < Main.rockLayer - 10 ? GenVars.tBottom + 120 : GenVars.tTop - 120;
            // Shove it upwards from the pass.
            placementPositionY -= 200;
            var placementPoint = new Point(placementPositionX, placementPositionY);
            var protectionArea = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, schematicAnchor);
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
