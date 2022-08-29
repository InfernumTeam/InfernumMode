using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class AthenaIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new Color(154, 164, 184);

        public override int AnimationTime => 210;

        public override bool TextShouldBeCentered => true;

        public override string TextToDisplay => "The Grand Mastermind\nAthena";

        public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:MechsIntro"].Shader;

        public override void PrepareShader(Effect shader)
        {
            shader.Parameters["uColor"].SetValue(new Vector3(1f, 0.5f, 0.56f));
            shader.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/EternityStreak").Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AthenaNPC>());

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