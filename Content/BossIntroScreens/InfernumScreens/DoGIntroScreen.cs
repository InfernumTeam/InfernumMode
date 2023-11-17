using CalamityMod.NPCs.DevourerofGods;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class DoGIntroScreen : BaseIntroScreen
    {
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override float TextScale => MajorBossTextScale;

        public override TextColorData TextColor => new(c => Color.Lerp(Color.Cyan, Color.Fuchsia, Sin(AnimationCompletion * 8f + c * Pi * 3f) * 0.5f + 0.5f));

        public override Color ScreenCoverColor => Color.Black;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => DevourerofGodsHead.AttackSound;
    }
}
