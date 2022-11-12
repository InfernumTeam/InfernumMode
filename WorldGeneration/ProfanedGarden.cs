using CalamityMod.Schematics;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.IO;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.WorldGeneration
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
            WorldSaveSystem.HasGeneratedProfanedShrine = true;
        }
    }
}