using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class BrainOfCthulhuIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color ichorColor = new Color(250, 171, 4);
            return Color.Lerp(Color.IndianRed, ichorColor, ((float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.PiOver2) * 0.5f + 0.5f) * 0.6f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Eldritch Mind\nThe Brain of Cthulhu";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.BrainofCthulhu);

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}