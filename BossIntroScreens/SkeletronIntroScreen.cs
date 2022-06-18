using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class SkeletronIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color shadowflameColor = new Color(200, 113, 255);
            Color boneColor = new Color(198, 187, 157);
            return Color.Lerp(shadowflameColor, boneColor, (float)Math.Sin(completionRatio * MathHelper.Pi * 4f + AnimationCompletion * MathHelper.Pi / 3f) * 0.5f + 0.5f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Old Man's Curse\nSkeletron";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.SkeletronHead);

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}