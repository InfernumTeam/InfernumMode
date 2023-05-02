using CalamityMod.NPCs.Bumblebirb;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class DragonfollyIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = MathF.Sin(AnimationCompletion * MathHelper.Pi * 6f - completionRatio * MathHelper.Pi * 3f) * 0.5f + 0.5f;
            Color featherColor = new(194, 145, 81);
            Color lightningColor = new(255, 41, 72);
            return Color.Lerp(featherColor, lightningColor, (float)Math.Pow(colorFadeInterpolant, 10.1));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "False Offspring\nThe Dragonfolly";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}