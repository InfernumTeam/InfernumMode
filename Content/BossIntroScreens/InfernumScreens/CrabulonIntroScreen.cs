using CalamityMod;
using CalamityMod.NPCs.Crabulon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class CrabulonIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Lerp(Color.Cyan, Color.DeepSkyBlue, CalamityUtils.Convert01To010(AnimationCompletion));

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Crabulon>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
