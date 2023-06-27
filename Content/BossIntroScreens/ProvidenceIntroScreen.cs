using CalamityMod.NPCs.Providence;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    public class ProvidenceIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new(_ =>
        {
            float colorFadeInterpolant = Sin(AnimationCompletion * Pi * 3f) * 0.5f + 0.5f;
            if (ProvidenceBehaviorOverride.IsEnraged)
                return Color.Lerp(new Color(107, 218, 255), new Color(79, 255, 158), colorFadeInterpolant);
            return Color.Lerp(new Color(255, 147, 35), new Color(255, 246, 120), AnimationCompletion);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay
        {
            get
            {
                if (ProvidenceBehaviorOverride.IsEnraged)
                    return "The Blaze of Purity\nProvidence";
                return "The Blaze of Absolution\nProvidence";
            }
        }

        public override float TextScale => MajorBossTextScale;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Providence>());

        public override SoundStyle? SoundToPlayWithTextCreation => null;
    }
}