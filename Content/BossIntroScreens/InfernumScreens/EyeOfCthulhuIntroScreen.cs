using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class EyeOfCthulhuIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color irisColor = new(55, 49, 181);
            Color bloodColor = new(140, 30, 30);
            return Color.Lerp(irisColor, bloodColor, Sin(completionRatio * TwoPi + AnimationCompletion * Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.EyeofCthulhu) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => SoundID.ForceRoar;
    }
}
