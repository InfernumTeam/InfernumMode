using CalamityMod.NPCs.StormWeaver;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class StormWeaverIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float lightningInterpolant = Utils.GetLerpValue(0.77f, 1f, Sin(AnimationCompletion * -Pi * 4f + completionRatio * Pi * 4f) * 0.5f + 0.5f);
            Color skinColor = new(219, 103, 151);
            Color lightningColor = new(190, 233, 215);
            return Color.Lerp(skinColor, lightningColor, lightningInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<StormWeaverHead>()) && Main.npc[NPC.FindFirstNPC(ModContent.NPCType<StormWeaverHead>())].ai[1] != 0f && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
