using CalamityMod.NPCs.Ravager;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class RavagerIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color redFireColor = new(255, 85, 0);
            Color blueFireColor = new(111, 89, 255);

            float colorSpan = (float)Math.Abs(Math.Tan(MathHelper.Pi * completionRatio + Main.GlobalTimeWrappedHourly * 3f));

            // Perform special checks to prevent potential exceptions causing problems with draw-logic or precision errors.
            if (float.IsInfinity(colorSpan) || float.IsNaN(colorSpan))
                colorSpan = 0f;
            if (colorSpan > 1000f)
                colorSpan = 1000f;

            float colorInterpolant = Utils.GetLerpValue(0f, 0.5f, colorSpan, true);
            return Color.Lerp(redFireColor, blueFireColor, colorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Fortress of Flesh\nRavager";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<RavagerBody>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}