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
            for (int i = 0; i < 10000; i++)
            {
                int placementPositionX = WorldGen.genRand.Next(WorldGen.tLeft - 270, WorldGen.tRight + 270);
                int placementPositionY = WorldGen.tTop < Main.rockLayer - 10.0 ? WorldGen.tBottom + 130 : WorldGen.tTop - 130;
                placementPoint = new(placementPositionX, placementPositionY + WorldGen.genRand.Next(-120, 120));
                Rectangle area = CalamityUtils.GetSchematicProtectionArea(schematic, placementPoint, schematicAnchor);

                // Check if the spot is valid.
                if (WorldGen.structures.CanPlace(area, 26) && CalamityUtils.ParanoidTileRetrieval(area.Center.X, area.Center.Y).WallType != WallID.JungleUnsafe1)
                {
                    protectionArea = area;
                    break;
                }
            }
            WorldSaveSystem.BlossomGardenCenter = placementPoint;
            bool _ = false;
            PlaceSchematic<Action<Chest>>("BlossomGarden", placementPoint, schematicAnchor, ref _);

            WorldGen.structures.AddProtectedStructure(protectionArea, 16);
        }
    }
}
