using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BossIntroScreens
{
    public class DraedonIntroScreen : BaseIntroScreen
    {
        public override TextColorData TextColor => new TextColorData(completionRatio =>
        {
            float colorFadeInterpolant = (float)Math.Sin(AnimationCompletion * MathHelper.Pi + completionRatio * MathHelper.Pi * 6f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Silver, Color.Gold, colorFadeInterpolant);
        });

        public override bool TextShouldBeCentered => true;

        public override bool ShouldCoverScreen => false;

        public override string TextToDisplay => "The Cosmic Engineer\nDraedon";

        public override Effect ShaderToApplyToLetters => GameShaders.Misc["Infernum:MechsIntro"].Shader;

        public override void PrepareShader(Effect shader)
        {
            Color color = CalamityUtils.MulticolorLerp((float)Math.Cos(Main.GlobalTime * 1.6f) * 0.5f + 0.5f, CalamityUtils.ExoPalette);
            shader.Parameters["uColor"].SetValue(color.ToVector3());
            shader.GraphicsDevice.Textures[1] = ModContent.GetTexture("InfernumMode/ExtraTextures/DiagonalGleam");
        }

        public override bool ShouldBeActive() => NPC.AnyNPCs(ModContent.NPCType<Draedon>());

        public override LegacySoundStyle SoundToPlayWithTextCreation => null;
    }
}