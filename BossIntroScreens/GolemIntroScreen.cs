using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class GolemIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color stoneColor = new Color(130, 68, 8);
            float sunColorInterpolant = Utils.InverseLerp(0.77f, 1f, (float)Math.Sin(AnimationCompletion * MathHelper.Pi * -4f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f);
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

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}