using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class AnahitaIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 4f + completionRatio * MathHelper.Pi * 12f) * 0.5f + 0.5f;
            Color lightWaterColor = new(187, 206, 245);
            return Color.Lerp(Color.Cyan, lightWaterColor, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Forgotten Deity\nAnahita";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Siren>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/AbilitySounds/AngelicAllianceActivation");
    }
}