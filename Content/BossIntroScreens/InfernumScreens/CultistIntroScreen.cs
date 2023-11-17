using CalamityMod;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class CultistIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => CalamityUtils.MulticolorLerp(CalamityUtils.Convert01To010(AnimationCompletion), CultistBehaviorOverride.PillarsPallete);

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.CultistBoss) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
