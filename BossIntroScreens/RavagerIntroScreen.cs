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
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color redFireColor = new Color(255, 85, 0);
            Color blueFireColor = new Color(111, 89, 255);

            float colorSpan = (float)Math.Abs(Math.Tan(MathHelper.Pi * completionRatio + Main.GlobalTime * 3f));

            // Perform special checks to prevent potential exceptions causing problems with draw-logic or precision errors.
            if (float.IsInfinity(colorSpan) || float.IsNaN(colorSpan))
                colorSpan = 0f;
            if (colorSpan > 1000f)
                colorSpan = 1000f;

            float colorInterpolant = Utils.InverseLerp(0f, 1.32f, colorSpan, true);
            return Color.Lerp(redFireColor, blueFireColor, colorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Fortress of Flesh\nRavager";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<RavagerBody>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}