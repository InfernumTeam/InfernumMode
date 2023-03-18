using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Backgrounds
{
    public class CragsLavaBackground : BaseHellLavaBackground
    {
        public override bool IsActive => Main.LocalPlayer.InCalamity();

        public override Color LavaColor => new(216, 41, 46, 196);
    }
}
