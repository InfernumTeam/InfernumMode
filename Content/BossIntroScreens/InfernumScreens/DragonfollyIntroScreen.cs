using CalamityMod.NPCs.Bumblebirb;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class DragonfollyIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * Pi * 6f - completionRatio * Pi * 3f) * 0.5f + 0.5f;
            Color featherColor = new(194, 145, 81);
            Color lightningColor = new(255, 41, 72);
            return Color.Lerp(featherColor, lightningColor, Pow(colorFadeInterpolant, 10.1f));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
