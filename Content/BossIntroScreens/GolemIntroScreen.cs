using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
{
    public class GolemIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color stoneColor = new(130, 68, 8);
            float sunColorInterpolant = Utils.GetLerpValue(0.77f, 1f, Sin(AnimationCompletion * Pi * -4f + completionRatio * TwoPi) * 0.5f + 0.5f);
            return Color.Lerp(stoneColor, new Color(255, 170, 0), sunColorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay
        {
            get
            {
                if (IntroScreenManager.ShouldDisplayJokeIntroText)
                    return "NUMBER ! SALSMAN\n[Circa 1997]";

                return "The Ancient Idol\nGolem";
            }
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.Golem);

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}