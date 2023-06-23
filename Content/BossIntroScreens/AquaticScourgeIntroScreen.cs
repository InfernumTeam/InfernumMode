using CalamityMod.NPCs.AquaticScourge;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class AquaticScourgeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color sulphuricColor = new(41, 142, 134);
            Color fleshColor = new(165, 119, 112);
            return Color.Lerp(sulphuricColor, fleshColor, (Sin(completionRatio * Pi * 3f + AnimationCompletion * PiOver2) * 0.5f + 0.5f) * 0.72f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Sulphuric Serpent\nThe Aquatic Scourge";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}