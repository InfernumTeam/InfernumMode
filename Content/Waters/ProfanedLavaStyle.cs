using CalamityMod.Systems;
using CalamityMod.Waters;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Waters
{
    public class ProfanedLavaflow : ModWaterfallStyle { }

    public class ProfanedLavaStyle : ModLavaStyle
    {
        public override string Texture => "InfernumMode/Content/Waters/ProfanedLava";

        public override string BlockTexture => Texture + "_Block";

        public override string SlopeTexture => Texture + "_Slope";

        public override bool IsLavaActive() => (Main.LocalPlayer.Infernum_Biome().ZoneProfaned || Main.LocalPlayer.Infernum_Biome().ProfanedLavaFountain) && !InfernumConfig.Instance.ReducedGraphicsConfig;

        public override string WaterfallTexture => "InfernumMode/Content/Waters/ProfanedLavaflow";

        public override int GetSplashDust() => 0;

        public override int GetDropletGore() => 0;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            var w = Color.White;
            r = w.R;
            g = w.G;
            b = w.B;
        }
    }
}
