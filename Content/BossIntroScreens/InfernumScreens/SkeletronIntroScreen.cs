using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class SkeletronIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color shadowflameColor = new(200, 113, 255);
            Color boneColor = new(198, 187, 157);
            return Color.Lerp(shadowflameColor, boneColor, Sin(completionRatio * Pi * 4f + AnimationCompletion * Pi / 3f) * 0.5f + 0.5f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.SkeletronHead) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
