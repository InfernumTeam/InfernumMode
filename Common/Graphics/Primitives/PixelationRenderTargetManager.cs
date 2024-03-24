using InfernumMode.Common.Graphics.Interfaces;
using Luminance.Core.Graphics;
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
        private static ManagedRenderTarget pixelRenderTarget;

        private static ManagedRenderTarget pixelRenderTargetBeforeNPCs;

        private static readonly List<IPixelPrimitiveDrawer> pixelPrimDrawersList = new();

        private static readonly List<IPixelPrimitiveDrawer> pixelPrimDrawersListBeforeNPCs = new();
        #endregion

        #region Overrides
        public override void Load()
        {
            On_Main.CheckMonoliths += DrawToCustomRenderTargets;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawPixelRenderTarget;
            pixelRenderTarget = new(true, CreatePixelTarget);
            pixelRenderTargetBeforeNPCs = new(true, CreatePixelTarget);
        }

        public override void Unload()
        {
            On_Main.CheckMonoliths -= DrawToCustomRenderTargets;
            On_Main.DoDraw_DrawNPCsOverTiles -= DrawPixelRenderTarget;
        }
        #endregion

        #region Methods
        public static RenderTarget2D CreatePixelTarget(int width, int height) => new(Main.instance.GraphicsDevice, width / 2, height / 2);

        private static void DrawScaledTarget(RenderTarget2D target)
        {
            if (!pixelPrimDrawersList.Any() && !pixelPrimDrawersListBeforeNPCs.Any())
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw the RT. The scale is important, it is 2 here as this RT is 0.5x the main screen size.
            Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        private void DrawPixelRenderTarget(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            if (pixelPrimDrawersListBeforeNPCs.Any())
                DrawScaledTarget(pixelRenderTargetBeforeNPCs.Target);
            orig(self);
            if (pixelPrimDrawersList.Any())
                DrawScaledTarget(pixelRenderTarget.Target);
        }

        private void DrawToCustomRenderTargets(On_Main.orig_CheckMonoliths orig)
        {
            // Clear the render targets from the previous frame.
            pixelPrimDrawersList.Clear();
            pixelPrimDrawersListBeforeNPCs.Clear();

            // Check every active projectile.
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile projectile = Main.projectile[i];

                // If the projectile is active, a mod projectile, and uses the interface, add it to the list of prims to draw this frame.
                if (projectile.active && projectile.ModProjectile != null && projectile.ModProjectile is IPixelPrimitiveDrawer pixelPrimitiveProjectile)
                {
                    if (pixelPrimitiveProjectile.DrawBeforeNPCs)
                        pixelPrimDrawersListBeforeNPCs.Add(pixelPrimitiveProjectile);
                    else
                        pixelPrimDrawersList.Add(pixelPrimitiveProjectile);
                }
            }

            // Check every active NPC.
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC npc = Main.npc[i];

                // If the NPC is active, a mod NPC, and uses our interface add it to the list of prims to draw this frame.
                if (npc.active && npc.ModNPC != null && npc.ModNPC is IPixelPrimitiveDrawer pixelPrimitiveNPC)
                {
                    if (pixelPrimitiveNPC.DrawBeforeNPCs)
                        pixelPrimDrawersListBeforeNPCs.Add(pixelPrimitiveNPC);
                    else
                        pixelPrimDrawersList.Add(pixelPrimitiveNPC);
                }
            }

            // Draw the prims. The render target gets set here.
            if (pixelPrimDrawersList.Any() || pixelPrimDrawersListBeforeNPCs.Any())
            {
                DrawPrimsToRenderTarget(pixelRenderTarget.Target, pixelPrimDrawersList);
                DrawPrimsToRenderTarget(pixelRenderTargetBeforeNPCs.Target, pixelPrimDrawersListBeforeNPCs);

                // Clear the current render target.
                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            }

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
        #endregion
    }
}
