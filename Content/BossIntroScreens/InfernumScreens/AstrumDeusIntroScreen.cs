using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class AstrumDeusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorInterpolant = Sin(completionRatio * Pi * 4f + AnimationCompletion * 1.45f * TwoPi) * 0.5f + 0.5f;
            return Color.Lerp(new(68, 221, 204), new(255, 100, 80), colorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
