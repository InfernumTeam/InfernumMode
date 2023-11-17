using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class MoonLordIntroScreen : BaseIntroScreen
    {
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override float TextScale => MajorBossTextScale * 0.75f;

        public override TextColorData TextColor => new(c =>
        {
            return Color.Lerp(Color.Turquoise, Color.Gray, Sin(AnimationCompletion * 12f + c * Pi * 4f) * 0.5f + 0.5f);
        });

        public override Color ScreenCoverColor => Color.Black;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
