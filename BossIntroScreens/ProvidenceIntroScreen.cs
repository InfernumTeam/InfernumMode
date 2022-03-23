using CalamityMod.NPCs.Providence;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class ProvidenceIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(_ =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 3f) * 0.5f + 0.5f;
            if (!Main.dayTime)
                return Color.Lerp(new Color(107, 218, 255), new Color(79, 255, 158), colorFadeInterpolant);
            return Color.Lerp(new Color(255, 147, 35), new Color(255, 246, 120), AnimationCompletion);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override Color ScreenCoverColor => Color.White;

        public override string TextToDisplay
        {
            get
            {
                if (!Main.dayTime)
                    return "The Blaze of Purity\nProvidence";
                return "The Blaze of Absolution\nProvidence";
            }
        }

        public override float TextScale => MajorBossTextScale;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Providence>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}