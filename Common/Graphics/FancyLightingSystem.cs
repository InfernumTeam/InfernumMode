using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
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
        private static RenderTarget2D DownscaledBloomTarget;
        // Temp target to perform bloom effects to.
        private static RenderTarget2D TempBloomTarget;

        private static RenderTarget2D RaymarchingOccludersTarget;

        private static RenderTarget2D RaymarchingLightsTarget;

        private static RenderTarget2D RaymarchingVoronoiTarget;

        private static RenderTarget2D RaymarchingOccluderDisplacementFieldTarget;

        private static RenderTarget2D RaymarchingLightDisplacementFieldTarget;

        private static RenderTarget2D TempRaymarchingDisplacementFieldTarget;


        private static int BloomPasses => InfernumConfig.Instance.ReducedGraphicsConfig ? 3 : 6;

        private static bool UseRaymarching = false;

        public override void Load()
        {
            Main.OnResolutionChanged += ResizeRenderTargets;
        }

        public override void Unload()
        {
            Main.OnResolutionChanged -= ResizeRenderTargets;
            DisposeOfTargets();
        }

        private static void ResizeRenderTargets(Vector2 obj)
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
            if (DownscaledBloomTarget != null && !DownscaledBloomTarget.IsDisposed)
                DownscaledBloomTarget.Dispose();

            DownscaledBloomTarget = new(Main.instance.GraphicsDevice, (int)(Main.screenWidth * 0.5f), (int)(Main.screenHeight * 0.5f));
            // Ensure it is correctly disposed.
            if (TempBloomTarget != null && !TempBloomTarget.IsDisposed)
                TempBloomTarget.Dispose();

            TempBloomTarget = new(Main.instance.GraphicsDevice, (int)(Main.screenWidth * 0.5f), (int)(Main.screenHeight * 0.5f));

            if (RaymarchingOccludersTarget != null && !RaymarchingOccludersTarget.IsDisposed)
                RaymarchingOccludersTarget.Dispose();
            RaymarchingOccludersTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            if (RaymarchingLightsTarget != null && !RaymarchingLightsTarget.IsDisposed)
                RaymarchingLightsTarget.Dispose();
            RaymarchingLightsTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            if (RaymarchingOccluderDisplacementFieldTarget != null && !RaymarchingOccluderDisplacementFieldTarget.IsDisposed)
                RaymarchingOccluderDisplacementFieldTarget.Dispose();
            RaymarchingOccluderDisplacementFieldTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            if (RaymarchingLightDisplacementFieldTarget != null && !RaymarchingLightDisplacementFieldTarget.IsDisposed)
                RaymarchingLightDisplacementFieldTarget.Dispose();
            RaymarchingLightDisplacementFieldTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            if (TempRaymarchingDisplacementFieldTarget != null && !TempRaymarchingDisplacementFieldTarget.IsDisposed)
                TempRaymarchingDisplacementFieldTarget.Dispose();
            TempRaymarchingDisplacementFieldTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);

            if (RaymarchingVoronoiTarget != null && !RaymarchingVoronoiTarget.IsDisposed)
                RaymarchingVoronoiTarget.Dispose();
            RaymarchingVoronoiTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }

        internal static RenderTarget2D DrawRTStuff(RenderTarget2D screenTarget1)
        {
            // This has its own config due to being mildly demanding.
            if (!InfernumConfig.Instance.FancyLighting)
                return screenTarget1;

            // Ensure these are all set.
            if (LightRenderTarget == null || ScreenCaptureTarget == null || ShadowRenderTarget == null || DownscaledBloomTarget == null
                || TempBloomTarget == null || RaymarchingOccludersTarget == null || RaymarchingLightsTarget == null || RaymarchingOccluderDisplacementFieldTarget == null
                || RaymarchingLightDisplacementFieldTarget == null || TempRaymarchingDisplacementFieldTarget == null || RaymarchingVoronoiTarget == null)
                ResizeRenderTargets(Vector2.Zero);

            UseRaymarching = false;

            // This is unfinished, and it's unlikely i will finish it. It will remain here inactive in the code unless/until i decide to remove it.
            if (UseRaymarching)
            {
                DoRaymarching(Main.spriteBatch, screenTarget1);
                return screenTarget1;
            }

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

            // Draw Bloom, only if the saturation bloom is not active.
            if (!ScreenSaturationBlurSystem.ShouldEffectBeActive)
                DoBloomEffect(Main.spriteBatch, screenTarget1);

            return screenTarget1;
        }

        private static void DoRaymarching(SpriteBatch spriteBatch, RenderTarget2D screenTarget1)
        {
            // For testing purposes, setup the two RTs
            RaymarchingOccludersTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(InfernumTextureRegistry.BlurryPerlinNoise.Value, new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.2f), Color.White);
            spriteBatch.Draw(InfernumTextureRegistry.VolcanoWarning.Value, new Vector2(Main.screenWidth * 0.8f, Main.screenHeight * 0.3f), Color.White);
            spriteBatch.End();

            RaymarchingLightsTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(TextureAssets.Sun.Value, GetSunPosition(screenTarget1), Color.White);
            spriteBatch.End();

            // Create the distance fields
            SetupDistanceField(spriteBatch, true);
            SetupDistanceField(spriteBatch, false);

            // Raymarch.
            screenTarget1.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            //spriteBatch.Draw(RaymarchingLightDisplacementFieldTarget, Vector2.Zero, Color.White);
            //spriteBatch.End();
            //return;
            Effect raymarchShader = InfernumEffectsRegistry.RaymarchingShader.GetShader().Shader;

            raymarchShader.Parameters["lightsTexture"].SetValue(RaymarchingLightDisplacementFieldTarget);
            raymarchShader.Parameters["noiseTexure"].SetValue(InfernumTextureRegistry.BlurryPerlinNoise.Value);
            raymarchShader.Parameters["screenTexture"].SetValue(RaymarchingOccludersTarget);
            raymarchShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            raymarchShader.Parameters["screenResolution"].SetValue(screenTarget1.Size());
            raymarchShader.Parameters["rEmissionMultiplier"].SetValue(1f);
            raymarchShader.Parameters["rDistanceMod"].SetValue(1f);
            raymarchShader.CurrentTechnique.Passes["RaymarchPass"].Apply();

            spriteBatch.Draw(RaymarchingOccluderDisplacementFieldTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
            // Draw them over the top of the scene.
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(RaymarchingOccludersTarget, Vector2.Zero, Color.White);
            spriteBatch.Draw(RaymarchingLightsTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void SetupDistanceField(SpriteBatch spriteBatch, bool forOccluders)
        {
            Effect jumpFlood = InfernumEffectsRegistry.JumpFloodShader.GetShader().Shader;

            // Setup the voronoi texture.
            RaymarchingVoronoiTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            jumpFlood.Parameters["screenResolution"].SetValue(RaymarchingOccludersTarget.Size());
            jumpFlood.CurrentTechnique.Passes["VoronoiPass"].Apply();
            if (forOccluders)
                spriteBatch.Draw(RaymarchingOccludersTarget, Vector2.Zero, Color.White);
            else
                spriteBatch.Draw(RaymarchingLightsTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            if (forOccluders)
                RaymarchingOccluderDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
            else
                RaymarchingLightDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin();
            spriteBatch.Draw(RaymarchingVoronoiTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // The number of passes required is the log2 of the largest viewport dimension rounded up to the nearest power of 2.
            var passes = MathF.Log(MathF.Max(RaymarchingOccludersTarget.Size().X, RaymarchingOccludersTarget.Size().Y) / MathF.Log(2));
            passes = 12;
            // Create the voronoi image.
            for (int i = 0; i < passes; i++)
            {
                // The offset for each pass is half the previous one, starting at half the square resolution rounded up to nearest power 2.
                float offset = MathF.Pow(2, passes - i - 1);
                if (i % 2 == 0)
                {
                    if (forOccluders)
                        RaymarchingOccluderDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
                    else
                        RaymarchingLightDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
                }
                else
                    TempRaymarchingDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                jumpFlood.Parameters["jfimageResolution"].SetValue(TempRaymarchingDisplacementFieldTarget.Size());
                jumpFlood.Parameters["jfOffset"].SetValue(offset);
                jumpFlood.CurrentTechnique.Passes["JumpFloodPass"].Apply();

                if (i == 0)
                    spriteBatch.Draw(RaymarchingVoronoiTarget, Vector2.Zero, Color.White);
                else if (i % 2 == 0)
                    spriteBatch.Draw(TempRaymarchingDisplacementFieldTarget, Vector2.Zero, Color.White);
                else
                {
                    if (forOccluders)
                        spriteBatch.Draw(RaymarchingOccluderDisplacementFieldTarget, Vector2.Zero, Color.White);
                    else
                        spriteBatch.Draw(RaymarchingLightDisplacementFieldTarget, Vector2.Zero, Color.White);
                }
                spriteBatch.End();
            }
            Effect mapShader = InfernumEffectsRegistry.DisplacementMap.GetShader().Shader;
            // Turn it into a distance map.
            if (forOccluders)
                RaymarchingOccluderDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
            else
                RaymarchingLightDisplacementFieldTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            mapShader.Parameters["dDistanceModifier"].SetValue(1f);
            mapShader.CurrentTechnique.Passes["DisplacementPass"].Apply();
            spriteBatch.Draw(TempRaymarchingDisplacementFieldTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void DrawLighting(SpriteBatch spriteBatch, RenderTarget2D screenTarget, out float intensity)
        {
            // Swap to the main lighting RT.
            LightRenderTarget.SwapToRenderTarget(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
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

            // Shrink it considerably so it is not a flashbang.
            intensity *= 0.5f;

            if ((Main.LocalPlayer.Center.Y / 16f) > Main.worldSurface - 150.0)
            {
                float distance = Main.LocalPlayer.Center.Y / 16f - ((float)Main.worldSurface - 150f);
                intensity *= 1f - distance / 600f;
            }

            // Night time should be a bit dimmer.
            lighting.Parameters["intensity"].SetValue(Main.dayTime ? intensity : 0.75f);
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
            screenTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 drawPosOrigin = GetSunPosition(screenTarget);

            // At day.
            if (Main.dayTime)
            {
                for (int i = 0; i < 30; i++)
                {
                    Color color = Color.White;
                    float alphaModifer = 0.55f * (30f - i) / 80;
                    color.A = (byte)(color.A * alphaModifer * (1.1f * (lightIntensity * 0.5f - 0.1f)));
                    float scale = 1f + i * 0.005f;
                    spriteBatch.Draw(ShadowRenderTarget, drawPosOrigin, null, color, 0f, drawPosOrigin, scale, SpriteEffects.None, 0f);
                }
            }

            // At night.
            else
            {
                for (int i = 0; i < 20; i++)
                {
                    Color color = Color.White;
                    float alpha = (20f - i) / 120f;
                    float scale = (1f + i * 0.005f);
                    spriteBatch.Draw(ShadowRenderTarget, drawPosOrigin, null, color * alpha, 0f, drawPosOrigin, scale, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.End();

            // Draw the shadows to the actual shadow render target.
            ShadowRenderTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void DoFinalDrawing(SpriteBatch spriteBatch, RenderTarget2D screenTarget)
        {
            // Reset the base screen target to the captured one before any changes happen.
            screenTarget.SwapToRenderTarget(Color.Transparent);
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
            // Swap to the downscaled bloom target and store a filtered pass of the screen.
            DownscaledBloomTarget.SwapToRenderTarget(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Effect bloomEffect = InfernumEffectsRegistry.BloomShader.GetShader().Shader;
            bloomEffect.Parameters["filterThreshold"].SetValue(0.99f);
            bloomEffect.CurrentTechnique.Passes["FilterPass"].Apply();
            // Draw the screen to it at half size. This "downscales" it and creates a better bloom effect when upscaling it.
            spriteBatch.Draw(ScreenCaptureTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Perform blurring.
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            bloomEffect.Parameters["textureSize"].SetValue(DownscaledBloomTarget.Size());

            // This performs multiple passes of blurring. More = better quality but also more performance costly.
            for (int i = 0; i < BloomPasses; i++)
            {
                // Swap to the temp target.
                TempBloomTarget.SwapToRenderTarget(Color.Transparent);

                // Perform the horizontal pass first.
                bloomEffect.Parameters["horizontal"].SetValue(true);
                bloomEffect.CurrentTechnique.Passes["BlurPass"].Apply();
                spriteBatch.Draw(DownscaledBloomTarget, Vector2.Zero, Color.White);

                // Then draw it to the main target and do the vertical pass. Ensure the already blurred one is drawn through.
                DownscaledBloomTarget.SwapToRenderTarget(Color.Transparent);
                bloomEffect.Parameters["horizontal"].SetValue(false);
                bloomEffect.CurrentTechnique.Passes["BlurPass"].Apply();
                spriteBatch.Draw(TempBloomTarget, Vector2.Zero, Color.White);
            }
            spriteBatch.End();

            // Swap back to the screen target, and perform the bloom blending.
            LightRenderTarget.SwapToRenderTarget(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            bloomEffect.Parameters["bloomScene"].SetValue(DownscaledBloomTarget);
            bloomEffect.Parameters["downsampledSize"].SetValue(1f);
            bloomEffect.Parameters["bloomIntensity"].SetValue(1.1f);
            bloomEffect.CurrentTechnique.Passes["BloomPass"].Apply();
            spriteBatch.Draw(screenTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0);
            spriteBatch.End();

            screenTarget.SwapToRenderTarget();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            spriteBatch.Draw(LightRenderTarget, Vector2.Zero, Color.White);
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
            Main.RunOnMainThread(() =>
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
                if (DownscaledBloomTarget != null && !DownscaledBloomTarget.IsDisposed)
                    DownscaledBloomTarget.Dispose();

                DownscaledBloomTarget = null;

                // Ensure it is correctly disposed.
                if (TempBloomTarget != null && !TempBloomTarget.IsDisposed)
                    TempBloomTarget.Dispose();

                TempBloomTarget = null;
            });
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