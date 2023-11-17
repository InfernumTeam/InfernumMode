using CalamityMod.NPCs.AquaticScourge;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
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

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AquaticScourgeHead>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
