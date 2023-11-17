using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class HiveMindIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            Color cursedFlameColor = new(0, 175, 51);
            Color corruptFleshColor = new(130, 97, 124);
            return Color.Lerp(cursedFlameColor, corruptFleshColor, Sin(completionRatio * Pi * 4f + AnimationCompletion * Pi));
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<HiveMind>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
