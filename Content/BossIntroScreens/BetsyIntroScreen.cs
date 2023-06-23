using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
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

        public override string TextToDisplay => "Mother of Wyverns\nBetsy";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.DD2Betsy);

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}