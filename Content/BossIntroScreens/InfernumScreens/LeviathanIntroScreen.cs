using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class LeviathanIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * TwoPi + completionRatio * Pi * 64f) * 0.5f + 0.5f;
            Color lightSkinColor = new(80, 211, 174);
            Color darkSkinColor = new(0, 149, 159);
            return Color.Lerp(lightSkinColor, darkSkinColor, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Leviathan>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/LeviathanRoarCharge");
    }
}
