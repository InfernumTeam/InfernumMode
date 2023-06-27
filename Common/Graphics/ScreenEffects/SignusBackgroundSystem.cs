using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class SignusBackgroundSystem : ModSystem
    {
        public static float Intensity
        {
            get;
            set;
        }

        public override void PostUpdateNPCs()
        {
            float maxIntensity = 1f;
            if (CalamityGlobalNPC.signus != -1 && Main.npc[CalamityGlobalNPC.signus].ai[1] == 0f)
                maxIntensity = Pow(Main.npc[CalamityGlobalNPC.signus].Infernum().ExtraAI[9], 2.4f);

            Intensity = Clamp(Intensity + (CalamityGlobalNPC.signus != -1).ToDirectionInt() * 0.03f, 0f, maxIntensity);
        }

        public static void Draw()
        {
            // Don't do anything if the effect is not active.
            if (Intensity <= 0f)
                return;

            // Prepare the shader.
            Main.spriteBatch.End();

            Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
            transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * new Vector3(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var backgroundShader = InfernumEffectsRegistry.SignusBackgroundShader;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            backgroundShader.Shader.Parameters["vortexSwirlSpeed"].SetValue(-2.33f);
            backgroundShader.Shader.Parameters["vortexSwirlDetail"].SetValue(67f);
            backgroundShader.Shader.Parameters["vortexEdgeFadeFactor"].SetValue(14f);
            backgroundShader.SetShaderTexture(InfernumTextureRegistry.WavyNoise);
            backgroundShader.UseColor(Color.Lerp(Color.Purple, Color.Black, 0.6f));
            backgroundShader.UseSecondaryColor(Color.White);
            backgroundShader.UseShaderSpecificData(new Vector4(screenArea.Y, screenArea.X, 0f, 0f));
            backgroundShader.Apply();

            Texture2D pixel = InfernumTextureRegistry.Pixel.Value;
            Vector2 textureArea = screenArea / pixel.Size();
            Main.spriteBatch.Draw(pixel, Vector2.Zero, null, Color.White * Intensity, 0f, Vector2.Zero, textureArea, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, transformationMatrix);
        }
    }
}