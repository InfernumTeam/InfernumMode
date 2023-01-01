using CalamityMod.NPCs.Yharon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class YharonIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Orange;

        public override Color ScreenCoverColor => Color.White;

        public override int AnimationTime => 240;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => true;

        public override string TextToDisplay
        {
            get
            {
                if (IntroScreenManager.ShouldDisplayJokeIntroText)
                    return "Grand\nYharon";

                return "Unwavering Guardian\nYharon";
            }
        }

        public override float TextScale => MajorBossTextScale;

        public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:SCalIntro"].Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(Color.Orange.ToVector3());
            shader.Parameters["uSecondaryColor"].SetValue(Color.Yellow.ToVector3());
            shader.GraphicsDevice.Textures[1] = InfernumTextureRegistry.CultistRayMap.Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Yharon>());

        public override SoundStyle? SoundToPlayWithTextCreation
        {
            get
            {
                if (CachedText == "Grand\nYharon")
                    return new SoundStyle("InfernumMode/Sounds/Custom/YharonRoarTroll");

                return new SoundStyle("CalamityMod/Sounds/Custom/Yharon/YharonRoar");
            }
        }

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.DD2_BetsyFireballShot;

        public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

        public override float LetterDisplayCompletionRatio(int animationTimer)
        {
            float completionRatio = Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

            // If the completion ratio exceeds the point where the name is displayed, display all letters.
            int startOfLargeTextIndex = TextToDisplay.IndexOf('\n');
            int currentIndex = (int)(completionRatio * TextToDisplay.Length);
            if (currentIndex >= startOfLargeTextIndex)
                completionRatio = 1f;

            return completionRatio;
        }
    }
}