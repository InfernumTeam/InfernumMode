using System;
using CalamityMod;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class ScreenEffectSystem : ModSystem
    {
        #region Blur
        private static ManagedRenderTarget BlurRenderTarget;

        private static Vector2 BlurPosition;

        private static float BlurIntensity;

        private static int BlurLifeTime;

        private static int BlurTime;

        private static bool BlurActive;

        public const float BaseScaleAmount = 0.04f;
        private const float BaseBlurAmount = 4f;

        private static float BlurLifetimeRatio => (float)BlurTime / BlurLifeTime;

        /// <summary>
        /// Call this to set a blur effect. Any existing ones will be replaced.
        /// </summary>
        /// <param name="position">The focal position, in world co-ordinates</param>
        /// <param name="intensity">How intense to make the scale and blur effect. A 0-1 range should be used</param>
        /// <param name="lifetime">How long the effect should last</param>
        public static void SetBlurEffect(Vector2 position, float intensity, int lifetime)
        {
            if (!CalamityConfig.Instance.Screenshake || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            BlurPosition = position;
            BlurIntensity = intensity;
            BlurLifeTime = lifetime;
            BlurTime = 0;
            BlurActive = true;
        }
        #endregion

        #region Flash
        private static ManagedRenderTarget FlashRenderTarget;

        private static Vector2 FlashPosition;

        private static float FlashIntensity;

        private static int FlashLifeTime;

        private static int FlashTime;

        private static bool FlashActive;

        private static float FlashLifetimeRatio => (float)FlashTime / FlashLifeTime;

        /// <summary>
        /// Call this to set a flash effect. Any existing ones will be replaced.
        /// </summary>
        /// <param name="position">The focal position, in world co-ordinates</param>
        /// <param name="intensity">How bright to make the flash. A 0-1 range should be used</param>
        /// <param name="lifetime">How long the effect should last</param>
        public static void SetFlashEffect(Vector2 position, float intensity, int lifetime)
        {
            if (!CalamityConfig.Instance.Screenshake || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            FlashPosition = position;
            FlashIntensity = intensity;
            FlashLifeTime = lifetime;
            FlashTime = 0;
            FlashActive = true;
        }
        #endregion

        #region Movie Bars
        private static ManagedRenderTarget MovieBarTarget;

        private static bool ScreenBarActive;

        private static int ScreenBarLength;

        private static int ScreenBarTime;

        private static float ScreenBarOffset;

        private static Func<int, float> ScreenBarFadeInterpolant;

        /// <summary>
        /// Call this to set a screen bar effect. Any existing ones will be replaced.
        /// </summary>
        /// <param name="barOffset">How much of the screen the bars should cover. 0-1.
        /// <param name="lifetime">How long the effect should last</param>
        /// <param name="fadeInterpolantFunction">How much of the offset should be present for values of timer passed through.</param>
        public static void SetMovieBarEffect(float barOffset, int lifetime, Func<int, float> fadeInterpolantFunction = null)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            ScreenBarOffset = barOffset;
            ScreenBarLength = lifetime;
            ScreenBarFadeInterpolant = fadeInterpolantFunction ?? new Func<int, float>(timer =>
            Utilities.EaseInOutCubic(Utils.GetLerpValue(0f, 20f, timer, true)) * Utilities.EaseInOutCubic(Utils.GetLerpValue(lifetime, lifetime - 20f, timer, true)));
            ScreenBarTime = 0;
            ScreenBarActive = true;
        }
        #endregion

        public override void Load()
        {
            BlurRenderTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            FlashRenderTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            MovieBarTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
        }

        public static bool AnyBlurOrFlashActive() => BlurActive || FlashActive;

        public override void PostUpdateEverything()
        {
            if (BlurActive)
            {
                if (BlurTime >= BlurLifeTime)
                {
                    BlurActive = false;
                    BlurTime = 0;
                }
                else
                    BlurTime++;
            }

            if (FlashActive)
            {
                if (FlashTime >= FlashLifeTime)
                {
                    FlashActive = false;
                    FlashTime = 0;
                }
                else
                    FlashTime++;
            }

            if (ScreenBarActive)
            {
                if (ScreenBarTime >= ScreenBarLength)
                {
                    ScreenBarActive = false;
                    ScreenBarTime = 0;
                }
                else
                    ScreenBarTime++;
            }

        }

        internal static RenderTarget2D DrawBlurEffect(RenderTarget2D screenTarget1)
        {
            if (BlurActive)
            {
                // Draw the screen contents to the blur render target.
                BlurRenderTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                // Reset the render target.
                screenTarget1.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                // Draw the blur render target 7 times, getting progressively larger and more transparent.
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;

                    // Increase the scale based on the intensity and lifetime of the blur.
                    float scaleAmount = BaseScaleAmount * BlurIntensity;
                    float blurAmount = BaseBlurAmount * BlurIntensity;
                    float scale = 1f + scaleAmount * (1f - BlurLifetimeRatio) * i / blurAmount;
                    Color drawColor = Color.White * 0.42f;
                    // Not doing this causes it to not properly fit on the screen. This extends it to be 100 extra in either direction.
                    Rectangle frameOffset = new(-100, -100, Main.screenWidth + 200, Main.screenHeight + 200);
                    // Use that and the position to set the origin to the draw position.
                    Vector2 origin = BlurPosition + new Vector2(100) - Main.screenPosition;
                    Main.spriteBatch.Draw(BlurRenderTarget.Target, BlurPosition - Main.screenPosition, frameOffset, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }

            // This draws over the blur, so doing them together isn't really ideal.
            else if (FlashActive)
            {
                // Draw the screen contents to the blur render target.
                FlashRenderTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                // Reset the render target.
                screenTarget1.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

                Color drawColor = new(1f, 1f, 1f, Clamp(Lerp(0.5f, 1f, (1f - FlashLifetimeRatio) * FlashIntensity), 0f, 1f));

                // Not doing this causes it to not properly fit on the screen. This extends it to be 100 extra in either direction.
                Rectangle frameOffset = new(-100, -100, Main.screenWidth + 200, Main.screenHeight + 200);
                // Use that and the position to set the origin to the draw position.
                Vector2 origin = FlashPosition + new Vector2(100) - Main.screenPosition;
                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(FlashRenderTarget.Target, FlashPosition - Main.screenPosition, frameOffset, drawColor, 0f, origin, 1f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }

            return DrawMovieBars(screenTarget1);
        }

        private static RenderTarget2D DrawMovieBars(RenderTarget2D screenTarget1)
        {
            if (!ScreenBarActive)
                return screenTarget1;

            MovieBarTarget.SwapToRenderTarget();

            Effect barShader = InfernumEffectsRegistry.MovieBarShader.GetShader().Shader;

            float fade = ScreenBarFadeInterpolant(ScreenBarTime);
            barShader.Parameters["barSize"]?.SetValue(ScreenBarOffset * fade);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, barShader);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            return MovieBarTarget;
        }
    }
}
