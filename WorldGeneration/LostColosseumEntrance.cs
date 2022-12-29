using CalamityMod;
using CalamityMod.Schematics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.IO;
using Terraria.WorldBuilding;
using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.WorldGeneration
{
    public static class LostColosseumEntrance
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            Point centerLeft = WorldGen.UndergroundDesertLocation.Center;
            centerLeft.Y -= 120;

            while (CalamityUtils.ParanoidTileRetrieval(centerLeft.X, centerLeft.Y).HasTile)
                centerLeft.X++;

            bool _ = false;
            PlaceSchematic<Action<Chest>>("LostColosseumEntrance", centerLeft, SchematicAnchor.CenterLeft, ref _);
        }
    }
}