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
    public static class ProfanedGarden
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            bool _3 = false;
            Point bottomRightOfWorld = new(Main.maxTilesX - 42, Main.maxTilesY - 42);
            PlaceSchematic<Action<Chest>>("Profaned Arena", bottomRightOfWorld, SchematicAnchor.BottomRight, ref _3);
            SchematicMetaTile[,] schematic = TileMaps["Profaned Arena"];
            int width = schematic.GetLength(0);
            int height = schematic.GetLength(1);

            WorldSaveSystem.ProvidenceArena = new(bottomRightOfWorld.X - width, bottomRightOfWorld.Y - height, width, height);

            // Sync the tile changes in case they were done due to a boss kill effect.
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = bottomRightOfWorld.X - width; i < bottomRightOfWorld.X; i++)
                {
                    for (int j = bottomRightOfWorld.Y - height; j < bottomRightOfWorld.Y; j++)
                        NetMessage.SendTileSquare(-1, i, j);
                }
            }

            WorldSaveSystem.HasGeneratedProfanedShrine = true;
            WorldSaveSystem.HasProvidenceDoorShattered = false;
            WorldSaveSystem.WayfinderGateLocation = Vector2.Zero;

            // Start this really far out so it doesn't try and shove the player left of it. Bit hacky, but oh well.
            WorldSaveSystem.ProvidenceDoorXPosition = int.MaxValue;
        }
    }
}
