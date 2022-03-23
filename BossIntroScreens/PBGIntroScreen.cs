using CalamityMod.NPCs.PlaguebringerGoliath;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class PBGIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 4f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
            return Color.Lerp(Color.Lime, Color.DarkOliveGreen, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Infected Insectoid\nThe Plaguebringer Goliath";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<PlaguebringerGoliath>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/AbilitySounds/PlagueReaperAbility");
    }
}