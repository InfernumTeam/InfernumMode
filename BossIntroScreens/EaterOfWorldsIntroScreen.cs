using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class EaterOfWorldsIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color cursedFlameColor = new Color(0, 145, 45);
            Color corruptFleshColor = new Color(130, 97, 124);
            return Color.Lerp(cursedFlameColor, corruptFleshColor, (float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.PiOver2));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Terror of the Corruption\nThe Eater of Worlds";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.EaterofWorldsHead);

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}