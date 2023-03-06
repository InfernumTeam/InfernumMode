using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Backgrounds
{
    public class ProfanedGardenLavaBackground : BaseHellLavaBackground
    {
        public override bool IsActive => Main.LocalPlayer.Infernum_Biome().ZoneProfaned;

        public override Color LavaColor
        {
            get
            {
                if (Main.dayTime)
                    return Color.Lerp(Color.Orange, Color.Wheat, 0.67f) with { A = 0 } * (CalamityUtils.AnyBossNPCS() ? 0.45f : 1f);

                return Color.SkyBlue with { A = 0 } * (CalamityUtils.AnyBossNPCS() ? 0.35f : 1f);
            }
        }
    }
}
