using CalamityMod.NPCs.Ravager;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class RavagerIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color redFireColor = new(255, 85, 0);
            Color blueFireColor = new(111, 89, 255);

            float colorSpan = Math.Abs(Tan(Pi * completionRatio + Main.GlobalTimeWrappedHourly * 3f));

            // Perform special checks to prevent potential exceptions causing problems with draw-logic or precision errors.
            if (float.IsInfinity(colorSpan) || float.IsNaN(colorSpan))
                colorSpan = 0f;
            if (colorSpan > 1000f)
                colorSpan = 1000f;

            float colorInterpolant = Utils.GetLerpValue(0f, 1.32f, colorSpan, true);
            return Color.Lerp(redFireColor, blueFireColor, colorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<RavagerBody>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
