using InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class EmpressOfLightIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(completionRatio =>
        {
            float period = Main.dayTime ? 0.3f : 1f;
            float rainbowInterpolant = Sin(completionRatio * Pi * 4f + AnimationCompletion * period * TwoPi) * 0.5f + 0.5f;

            if (!Main.dayTime)
                return Main.hslToRgb(rainbowInterpolant, 1f, 0.64f);
            return EmpressOfLightBehaviorOverride.GetDaytimeColor(rainbowInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override bool ShouldBeActive() => NPC.AnyNPCs(NPCID.HallowBoss) && !NPC.AnyNPCs(NPCID.EmpressButterfly) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}
