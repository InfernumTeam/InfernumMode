using CalamityMod.NPCs.Leviathan;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class AnahitaIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * Pi * 4f + completionRatio * Pi * 12f) * 0.5f + 0.5f;
            Color lightWaterColor = new(187, 206, 245);
            return Color.Lerp(Color.Cyan, lightWaterColor, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Forgotten Deity\nAnahita";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Anahita>());

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("CalamityMod/Sounds/Custom/AbilitySounds/AngelicAllianceActivation");
    }
}