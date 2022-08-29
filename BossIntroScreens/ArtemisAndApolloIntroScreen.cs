using CalamityMod.NPCs.ExoMechs.Apollo;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class ArtemisAndApolloIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new Color(125, 156, 165);

        public override int AnimationTime => 210;

        public override bool TextShouldBeCentered => true;

        public override string TextToDisplay => "The Supreme Hunters\nArtemis and Apollo";

        public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:MechsIntro"].Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(new Vector3(0.12f, 0.86f, 0.52f));
            shader.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/DiagonalGleam").Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Apollo>());

        public override SoundStyle? SoundToPlayWithTextCreation => new SoundStyle("InfernumMode/Sounds/Custom/ThanatosTransition");

        public override SoundStyle? SoundToPlayWithLetterAddition => SoundID.NPCHit4;

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