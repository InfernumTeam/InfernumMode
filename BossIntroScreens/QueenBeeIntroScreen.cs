using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
	public class QueenBeeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(_ =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 4f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Yellow, Color.Orange, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Oversized Insect\nQueen Bee";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.QueenBee);

        public override LegacySoundStyle SoundToPlayWithTextCreation => SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/AbilitySounds/PlagueReaperAbility");
    }
}