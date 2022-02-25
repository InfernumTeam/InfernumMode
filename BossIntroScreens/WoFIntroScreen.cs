using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class WoFIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color darkFleshColor = new Color(73, 38, 45);
            Color fleshColor = new Color(178, 105, 112);
            return Color.Lerp(darkFleshColor, fleshColor, (float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.PiOver2) * 0.5f + 0.5f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override int AnimationTime => 240;

        public override string TextToDisplay => "Hungering Conglomeration\nThe Wall of Flesh";

        public override float TextScale => MajorBossTextScale;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.WallofFlesh);

        public override LegacySoundStyle SoundToPlayWithTextCreation => new LegacySoundStyle(SoundID.Roar, 0);

        public override LegacySoundStyle SoundToPlayWithLetterAddition => SoundID.NPCHit13;

        public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

        public override float LetterDisplayCompletionRatio(int animationTimer)
        {
            float completionRatio = Utils.InverseLerp(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

            // If the completion ratio exceeds the point where the name is displayed, display all letters.
            int startOfLargeTextIndex = TextToDisplay.IndexOf('\n');
            int currentIndex = (int)(completionRatio * TextToDisplay.Length);
            if (currentIndex >= startOfLargeTextIndex)
                completionRatio = 1f;

            return completionRatio;
        }
    }
}