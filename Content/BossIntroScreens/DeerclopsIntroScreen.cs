using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
{
    public class DeerclopsIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * TwoPi + completionRatio * Pi) * 0.5f + 0.5f;
            return Color.Lerp(Color.White, Color.LightGray, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Winter Beast\nThe Deerclops";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.Deerclops);

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}