using CalamityMod.NPCs.AquaticScourge;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class AquaticScourgeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            Color sulphuricColor = new Color(41, 142, 134);
            Color fleshColor = new Color(165, 119, 112);
            return Color.Lerp(sulphuricColor, fleshColor, ((float)Math.Sin(completionRatio * MathHelper.Pi * 3f + AnimationCompletion * MathHelper.PiOver2) * 0.5f + 0.5f) * 0.72f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "Sulpuric Serpent\nThe Aquatic Scourge";

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => InfernumMode.CalamityMod.GetLegacySoundSlot(TMLSoundType.Custom, "Sounds/Custom/DesertScourgeRoar");
    }
}