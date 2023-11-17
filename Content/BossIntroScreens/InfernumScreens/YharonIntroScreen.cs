using CalamityMod.NPCs.Yharon;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class YharonIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.Orange;

        public override Color ScreenCoverColor => Color.White;

        public override int AnimationTime => 240;

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override LocalizedText TextToDisplay => GetLocalizedText(IntroScreenManager.ShouldDisplayJokeIntroText || Utilities.IsAprilFirst() ? "JokeTextToDisplay" : "TextToDisplay");

        public override float TextScale => MajorBossTextScale;

        public override Effect ShaderToApplyToLetters => InfernumEffectsRegistry.SCalIntroLetterShader.Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(Color.Orange.ToVector3());
            shader.Parameters["uSecondaryColor"].SetValue(Color.Yellow.ToVector3());
            shader.GraphicsDevice.Textures[1] = InfernumTextureRegistry.CultistRayMap.Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Yharon>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation
        {
            get
            {
                if (CachedText == "Grand\nYharon")
                    return new SoundStyle("InfernumMode/Assets/Sounds/Custom/Yharon/YharonRoarTroll");

                return null;
            }
        }

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.DD2_BetsyFireballShot;

        public override bool CanPlaySound => LetterDisplayCompletionRatio(AnimationTimer) >= 1f;

        public override float LetterDisplayCompletionRatio(int animationTimer)
        {
            float completionRatio = Utils.GetLerpValue(TextDelayInterpolant, 0.92f, animationTimer / (float)AnimationTime, true);

            // If the completion ratio exceeds the point where the name is displayed, display all letters.
            int startOfLargeTextIndex = TextToDisplay.Value.IndexOf('\n');
            int currentIndex = (int)(completionRatio * TextToDisplay.Value.Length);
            if (currentIndex >= startOfLargeTextIndex)
                completionRatio = 1f;

            return completionRatio;
        }
    }
}
