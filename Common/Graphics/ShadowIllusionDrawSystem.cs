using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class ShadowIllusionDrawSystem : ModSystem
    {
        public static RenderTarget2D ShadowDrawTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D ShadowWispTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D TemporaryAuxillaryTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareAEWTargets;
            On.Terraria.Main.SetDisplayMode += ResetTargetSizes;
        }

        private void ResetTargetSizes(On.Terraria.Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            if (ShadowDrawTarget is not null && width == ShadowDrawTarget.Width && height == ShadowDrawTarget.Height)
                return;

            ScreenSaturationBlurSystem.DrawActionQueue.Enqueue(() =>
            {
                // Free GPU resources for the old targets.
                ShadowDrawTarget?.Dispose();
                ShadowWispTarget?.Dispose();
                TemporaryAuxillaryTarget?.Dispose();

                // Recreate targets.
                ShadowDrawTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
                ShadowWispTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
                TemporaryAuxillaryTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            });

            orig(width, height, fullscreen);
        }

        internal static void PrepareAEWTargets(GameTime obj)
        {
            if (Main.gameMenu || ShadowDrawTarget.IsDisposed)
                return;

            Main.instance.GraphicsDevice.SetRenderTarget(ShadowDrawTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            DrawShadowProjectiles();
            Main.spriteBatch.End();

            PrepareNextFrameTarget();
        }

        internal static void DrawShadowProjectiles()
        {
            // Draw regular projectiles.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].ModProjectile is null)
                    continue;

                if (!Main.projectile[i].active || !Main.projectile[i].Infernum().DrawAsShadow || Main.projectile[i].ModProjectile is IAdditiveDrawer)
                    continue;

                Main.instance.DrawProj(i);
            }

            // Draw additive effects.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].ModProjectile is null)
                    continue;

                if (!Main.projectile[i].active || !Main.projectile[i].Infernum().DrawAsShadow || Main.projectile[i].ModProjectile is not IAdditiveDrawer)
                    continue;

                Main.instance.DrawProj(i);
            }
        }

        internal static void PrepareNextFrameTarget()
        {
            // Update the shadowy wisp effect every frame.
            Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            Main.instance.GraphicsDevice.Textures[0] = ShadowWispTarget;
            Main.instance.GraphicsDevice.Textures[1] = ShadowDrawTarget;
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
            var shader = InfernumEffectsRegistry.AEWShadowFormShader.Shader;
            shader.Parameters["actualSize"].SetValue(ShadowWispTarget.Size());
            shader.Parameters["screenMoveOffset"].SetValue(Main.screenPosition - Main.screenLastPosition);
            shader.CurrentTechnique.Passes["UpdatePass"].Apply();

            Main.spriteBatch.Draw(ShadowWispTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            Main.spriteBatch.End();

            ShadowWispTarget.CopyContentsFrom(TemporaryAuxillaryTarget);
        }

        public static void DrawTarget()
        {
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["lightFormInterpolant"].SetValue(0f);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["darkFormInterpolant"].SetValue(1f);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["actualSize"].SetValue(ShadowDrawTarget.Size());
            InfernumEffectsRegistry.AEWShadowFormShader.UseColor(Color.Purple);
            InfernumEffectsRegistry.AEWShadowFormShader.UseSecondaryColor(Color.DarkViolet * 0.7f);
            InfernumEffectsRegistry.AEWShadowFormShader.UseImage1("Images/Misc/Perlin");
            InfernumEffectsRegistry.AEWShadowFormShader.Apply();
            Main.instance.GraphicsDevice.Textures[2] = ShadowWispTarget;
            Main.spriteBatch.Draw(ShadowDrawTarget, Vector2.Zero, Color.White);
        }
    }
}