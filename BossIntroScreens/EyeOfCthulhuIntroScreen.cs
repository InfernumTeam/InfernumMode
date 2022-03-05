using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class EyeOfCthulhuIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color irisColor = new Color(55, 49, 181);
            Color bloodColor = new Color(140, 30, 30);
            return Color.Lerp(irisColor, bloodColor, (float)Math.Sin(completionRatio * MathHelper.TwoPi + AnimationCompletion * MathHelper.Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Demonic Seer\nThe Eye of Cthulhu";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.EyeofCthulhu);

        public override LegacySoundStyle SoundToPlayWithTextCreation => new LegacySoundStyle(SoundID.ForceRoar, -1);
    }
}