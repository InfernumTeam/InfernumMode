using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class ScreenInversionMetaballSystem : ModSystem
    {
        internal static Queue<DrawData> Metaballs = new();

        public static RenderTarget2D MetaballsTarget
        {
            get;
            private set;
        }

        public override void OnWorldLoad() => Metaballs = new();

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareTarget;
            On.Terraria.Main.SetDisplayMode += ResetTargetSize;
            On.Terraria.Main.SortDrawCacheWorms += DrawMetaballs;
        }

        private void ResetTargetSize(On.Terraria.Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            if (MetaballsTarget is not null && width == MetaballsTarget.Width && height == MetaballsTarget.Height)
                return;

            ScreenSaturationBlurSystem.DrawActionQueue.Enqueue(() =>
            {
                // Free GPU resources for the old targets.
                MetaballsTarget?.Dispose();

                // Recreate targets.
                MetaballsTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            });

            orig(width, height, fullscreen);
        }

        private void DrawMetaballs(On.Terraria.Main.orig_SortDrawCacheWorms orig, Main self)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.instance.GraphicsDevice.Textures[1] = ScreenSaturationBlurSystem.FinalScreenTarget;
            InfernumEffectsRegistry.ScreenInversionMetaballShader.Apply();
            Main.spriteBatch.Draw(MetaballsTarget, Main.screenLastPosition - Main.screenPosition, Color.White);
            Main.spriteBatch.End();

            orig(self);
        }

        internal static void PrepareTarget(GameTime obj)
        {
            if (Main.gameMenu || MetaballsTarget.IsDisposed)
                return;

            // Draw all metaball particles to the target.
            Main.instance.GraphicsDevice.SetRenderTarget(MetaballsTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            while (Metaballs.TryDequeue(out DrawData d))
                d.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        public static void AddMetaball(DrawData metaballData) => Metaballs.Enqueue(metaballData);
    }
}