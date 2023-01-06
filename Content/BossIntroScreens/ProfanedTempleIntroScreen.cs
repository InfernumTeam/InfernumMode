using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
{
    public class ProfanedTempleIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Orange;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool CaresAboutBossEffectCondition => false;

        public override int AnimationTime => 210;

        public override string TextToDisplay => "Cleansed Site\nThe Profaned Garden";

        public override bool ShouldBeActive() => Main.LocalPlayer.Infernum_Biome().ZoneProfaned && !Main.LocalPlayer.Infernum_Biome().ProfanedTempleAnimationHasPlayed;

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/ProvidenceSpawn");

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.Item100;

        public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

        public override float LetterDisplayCompletionRatio(int animationTimer)
        {
            float completionRatio = Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

            // If the completion ratio exceeds the point where the name is displayed, display all letters.
            int startOfLargeTextIndex = TextToDisplay.IndexOf('\n');
            int currentIndex = (int)(completionRatio * TextToDisplay.Length);
            if (currentIndex >= startOfLargeTextIndex)
                completionRatio = 1f;

            return completionRatio;
        }

        public override void DoCompletionEffects()
        {
            Main.LocalPlayer.Infernum_Biome().ProfanedTempleAnimationHasPlayed = true;
            AnimationTimer = 0;
        }
    }
}