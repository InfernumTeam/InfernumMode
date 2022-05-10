using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class KingSlimeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 4f + completionRatio * MathHelper.Pi * 12f) * 0.5f + 0.5f;
            return Color.Lerp(Color.MediumSlateBlue, Color.DarkCyan, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Monarch of the Gelatinous\nKing Slime";

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.KingSlime);

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/SlimeGodPossession");
    }
}