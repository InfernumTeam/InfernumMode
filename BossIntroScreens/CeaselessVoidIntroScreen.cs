using CalamityMod.NPCs.CeaselessVoid;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class CeaselessVoidIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float voidInterpolant = Utils.GetLerpValue(0.77f, 1f, (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 3f + completionRatio * MathHelper.Pi) * 0.5f + 0.5f);
            Color metalColor = new(167, 181, 209);
            Color voidColor = new(12, 18, 27);
            return Color.Lerp(metalColor, voidColor, voidInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Never-Ending\nCeaseless Void";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<CeaselessVoid>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}