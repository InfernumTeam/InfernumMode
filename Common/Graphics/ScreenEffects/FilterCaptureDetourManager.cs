using InfernumMode.Common.BaseEntities;
using InfernumMode.Content.Cutscenes;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class FilterCaptureDetourManager : ModSystem
    {
        public ManagedRenderTarget ScreenTarget
        {
            get;
            private set;
        }

        public override void Load()
        {
            ScreenTarget = new(true, RenderTargetManager.CreateScreenSizedTarget, true);
            On_FilterManager.EndCapture += EndCaptureManager;
        }

        public override void Unload() => On_FilterManager.EndCapture -= EndCaptureManager;

        // The purpose of this is to make these all work together and apply in the correct order. Even though it does it for two things. Oh well!
        private void EndCaptureManager(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            ScreenTarget.SwapToRenderTarget();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // Draw pulse rings.
            BasePulseRingProjectile.DrawPulseRings();

            screenTarget1.SwapToRenderTarget();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(ScreenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            // Draw the screen effects.
            if (!InfernumConfig.Instance.ReducedGraphicsConfig)
                screenTarget1 = ScreenEffectSystem.DrawBlurEffect(screenTarget1);

            CutsceneManager.DrawWorld(screenTarget1);

            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
