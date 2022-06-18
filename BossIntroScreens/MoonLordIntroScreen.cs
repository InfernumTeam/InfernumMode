using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BossIntroScreens
{
    public class MoonLordIntroScreen : BaseIntroScreen
    {
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override float TextScale => MajorBossTextScale * 0.75f;

        public override TextColorData TextColor => new TextColorData(c =>
        {
            return Color.Lerp(Color.Turquoise, Color.Gray, (float)Math.Sin(AnimationCompletion * 12f + c * MathHelper.Pi * 4f) * 0.5f + 0.5f);
        });

        public override Color ScreenCoverColor => Color.Black;

        public override string TextToDisplay => "The Remains of the Moon Lord-";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.MoonLordCore);

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}