using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class ScreenSaturationBlurSystem : ModSystem
    {
        public static bool ShouldEffectBeActive
        {
            get;
            set;
        }

        public static float Intensity
        {
            get;
            private set;
        }

        public static RenderTarget2D BloomTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D FinalScreenTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D DownscaledBloomTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D TemporaryAuxillaryTarget
        {
            get;
            private set;
        }

        public const int TotalBlurIterations = 1;

        public const float DownscaleFactor = 16f;

        public const float BlurBrightnessFactor = 3f;

        public const float BlurBrightnessExponent = 1.71f;

        public const float BlurSaturationBiasInterpolant = 0.3f;

        public override void OnModLoad()
        {
            On.Terraria.Main.SetDisplayMode += ResetSaturationMapSize;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += GetFinalScreenShader;
            Main.OnPreDraw += PrepareBlurEffects;
        }

        private void GetFinalScreenShader(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            // Copy the contents of the screen target in the final screen target.
            Main.instance.GraphicsDevice.SetRenderTarget(FinalScreenTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            Main.instance.GraphicsDevice.SetRenderTarget(null);

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        public override void OnModUnload()
        {
            BloomTarget?.Dispose();
            FinalScreenTarget?.Dispose();
            DownscaledBloomTarget?.Dispose();
            TemporaryAuxillaryTarget?.Dispose();
        }
        
        internal static void ResetSaturationMapSize(On.Terraria.Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            if (BloomTarget is not null && (width == BloomTarget.Width && height == BloomTarget.Height))
                return;

            // Free GPU resources for the old targets.
            BloomTarget?.Dispose();
            FinalScreenTarget?.Dispose();
            DownscaledBloomTarget?.Dispose();
            TemporaryAuxillaryTarget?.Dispose();

            // Recreate targets.
            BloomTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            FinalScreenTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            DownscaledBloomTarget = new(Main.instance.GraphicsDevice, (int)(width / DownscaleFactor), (int)(height / DownscaleFactor), true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            TemporaryAuxillaryTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);

            orig(width, height, fullscreen);
        }

        internal static void PrepareBlurEffects(GameTime obj)
        {
            // Bullshit to ensure that the scene effect can always capture, thus preventing very bad screen flash effects.
            if (Intensity > 0f)
                Main.drawToScreen = false;
            else
                return;

            if (InfernumConfig.Instance.SaturationBloomIntensity <= 0f)
                return;
            
            // Get the downscaled texture.
            Main.instance.GraphicsDevice.SetRenderTarget(DownscaledBloomTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            Main.spriteBatch.Draw(FinalScreenTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f / DownscaleFactor, 0, 0f);
            Main.spriteBatch.End();

            // Upscale the texture again.
            Main.instance.GraphicsDevice.SetRenderTarget(BloomTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            Main.spriteBatch.Draw(DownscaledBloomTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, DownscaleFactor, 0, 0f);
            Main.spriteBatch.End();

            // Apply blur iterations.
            for (int i = 0; i < TotalBlurIterations; i++)
            {
                Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget);
                Main.instance.GraphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

                var shader = Filters.Scene["InfernumMode:ScreenSaturationBlur"].GetShader().Shader;
                shader.Parameters["uImageSize1"].SetValue(BloomTarget.Size());
                shader.Parameters["blurMaxOffset"].SetValue(205f);
                shader.CurrentTechnique.Passes["DownsamplePass"].Apply();
                
                Main.spriteBatch.Draw(BloomTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                BloomTarget.CopyContentsFrom(TemporaryAuxillaryTarget);
            }
        }

        public override void PostUpdateEverything()
        {
            // Don't mess with shaders server-side.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Update the intensity in accordance with the effect state.
            bool effectShouldBeActive = ShouldEffectBeActive && InfernumConfig.Instance.SaturationBloomIntensity > 0f;
            Intensity = MathHelper.Clamp(Intensity + effectShouldBeActive.ToDirectionInt() * 0.05f, 0f, 1f);
            
            if (effectShouldBeActive)
            {
                if (!Filters.Scene["InfernumMode:ScreenSaturationBlur"].IsActive())
                    Filters.Scene.Activate("InfernumMode:ScreenSaturationBlur", Main.LocalPlayer.Center);
            }
            else if (Filters.Scene["InfernumMode:ScreenSaturationBlur"].IsActive() && Intensity <= 0f)
                Filters.Scene["InfernumMode:ScreenSaturationBlur"].Deactivate();

            ShouldEffectBeActive = false;
        }
    }
}