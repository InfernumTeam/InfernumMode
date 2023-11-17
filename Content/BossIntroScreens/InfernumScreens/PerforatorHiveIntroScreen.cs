using CalamityMod.NPCs.Perforator;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class PerforatorHiveIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color ichorColor = new(252, 180, 3);
            return Color.Lerp(Color.DarkRed, ichorColor, (Sin(completionRatio * Pi * 3f + AnimationCompletion * PiOver2) * 0.5f + 0.5f) * 0.6f);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
