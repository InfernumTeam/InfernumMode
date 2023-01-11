using CalamityMod.NPCs.Calamitas;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class CalamitasCloneIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Red;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "False Witch\nCalamitas' Clone";

        public override float TextScale => MajorBossTextScale;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<CalamitasClone>());

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
    }
}