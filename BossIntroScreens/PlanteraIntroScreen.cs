using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class PlanteraIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color plantColor = new Color(96, 148, 14);
            Color flowerColor = new Color(225, 128, 206);
            float flowerInterpolant = (float)Math.Pow(Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.Pi) * 0.5f + 0.5f, 2.3);
            return Color.Lerp(plantColor, flowerColor, flowerInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Overgrowth\nPlantera";

        public override float TextScale => MajorBossTextScale;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.Plantera);

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;

        public override LegacySoundStyle SoundToPlayWithLetterAddition => SoundID.Item17;

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