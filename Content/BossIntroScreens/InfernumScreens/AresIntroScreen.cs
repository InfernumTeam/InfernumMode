using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class AresIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new Color(154, 164, 184);

        public override int AnimationTime => 210;

        public override bool TextShouldBeCentered => true;

        public override LocalizedText TextToDisplay => GetLocalizedText(IntroScreenManager.ShouldDisplayJokeIntroText ? "JokeTextToDisplay" : "TextToDisplay");

        public override Effect ShaderToApplyToLetters => InfernumEffectsRegistry.MechsIntroLetterShader.Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(new Vector3(1f, 0.34f, 0.09f));
            shader.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/EternityStreak").Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AresBody>()) && InfernumMode.CanUseCustomAIs;

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("InfernumMode/Assets/Sounds/Custom/ExoMechs/ThanatosTransition");

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.NPCHit4;

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
