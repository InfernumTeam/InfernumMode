using InfernumMode.Content.Credits;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class FilterCaputureDetourManager : ModSystem
    {
        public override void Load()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += EndCaptureManager;
        }

        public override void Unload()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= EndCaptureManager;
        }

        // The purpose of this is to make these all work together and apply in the correct order.
        private void EndCaptureManager(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            // Draw the screen effects first.
            screenTarget1 = ScreenEffectSystem.DrawBlurEffect(screenTarget1);

            // Draw lighting effects.
            screenTarget1 = FancyLightingSystem.DrawRTStuff(screenTarget1);

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}