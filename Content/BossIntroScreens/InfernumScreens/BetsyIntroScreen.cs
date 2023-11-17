using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class BetsyIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color c1 = new(155, 39, 68);
            Color c2 = new(255, 145, 57);
            float orangeFlash = Pow(CalamityUtils.Convert01To010(AnimationCompletion), 4f);
            return Color.Lerp(c1, c2, orangeFlash);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.DD2Betsy) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
