using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens
{
    public class QueenSlimeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float hue = Sin(completionRatio * Pi * 3f + AnimationCompletion * Pi) * 0.5f + 0.5f;
            return Color.Lerp(Color.SkyBlue, Color.HotPink, hue);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Hallowed Mass\nThe Queen Slime";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.QueenSlimeBoss);

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}