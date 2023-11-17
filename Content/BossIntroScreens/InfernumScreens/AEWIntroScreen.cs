using CalamityMod;
using CalamityMod.NPCs.PrimordialWyrm;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class AEWIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorInterpolant = Sin(completionRatio * Pi * 4f + AnimationCompletion * TwoPi * 0.4f) * 0.5f + 0.5f;
            Color dark = Color.Lerp(Color.Navy, Color.Black, 0.7f);
            Color light = Color.Lerp(Color.Blue, Color.White, 0.65f);
            return Color.Lerp(light, dark, CalamityUtils.Convert01To010(colorInterpolant * 3f % 1f) * 0.6f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool CaresAboutBossEffectCondition => true;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
