using System;
using CalamityMod.Tiles.FurnitureProfaned;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class InfernumBiomeTileCounterSystem : ModSystem
    {
        public static int ProfanedTile
        {
            get;
            set;
        }

        public override void ResetNearbyTileEffects()
        {
            ProfanedTile = 0;
        }

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            ProfanedTile = tileCounts[ModContent.TileType<ProfanedSlab>()] + tileCounts[ModContent.TileType<RunicProfanedBrick>()] + tileCounts[ModContent.TileType<ProfanedRock>()];
        }
    }
}
