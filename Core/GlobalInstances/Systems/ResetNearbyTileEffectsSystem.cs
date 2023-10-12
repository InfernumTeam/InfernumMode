using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class ResetNearbyTileEffectsSystem : ModSystem
    {
        public override void ResetNearbyTileEffects()
        {
            Main.LocalPlayer.Infernum_Biome().ProfanedLavaFountain = false;
            Main.LocalPlayer.Infernum_Biome().CosmicBackgroundEffect = false;
            Main.LocalPlayer.Infernum_Biome().AstralMonolithEffect = false;
        }
    }
}
