using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class EaterOfWorldsIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color cursedFlameColor = new(0, 145, 45);
            Color corruptFleshColor = new(130, 97, 124);
            return Color.Lerp(cursedFlameColor, corruptFleshColor, Sin(completionRatio * Pi * 3f + AnimationCompletion * PiOver2));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.EaterofWorldsHead) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
