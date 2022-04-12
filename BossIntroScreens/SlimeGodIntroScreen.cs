using CalamityMod.NPCs.SlimeGod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class SlimeGodIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color redSlimeColor = new(170, 25, 57);
            Color purpleSlimeColor = new(108, 67, 108);

            float colorSpan = (float)Math.Abs(Math.Tan(MathHelper.Pi * completionRatio + Main.GlobalTime * 3f));

            // Perform special checks to prevent potential exceptions causing problems with draw-logic or precision errors.
            if (float.IsInfinity(colorSpan) || float.IsNaN(colorSpan))
                colorSpan = 0f;
            if (colorSpan > 1000f)
                colorSpan = 1000f;

            float colorInterpolant = Utils.InverseLerp(0f, 0.5f, colorSpan, true);
            return Color.Lerp(redSlimeColor, purpleSlimeColor, colorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Primordial Formation\nThe Slime God";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<SlimeGodCore>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/SlimeGodPossession");
    }
}