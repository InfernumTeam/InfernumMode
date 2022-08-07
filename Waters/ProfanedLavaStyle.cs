using CalamityMod.Waters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Waters
{
    public class ProfanedLavaflow : ModWaterfallStyle { }

    public class ProfanedLavaStyle : CustomLavaStyle
    {
        public override string LavaTexturePath => "InfernumMode/Waters/ProfanedLava";

        public override string BlockTexturePath => LavaTexturePath + "_Block";

        public override string SlopeTexturePath => LavaTexturePath + "_Slope";

        public override bool ChooseLavaStyle() => Main.LocalPlayer.Infernum().ZoneProfaned;

        public override int ChooseWaterfallStyle() => ModContent.Find<ModWaterfallStyle>("InfernumMode/ProfanedLavaflow").Slot;

        public override int GetSplashDust() => 0;

        public override int GetDropletGore() => 0;

        public override void SelectLightColor(ref Color initialLightColor)
        {
            initialLightColor = Color.Lerp(Color.Orange, Color.White, 0.7f);
            initialLightColor.A = 0;
        }
    }
}
