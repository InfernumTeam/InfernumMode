using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Waters
{
    public class AbyssWater : ModWaterStyle
    {
        public override int ChooseWaterfallStyle() => ModContent.Find<ModWaterfallStyle>("InfernumMode/Content/AbyssWaterflow").Slot;

        public override int GetSplashDust() => 33;

        public override int GetDropletGore() => 713;

        public override Color BiomeHairColor() => Color.DarkBlue;
    }
}
