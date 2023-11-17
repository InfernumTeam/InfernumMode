using CalamityMod;
using CalamityMod.NPCs.Polterghast;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class PolterghastIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorInterpolant = Sin(completionRatio * Pi * 4f + AnimationCompletion * TwoPi * 0.4f) * 0.5f + 0.5f;
            Color pink = Color.HotPink;
            Color cyan = Color.Cyan;
            return Color.Lerp(pink, cyan, CalamityUtils.Convert01To010(colorInterpolant * 3f % 1f));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Polterghast>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
