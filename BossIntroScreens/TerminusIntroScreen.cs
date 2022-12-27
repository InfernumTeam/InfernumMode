using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class TerminusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio => Color.LightCoral);

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool CaresAboutBossEffectCondition => false;

        public override int AnimationTime => 360;

        public override string TextToDisplay => "You found the Terminus!";

        public override bool ShouldBeActive() => !NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>()) && Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;

        public override float LetterDisplayCompletionRatio(int animationTimer) =>
            Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);
    }
}