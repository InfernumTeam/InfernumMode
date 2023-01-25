using CalamityMod.Waters;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Waters
{
    public class ProfanedLavaflow : ModWaterfallStyle { }

    public class ProfanedLavaStyle : CustomLavaStyle
    {
        public override string LavaTexturePath => "InfernumMode/Content/Waters/ProfanedLava";

        public override string BlockTexturePath => LavaTexturePath + "_Block";

        public override string SlopeTexturePath => LavaTexturePath + "_Slope";

        public override bool ChooseLavaStyle() => (Main.LocalPlayer.Infernum_Biome().ZoneProfaned || Main.LocalPlayer.Infernum_Biome().ProfanedLavaFountain) && !InfernumConfig.Instance.ReducedGraphicsConfig;

        public override int ChooseWaterfallStyle() => ModContent.Find<ModWaterfallStyle>("InfernumMode/ProfanedLavaflow").Slot;

        public override int GetSplashDust() => 0;

        public override int GetDropletGore() => 0;

        public override void SelectLightColor(ref Color initialLightColor)
        {
            initialLightColor = Color.White;
        }
    }
}
