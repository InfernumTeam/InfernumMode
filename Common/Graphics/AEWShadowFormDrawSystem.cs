using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class AEWShadowFormDrawSystem : ModSystem
    {
        public static RenderTarget2D AEWDrawTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D AEWShadowWispTarget
        {
            get;
            private set;
        }

        public static RenderTarget2D TemporaryAuxillaryTarget
        {
            get;
            private set;
        }

        public static List<DrawData> LightAndDarkEffectsCache
        {
            get;
            private set;
        } = new();

        public static List<DrawData> AEWEyesDrawCache
        {
            get;
            private set;
        } = new();

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareAEWTargets;
            On.Terraria.Main.SetDisplayMode += ResetTargetSizes;
        }

        private void ResetTargetSizes(On.Terraria.Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
        {
            if (AEWDrawTarget is not null && width == AEWDrawTarget.Width && height == AEWDrawTarget.Height)
                return;

            ScreenSaturationBlurSystem.DrawActionQueue.Enqueue(() =>
            {
                // Free GPU resources for the old targets.
                AEWDrawTarget?.Dispose();
                AEWShadowWispTarget?.Dispose();
                TemporaryAuxillaryTarget?.Dispose();

                // Recreate targets.
                AEWDrawTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
                AEWShadowWispTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
                TemporaryAuxillaryTarget = new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            });

            orig(width, height, fullscreen);
        }

        internal static void PrepareAEWTargets(GameTime obj)
        {
            if (Main.gameMenu || AEWDrawTarget.IsDisposed || !NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>()))
                return;

            Main.instance.GraphicsDevice.SetRenderTarget(AEWDrawTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            DrawShadowProjectiles();
            LightAndDarkEffectsCache.EmptyDrawCache();
            Main.spriteBatch.End();

            PrepareNextFrameTarget();
        }

        internal static void DrawShadowProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].ModProjectile is not IAEWShadowProjectileDrawer drawer)
                    continue;

                drawer.DrawShadow(Main.spriteBatch);
            }
        }

        internal static void PrepareNextFrameTarget()
        {
            // Update the shadowy wisp effect every frame.
            Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            Main.instance.GraphicsDevice.Textures[0] = AEWShadowWispTarget;
            Main.instance.GraphicsDevice.Textures[1] = AEWDrawTarget;
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
            var shader = InfernumEffectsRegistry.AEWShadowFormShader.Shader;
            shader.Parameters["actualSize"].SetValue(AEWShadowWispTarget.Size());
            shader.Parameters["screenMoveOffset"].SetValue(Main.screenPosition - Main.screenLastPosition);
            shader.CurrentTechnique.Passes["UpdatePass"].Apply();

            Main.spriteBatch.Draw(AEWShadowWispTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            Main.spriteBatch.End();

            AEWShadowWispTarget.CopyContentsFrom(TemporaryAuxillaryTarget);
        }

        public static void DrawTarget()
        {
            int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<AdultEidolonWyrmHead>());
            if (aewIndex == -1)
                return;

            NPC aew = Main.npc[aewIndex];
            float lightFormInterpolant = aew.Infernum().ExtraAI[AEWHeadBehaviorOverride.LightFormInterpolantIndex];
            float darkFormInterpolant = aew.Infernum().ExtraAI[AEWHeadBehaviorOverride.DarkFormInterpolantIndex];
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["lightFormInterpolant"].SetValue(lightFormInterpolant * 0.5f);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["darkFormInterpolant"].SetValue(darkFormInterpolant);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["actualSize"].SetValue(AEWDrawTarget.Size());
            InfernumEffectsRegistry.AEWShadowFormShader.UseColor(Color.Purple);
            InfernumEffectsRegistry.AEWShadowFormShader.UseSecondaryColor(Color.DarkViolet * 0.7f);
            InfernumEffectsRegistry.AEWShadowFormShader.UseImage1("Images/Misc/Perlin");
            InfernumEffectsRegistry.AEWShadowFormShader.Apply();
            Main.instance.GraphicsDevice.Textures[2] = AEWShadowWispTarget;
            Main.spriteBatch.Draw(AEWDrawTarget, Vector2.Zero, Color.White);
        }
    }
}