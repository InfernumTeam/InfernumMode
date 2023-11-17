using InfernumMode.Content.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class ProfanedTempleIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Orange;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool CaresAboutBossEffectCondition => false;

        public override int AnimationTime => 210;

        public override bool ShouldBeActive() => !SubworldSystem.IsActive<LostColosseum>() &&
            Main.LocalPlayer.Infernum_Biome().ZoneProfaned && !Main.LocalPlayer.Infernum_Biome().ProfanedTempleAnimationHasPlayed && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceSpawn");

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.Item100;

        public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

        public override float LetterDisplayCompletionRatio(int animationTimer)
        {
            float completionRatio = Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

            // If the completion ratio exceeds the point where the name is displayed, display all letters.
            int startOfLargeTextIndex = TextToDisplay.Value.IndexOf('\n');
            int currentIndex = (int)(completionRatio * TextToDisplay.Value.Length);
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
