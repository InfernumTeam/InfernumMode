using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class LeviathanIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = MathF.Sin(AnimationCompletion * MathHelper.TwoPi + completionRatio * MathHelper.Pi * 64f) * 0.5f + 0.5f;
            Color lightSkinColor = new(80, 211, 174);
            Color darkSkinColor = new(0, 149, 159);
            return Color.Lerp(lightSkinColor, darkSkinColor, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Timeworn Beast\nThe Leviathan";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Leviathan>());

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/LeviathanRoarCharge");
    }
}