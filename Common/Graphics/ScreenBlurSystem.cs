using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class ScreenBlurSystem : ModSystem
    {
        private static RenderTarget2D BlurRenderTarget;

        private static Vector2 Position;

        private static float Intensity;

        private static int LifeTime;

        private static int Time;

        private static bool Active;

        public const float BaseScaleAmount = 0.04f;
        private const float BaseBlurAmount = 4f;

        private static float LifetimeRatio => (float)Time / LifeTime;

        /// <summary>
        /// Call this to set the blur effect. Any existing ones will be replaced.
        /// </summary>
        /// <param name="position">The focal position, in world co-ordinates</param>
        /// <param name="intensity">How intense to make the scale and blur effect. A 0-1 range should be used</param>
        /// <param name="lifetime">How long the effect should last</param>
        public static void SetBlurEffect(Vector2 position, float intensity, int lifetime)
        {
            Position = position;
            Intensity = intensity;
            LifeTime = lifetime;
            Time = 0;
            Active = true;
        }

        public override void Load()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += DrawBlurEffect;
            Main.OnResolutionChanged += ResizeRenderTarget;
        }

        public override void Unload() 
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= DrawBlurEffect;
            Main.OnResolutionChanged -= ResizeRenderTarget;
        }

        public override void PostUpdateEverything()
        {
            if (Active)
            {
                if (Time >= LifeTime)
                {
                    Active = false;
                    Time = 0;
                }
                Time++;
            }
        }

        private void DrawBlurEffect(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig, Terraria.Graphics.Effects.FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Microsoft.Xna.Framework.Color clearColor)
        {
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);

            if (Active)
            {
                if (BlurRenderTarget is null)
                    ResizeRenderTarget(Vector2.Zero);

                // Draw the screen contents to the blur render target.
                BlurRenderTarget.SwapToRenderTarget();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                // Reset the render target.
                Main.instance.GraphicsDevice.SetRenderTarget(null);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Draw the blur render target 4 times, getting progressively larger and more transparent.
                for (int i = -4; i <= 4; i++)
                {
                    // Increase the scale based on the intensity and lifetime of the blur.
                    float scaleAmount = BaseScaleAmount * Intensity;
                    float blurAmount = BaseBlurAmount * Intensity;
                    float scale = 1f + scaleAmount * (1f - LifetimeRatio) * i / blurAmount;
                    Color drawColor = Color.White * 0.35f;

                    // Not doing this causes it to not properly fit on the screen. This extends it to be 100 extra in either direction.
                    Rectangle frameOffset = new(-100, -100, Main.screenWidth + 200, Main.screenHeight + 200);
                    // Use that and the position to set the origin to the draw position.
                    Vector2 origin = Position + new Vector2(100) - Main.screenPosition;
                    Main.spriteBatch.Draw(BlurRenderTarget, Position - Main.screenPosition, frameOffset, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
        }

        private void ResizeRenderTarget(Vector2 obj)
        {
            BlurRenderTarget = new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }
    }
}
