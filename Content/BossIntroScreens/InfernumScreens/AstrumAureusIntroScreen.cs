using CalamityMod.NPCs.AstrumAureus;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens.InfernumScreens
{
    public class AstrumAureusIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => Color.White;

        public override int AnimationTime => 210;

        public override bool TextShouldBeCentered => true;

        public override Effect ShaderToApplyToLetters => InfernumEffectsRegistry.MechsIntroLetterShader.Shader;

        public override void PrepareShader(Effect shader)
        {
            Color gleamColor = Color.Lerp(new Color(255, 164, 94), new Color(109, 242, 196), Cos(Main.GlobalTimeWrappedHourly * 6f) * 0.5f + 0.5f);
            shader.Parameters["uColor"].SetValue(gleamColor.ToVector3());
            shader.GraphicsDevice.Textures[1] = InfernumTextureRegistry.DiagonalGleam.Value;
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<AstrumAureus>()) && InfernumMode.CanUseCustomAIs;

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
