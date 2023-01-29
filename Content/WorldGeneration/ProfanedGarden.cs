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
            Point bottomLeftOfWorld = new(Main.maxTilesX - 30, Main.maxTilesY - 42);
            PlaceSchematic<Action<Chest>>("Profaned Arena", bottomLeftOfWorld, SchematicAnchor.BottomRight, ref _3);
            SchematicMetaTile[,] schematic = TileMaps["Profaned Arena"];
            int width = schematic.GetLength(0);
            int height = schematic.GetLength(1);

            WorldSaveSystem.ProvidenceArena = new(bottomLeftOfWorld.X - width, bottomLeftOfWorld.Y - height, width, height);

            // Sync the tile changes in case they were done due to a boss kill effect.
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = bottomLeftOfWorld.X - width; i < bottomLeftOfWorld.X; i++)
                {
                    for (int j = bottomLeftOfWorld.Y - height; j < bottomLeftOfWorld.Y; j++)
                        NetMessage.SendTileSquare(-1, i, j);
                }
            }

            WorldSaveSystem.HasGeneratedProfanedShrine = true;
        }
    }
}