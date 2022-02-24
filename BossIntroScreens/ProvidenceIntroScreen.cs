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
        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override Color ScreenCoverColor => Color.White;

        public override string TextToDisplay
        {
            get
            {
                if (!Main.dayTime)
                    return "Providence, the Blaze of Purity";
                return "Providence, the Blaze of Absolution";
            }
        }

        public override Color TextColor
        {
            get
            {
                float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi * 3f) * 0.5f + 0.5f;
                if (!Main.dayTime)
                    return Color.Lerp(new Color(107, 218, 255), new Color(79, 255, 158), colorFadeInterpolant);
                return Color.Lerp(new Color(255, 147, 35), new Color(255, 246, 120), AnimationCompletion);
            }
        }

        public override float TextScale => 1.4f;

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Providence>());

        public override LegacySoundStyle SoundToPlayWithText => null;
    }
}