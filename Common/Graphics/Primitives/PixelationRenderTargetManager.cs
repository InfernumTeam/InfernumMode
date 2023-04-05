using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Primitives
{
    public class PixelationRenderTargetManager : ModSystem
    {
        #region Fields And Properities
        private Vector2 previousScreenSize;

        private static RenderTarget2D pixelRenderTarget;

        private static readonly List<IPixelPrimitiveDrawer> pixelPrimDrawersList = new();
        #endregion

        #region Overrides
        public override void Load()
        {
            On.Terraria.Main.CheckMonoliths += DrawToCustomRenderTargets;
            On.Terraria.Main.DoDraw_DrawNPCsOverTiles += DrawPixelRenderTarget;
            ResizePixelRenderTarget(true);
        }

        public override void Unload()
        {
            On.Terraria.Main.CheckMonoliths -= DrawToCustomRenderTargets;
            On.Terraria.Main.DoDraw_DrawNPCsOverTiles -= DrawPixelRenderTarget;
        }

        public override void PostUpdateEverything() => ResizePixelRenderTarget(false);
        #endregion

        #region Methods
        private void DrawPixelRenderTarget(On.Terraria.Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            orig(self);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw the RT. The scale is important, it is 2 here as this RT is 0.5x the main screen size.
            Main.spriteBatch.Draw(pixelRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        private void DrawToCustomRenderTargets(On.Terraria.Main.orig_CheckMonoliths orig)
        {
            // Clear the render target from the previous frame.
            pixelPrimDrawersList.Clear();

            // Check every active projectile.
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile projectile = Main.projectile[i];

                // If the projectile is active, a mod projectile, and uses the interface, add it to the list of prims to draw this frame.
                if (projectile.active && projectile.ModProjectile != null && projectile.ModProjectile is IPixelPrimitiveDrawer pixelPrimitiveProjectile)
                    pixelPrimDrawersList.Add(pixelPrimitiveProjectile);
            }

            // Check every active NPC.
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC npc = Main.npc[i];

                // If the NPC is active, a mod NPC, and uses our interface add it to the list of prims to draw this frame.
                if (npc.active && npc.ModNPC != null && npc.ModNPC is IPixelPrimitiveDrawer pixelPrimitiveNPC)
                    pixelPrimDrawersList.Add(pixelPrimitiveNPC);
            }

            // Draw the prims. The render target gets set here.
            DrawPrimsToRenderTarget(pixelRenderTarget, pixelPrimDrawersList);

            // Clear the current render target.
            Main.graphics.GraphicsDevice.SetRenderTarget(null);

            // Call the original method.
            orig();
        }

        private static void DrawPrimsToRenderTarget(RenderTarget2D renderTarget, List<IPixelPrimitiveDrawer> pixelPrimitives)
        {
            // Swap to the custom render target to prepare things to pixelation.
            renderTarget.SwapToRenderTarget();

            if (pixelPrimitives.Any())
            {
                // Start a spritebatch, as one does not exist before the method we're detouring.
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

                // Loop through the list and call each draw function.
                foreach (IPixelPrimitiveDrawer pixelPrimitiveDrawer in pixelPrimitives)
                    pixelPrimitiveDrawer.DrawPixelPrimitives(Main.spriteBatch);

                // Prepare the sprite batch for the next draw cycle.
                Main.spriteBatch.End();
            }
        }

        private void ResizePixelRenderTarget(bool load)
        {
            // If not in the game menu, and not on a dedicated server, or this is the initial setup.
            if (!Main.gameMenu && !Main.dedServ || load && !Main.dedServ)
            {
                // Get the current screen size.
                Vector2 currentScreenSize = new(Main.screenWidth, Main.screenHeight);

                // If it does not match the previous one, update it.
                if (currentScreenSize != previousScreenSize)
                {
                    // Render target stuff should be done on the main thread only.
                    Main.QueueMainThreadAction(() =>
                    {
                        // If it is not null, or already disposed, dispose it.
                        if (pixelRenderTarget != null && !pixelRenderTarget.IsDisposed)
                            pixelRenderTarget.Dispose();

                        // Recreate the render target with the current, accurate screen dimensions.
                        // In this case, we want to halve them to downscale it, pixelating it.
                        pixelRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
                    });
                }

                // Set the current one to the previous one for next frame.
                previousScreenSize = currentScreenSize;
            }
        }
        #endregion
    }
}
