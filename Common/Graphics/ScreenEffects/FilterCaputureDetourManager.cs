using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class FilterCaputureDetourManager : ModSystem
    {
        public override void Load()
        {
            On_FilterManager.EndCapture += EndCaptureManager;
        }

        public override void Unload()
        {
            On_FilterManager.EndCapture -= EndCaptureManager;
        }

        // The purpose of this is to make these all work together and apply in the correct order.
        private void EndCaptureManager(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            // Draw the screen effects first.
            screenTarget1 = ScreenEffectSystem.DrawBlurEffect(screenTarget1);

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}