using CalamityMod;
using CalamityMod.NPCs.Polterghast;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class PolterghastIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorInterpolant = (float)Math.Sin(completionRatio * MathHelper.Pi * 4f + AnimationCompletion * MathHelper.TwoPi * 0.4f) * 0.5f + 0.5f;
            Color pink = Color.HotPink;
            Color cyan = Color.Cyan;
            return Color.Lerp(pink, cyan, CalamityUtils.Convert01To010(colorInterpolant * 3f % 1f));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Wrathful Coalescence\nThe Polterghast";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Polterghast>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}