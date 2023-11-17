using CalamityMod.NPCs.GreatSandShark;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class GreatSandSharkIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color sandColor = new(191, 142, 104);
            Color sandColor2 = new(150, 96, 88);
            return Color.Lerp(sandColor, sandColor2, Sin(completionRatio * Pi * 3f - AnimationCompletion * Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<GreatSandShark>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
