using CalamityMod.NPCs.OldDuke;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

using TMLSoundType = Terraria.ModLoader.SoundType;

namespace InfernumMode.BossIntroScreens
{
    public class OldDukeIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            float limeColorInterpolant = Utils.InverseLerp(0.77f, 1f, (float)Math.Sin(AnimationCompletion * -MathHelper.Pi * 4f + completionRatio * MathHelper.Pi) * 0.5f + 0.5f);
            Color skinColor = new Color(113, 90, 71);
            Color irradiatedColor = new Color(170, 216, 15);
            return Color.Lerp(skinColor, irradiatedColor, limeColorInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay
        {
            get
            {
                if (IntroScreenManager.ShouldDisplayJokeIntroText)
                    return "Speed Demon\nThe Old Duke";

                return "Sulpuric Terror\nThe Old Duke";
            }
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<OldDuke>());

        // Sounds are played in the Old Duke's AI.
        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}