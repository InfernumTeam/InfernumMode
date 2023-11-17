using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Content.Projectiles.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class TerminusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio => Color.LightCoral);

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool CaresAboutBossEffectCondition => false;

        public override int AnimationTime => 360;

        public override bool ShouldBeActive() => !NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) && Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;

        public override float LetterDisplayCompletionRatio(int animationTimer) =>
            Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);
    }
}
