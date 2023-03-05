using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Backgrounds
{
    public class ProfanedGardenLavaBackground : BaseHellLavaBackground
    {
        public override bool IsActive => Main.LocalPlayer.Infernum_Biome().ZoneProfaned;

        public override Color LavaColor => Color.Lerp(Color.Orange, Color.Wheat, 0.67f) with { A = 0 };
    }
}
