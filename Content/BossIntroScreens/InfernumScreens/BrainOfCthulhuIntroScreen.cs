using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class BrainOfCthulhuIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color ichorColor = new(250, 171, 4);
            return Color.Lerp(Color.IndianRed, ichorColor, (Sin(completionRatio * Pi * 3f + AnimationCompletion * PiOver2) * 0.5f + 0.5f) * 0.6f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.BrainofCthulhu) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
