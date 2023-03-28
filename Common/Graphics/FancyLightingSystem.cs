using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class FancyLightingSystem : ModSystem
    {
        // Used to store the original screen as that RT is used as a temp buffer.
        private static RenderTarget2D ScreenCaptureTarget;
        // Used to store the main screen with the light shader applied.
        private static RenderTarget2D LightRenderTarget;
        // Used to store the shadow information.
        private static RenderTarget2D ShadowRenderTarget;
        // Used to store bloom information.
        private static RenderTarget2D BloomRenderTarget;

        private static RenderTarget2D TempBloomTarget;

        public override void Load()
        {
            Main.OnResolutionChanged += ResizeRenderTargets;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += DrawRTStuff;
        }

        public override void Unload()
        {
            Main.OnResolutionChanged -= ResizeRenderTargets;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= DrawRTStuff;
            DisposeOfTargets();
        }

        private void ResizeRenderTargets(Vector2 obj)
        {
            // Ensure it is correctly disposed.
            if (LightRenderTarget != null && !LightRenderTarget.IsDisposed)
                LightRenderTarget.Dispose();

            LightRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // Ensure it is correctly disposed.
            if (ScreenCaptureTarget != null && !ScreenCaptureTarget.IsDisposed)
                ScreenCaptureTarget.Dispose();

            ScreenCaptureTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // Ensure it is correctly disposed.
            if (ShadowRenderTarget != null && !ShadowRenderTarget.IsDisposed)
                ShadowRenderTarget.Dispose();

            ShadowRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // Ensure it is correctly disposed.
            if (BloomRenderTarget != null && !BloomRenderTarget.IsDisposed)
                BloomRenderTarget.Dispose();

            BloomRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            // Ensure it is correctly disposed.
            if (TempBloomTarget != null && !TempBloomTarget.IsDisposed)
                TempBloomTarget.Dispose();

            TempBloomTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }

        private void DrawRTStuff(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Microsoft.Xna.Framework.Color clearColor)
        {
            // This has its own config due to being mildly demanding.
            if (!InfernumConfig.Instance.FancyLighting)
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                return;
            }

            // Ensure these are all set.
            if (LightRenderTarget == null || ScreenCaptureTarget == null || ShadowRenderTarget == null || BloomRenderTarget == null || TempBloomTarget == null)
                ResizeRenderTargets(Vector2.Zero);

            // Save the current screen. This will be redrawn later.
            ScreenCaptureTarget.SwapToRenderTarget(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // Do the lighting.
            DrawLighting(Main.spriteBatch, screenTarget1, out var lightIntensity);

            // Do shadows.
            DrawShadows(Main.spriteBatch, screenTarget1, lightIntensity);

            // Do the final drawing.
            DoFinalDrawing(Main.spriteBatch, screenTarget1);

            // Draw Bloom.
            DoBloomEffect(Main.spriteBatch, screenTarget1);
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        private static void DrawLighting(SpriteBatch spriteBatch, RenderTarget2D screenTarget, out float intensity)
        {
            // Swap to the main lighting RT.
            LightRenderTarget.SwapToRenderTarget(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            // Set the shader params.
            Effect lighting = InfernumEffectsRegistry.BasicLightingShader.GetShader().Shader;
            // Set the gradient texture to sample from.
            lighting.Parameters["mainTexture"].SetValue(Main.dayTime ? InfernumTextureRegistry.DayGradient.Value : InfernumTextureRegistry.NightGradient.Value);
            // Set the correct resolution.
            lighting.Parameters["screenResolution"].SetValue(new Vector2(screenTarget.Width, screenTarget.Height));
            // Get the current sun position, and scale it by the resolution.
            lighting.Parameters["sunPosition"].SetValue(GetSunPosition(screenTarget) / new Vector2(screenTarget.Width, screenTarget.Height));
            // Get the correct max time for the time of day/night.
            lighting.Parameters["time"].SetValue((float)Main.time / (Main.dayTime ? 54000f : 32400f));
            // Get an intensity based on the current sky color. These numbers can be anything that looks nice.
            Color skyColor = Main.ColorOfTheSkies;
            intensity = (skyColor.R * 0.3f + skyColor.G * 0.6f + skyColor.B * 0.1f) / 255f;

            // Modify the intensity based on the current biome. Not doing this causes it to look pretty bad in them.
            ModifyIntensityBasedOnBiomes(ref intensity);

            // Shrink it a bit.
            intensity *= 0.75f;

            if ((Main.LocalPlayer.Center.Y / 16f) > Main.worldSurface - 150.0)
            {
                float distance = Main.LocalPlayer.Center.Y / 16f - ((float)Main.worldSurface - 150f);
                intensity *= 1f - distance / 600f;
            }

            // Night time should be a bit dimmer.
            lighting.Parameters["intensity"].SetValue(Main.dayTime ? intensity : 0.8f);
            // Apply the shader
            lighting.CurrentTechnique.Passes[0].Apply();

            // Draw a white rectangle over the screen, which will have the shader applied.
            Texture2D pixel = InfernumTextureRegistry.Pixel.Value;
            spriteBatch.Draw(pixel, new Rectangle(0, 0, screenTarget.Width, screenTarget.Height), Color.White);
            spriteBatch.End();
            if (!Main.dayTime)
                intensity = 0.8f;
        }

        private static void DrawShadows(SpriteBatch spriteBatch, RenderTarget2D screenTarget, float lightIntensity)
        {
            // Swap to the shadow RT, and draw a copy of the screen RT.
            // The way this works, is it then draws this multiple times with increasing sizes to create convincing fake shadows.
            ShadowRenderTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            Effect shadow = InfernumEffectsRegistry.ShadowShader.GetShader().Shader;

            // Get the threshold for the shader to use.
            float shadowThreshold = Main.bgAlphaFrontLayer[2] * 0.09f;

            // Use a set threshold at night time due to everything being dark.
            shadow.Parameters["threshold"].SetValue(Main.dayTime ? 1f - shadowThreshold : 0.03f);
            shadow.CurrentTechnique.Passes[0].Apply();

            // Draw the screen.
            spriteBatch.Draw(screenTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Swap to the screen target temporarily, and draw the shadow target several times additively at increasing scales to get accurate dark zones to use as shadows.
            screenTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 drawPosOrigin = GetSunPosition(screenTarget);

            // At day.
            if (Main.dayTime)
            {
                for (int i = 0; i < 30; i++)
                {
                    Color color = Color.White;
                    float alphaModifer = 0.55f * (30f - i) / 90f;
                    color.A = (byte)(color.A * alphaModifer * (1.1f * (lightIntensity * 0.5f - 0.1f)));
                    float scale = 1f + i * 0.011322f;
                    spriteBatch.Draw(ShadowRenderTarget, drawPosOrigin, null, color, 0f, drawPosOrigin, scale, SpriteEffects.None, 0f);
                }
            }

            // At night.
            else
            {
                for (int i = 0; i < 20; i++)
                {
                    Color color = Color.White;
                    float alpha = (20f - i) / 150f;
                    float scale = (1f + i * 0.01f);
                    spriteBatch.Draw(ShadowRenderTarget, drawPosOrigin, null, color * alpha, 0f, drawPosOrigin, scale, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.End();

            // Draw the shadows to the actual shadow render target.
            ShadowRenderTarget.SwapToRenderTarget(Color.Black);          
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void DoFinalDrawing(SpriteBatch spriteBatch, RenderTarget2D screenTarget)
        {
            // Reset the base screen target to the captured one before any changes happen.
            screenTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(ScreenCaptureTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Over the top of this additively, draw the lighting RT with the shadow shader applied passing through
            // the prepared shadow RT. This adds the shadows to the main lighting.
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Effect shadowShader = InfernumEffectsRegistry.ShadowShader.GetShader().Shader;
            shadowShader.Parameters["mainTexture"].SetValue(ShadowRenderTarget);
            shadowShader.CurrentTechnique.Passes[1].Apply();
            spriteBatch.Draw(LightRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void DoBloomEffect(SpriteBatch spriteBatch, RenderTarget2D screenTarget)
        {
            // Store the current screen in the shadow RT.
            ShadowRenderTarget.SwapToRenderTarget(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
            
            // Swap to the now redundant lighting target, and draw the original screen with a filter shader.
            LightRenderTarget.SwapToRenderTarget(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            var shader = InfernumEffectsRegistry.ScreenSaturationBlurScreenShader.GetShader().Shader;
            shader.Parameters["prefilteringThreshold"].SetValue(0.9f);
            shader.CurrentTechnique.Passes["PrefilteringPass"].Apply();
            spriteBatch.Draw(ScreenCaptureTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Swap to the bloom target and draw the filtered RT with the blur effect.
            BloomRenderTarget.SwapToRenderTarget(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            shader.Parameters["uImageSize1"].SetValue(LightRenderTarget.Size());
            shader.Parameters["blurMaxOffset"].SetValue(10f);
            shader.CurrentTechnique.Passes["DownsamplePass"].Apply();
            spriteBatch.Draw(LightRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Swap to the temp target, and draw the bloom.
            TempBloomTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            Main.instance.GraphicsDevice.Textures[1] = BloomRenderTarget;

            // Get the intensity and use that for the blur amount.
            Color skyColor = Main.ColorOfTheSkies;
            float intensity = Main.dayTime ? (skyColor.R * 0.3f + skyColor.G * 0.6f + skyColor.B * 0.1f) / 255f * 0.75f : 0.8f;
            shader.Parameters["maxSaturationAdditive"].SetValue(intensity);
            shader.Parameters["blurExponent"].SetValue(1.53f);

            float saturationBias = 0.3f;
            float brightness = intensity * 4.5f;
            shader.Parameters["blurAdditiveBrightness"].SetValue(brightness);
            shader.Parameters["blurSaturationBiasInterpolant"].SetValue(saturationBias);
            shader.Parameters["onlyShowBlurMap"].SetValue(false);
            shader.CurrentTechnique.Passes["ScreenPass"].Apply();
            spriteBatch.Draw(ShadowRenderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Swap to the main screen target and draw both the original screen and the bloomed one.
            screenTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            spriteBatch.Draw(ShadowRenderTarget, Vector2.Zero, Color.White * 0.65f);
            spriteBatch.Draw(TempBloomTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static Vector2 GetSunPosition(RenderTarget2D rt)
        {
            float midTime = (Main.dayTime ? 27000 : 16200);
            float posOffset = (0f - Main.screenPosition.Y) / (float)(Main.worldSurface * 16.0 - 600.0) * 200f;
            float timeRatio = 1f - (float)Main.time / midTime;
            float xPos = (1f - timeRatio) * (float)Main.screenWidth / 2f - 100f * timeRatio;
            float tSquared = timeRatio * timeRatio;
            float yPos = posOffset + tSquared * 250f + 180f;

            // Account for gravitation potions.
            if (Main.LocalPlayer != null && Main.LocalPlayer.gravDir == -1f)
                return new Vector2(xPos, rt.Height - yPos);
            return new Vector2(xPos, yPos);
        }

        private static void DisposeOfTargets()
        {
            // Ensure it is correctly disposed.
            if (LightRenderTarget != null && !LightRenderTarget.IsDisposed)
                LightRenderTarget.Dispose();

            LightRenderTarget = null;

            // Ensure it is correctly disposed.
            if (ScreenCaptureTarget != null && !ScreenCaptureTarget.IsDisposed)
                ScreenCaptureTarget.Dispose();

            ScreenCaptureTarget = null;

            // Ensure it is correctly disposed.
            if (ShadowRenderTarget != null && !ShadowRenderTarget.IsDisposed)
                ShadowRenderTarget.Dispose();

            ShadowRenderTarget = null;

            // Ensure it is correctly disposed.
            if (BloomRenderTarget != null && !BloomRenderTarget.IsDisposed)
                BloomRenderTarget.Dispose();

            BloomRenderTarget = null;

            // Ensure it is correctly disposed.
            if (TempBloomTarget != null && !TempBloomTarget.IsDisposed)
                TempBloomTarget.Dispose();

            TempBloomTarget = null;
        }

        private static void ModifyIntensityBasedOnBiomes(ref float intensity)
        {
            if (Main.LocalPlayer.ZoneSnow && !Main.LocalPlayer.ZoneCrimson && !Main.LocalPlayer.ZoneCorrupt)
                intensity -= 0.1f;
            if (Main.LocalPlayer.ZoneCrimson)
                intensity += 0.2f;
            if (Main.snowBG[0] is 263 or 258 or 267)
                intensity -= 1f;
            if (Main.snowBG[0] == 263)
                intensity -= 0.5f;
            if (Main.desertBG[0] == 248)
                intensity -= 0.9f;
        }
    }
}