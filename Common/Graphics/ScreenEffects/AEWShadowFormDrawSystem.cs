using CalamityMod;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Content.Tiles.Colosseum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using static InfernumMode.Core.GlobalInstances.Systems.ScreenOverlaysSystem;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class AEWShadowFormDrawSystem : ModSystem
    {
        public static ManagedRenderTarget AEWDrawTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget AEWShadowWispTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget TemporaryAuxillaryTarget
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
            AEWDrawTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            AEWShadowWispTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            TemporaryAuxillaryTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            Filters.Scene.OnPostDraw += WhatTheFuck;
        }

        public override void OnModUnload()
        {
            Main.QueueMainThreadAction(() =>
            {
                Main.OnPreDraw -= PrepareAEWTargets;

                if (AEWDrawTarget is not null && !AEWDrawTarget.IsDisposed)
                    AEWDrawTarget.Dispose();

                AEWDrawTarget = null;

                if (AEWShadowWispTarget is not null && !AEWShadowWispTarget.IsDisposed)
                    AEWShadowWispTarget.Dispose();

                AEWShadowWispTarget = null;

                if (TemporaryAuxillaryTarget is not null && !TemporaryAuxillaryTarget.IsDisposed)
                    TemporaryAuxillaryTarget.Dispose();

                TemporaryAuxillaryTarget = null;

                Filters.Scene.OnPostDraw -= WhatTheFuck;
            });

            LightAndDarkEffectsCache.Clear();
            AEWEyesDrawCache.Clear();
        }

        internal static void PrepareAEWTargets(GameTime obj)
        {
            if (Main.gameMenu || !NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || AEWDrawTarget.Target.IsDisposed)
                return;

            Main.instance.GraphicsDevice.SetRenderTarget(AEWDrawTarget.Target);
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
            Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget.Target);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            Main.instance.GraphicsDevice.Textures[0] = AEWShadowWispTarget.Target;
            Main.instance.GraphicsDevice.Textures[1] = AEWDrawTarget.Target;
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
            var shader = InfernumEffectsRegistry.AEWShadowFormShader.Shader;
            shader.Parameters["actualSize"].SetValue(AEWShadowWispTarget.Target.Size());
            shader.Parameters["screenMoveOffset"].SetValue(Main.screenPosition - Main.screenLastPosition);
            shader.CurrentTechnique.Passes["UpdatePass"].Apply();

            Main.spriteBatch.Draw(AEWShadowWispTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
            Main.spriteBatch.End();

            AEWShadowWispTarget.Target.CopyContentsFrom(TemporaryAuxillaryTarget.Target);
        }

        public static void DrawTarget()
        {
            int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<PrimordialWyrmHead>());
            if (aewIndex == -1)
                return;

            NPC aew = Main.npc[aewIndex];
            float lightFormInterpolant = aew.Infernum().ExtraAI[AEWHeadBehaviorOverride.LightFormInterpolantIndex];
            float darkFormInterpolant = aew.Infernum().ExtraAI[AEWHeadBehaviorOverride.DarkFormInterpolantIndex];
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["lightFormInterpolant"].SetValue(lightFormInterpolant * 0.5f);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["darkFormInterpolant"].SetValue(darkFormInterpolant);
            InfernumEffectsRegistry.AEWShadowFormShader.Shader.Parameters["actualSize"].SetValue(AEWDrawTarget.Target.Size());
            InfernumEffectsRegistry.AEWShadowFormShader.UseColor(Color.Purple);
            InfernumEffectsRegistry.AEWShadowFormShader.UseSecondaryColor(Color.DarkViolet * 0.7f);
            InfernumEffectsRegistry.AEWShadowFormShader.UseImage1("Images/Misc/Perlin");
            InfernumEffectsRegistry.AEWShadowFormShader.Apply();
            Main.instance.GraphicsDevice.Textures[2] = AEWShadowWispTarget.Target;
            Main.spriteBatch.Draw(AEWDrawTarget.Target, Vector2.Zero, Color.White);
        }

        private void WhatTheFuck()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            AEWHeadBehaviorOverride.TryToDrawAbyssalBlackHole();

            if (Main.GameUpdateCount % 10 == 0)
                LargeLumenylCrystal.DefineCrystalDrawers();

            IcicleDrawer.ApplyShader();
            foreach (Point p in LargeLumenylCrystal.CrystalCache.Keys)
            {
                IcicleDrawer crystal = LargeLumenylCrystal.CrystalCache[p];
                crystal.Draw((p.ToWorldCoordinates(8f, 0f) + Vector2.UnitY.RotatedBy(crystal.BaseDirection) * 10f).ToPoint(), false);
            }

            // Regularly reset the crystal cache.
            if (Main.GameUpdateCount % 120 == 119)
                LargeLumenylCrystal.CrystalCache.Clear();

            ColosseumPortal.PortalCache.RemoveAll(p => CalamityUtils.ParanoidTileRetrieval(p.X, p.Y).TileType != ModContent.TileType<ColosseumPortal>());
            foreach (Point p in ColosseumPortal.PortalCache)
                ColosseumPortal.DrawSpecialEffects(p.ToWorldCoordinates());

            ThingsToDrawOnTopOfBlur.EmptyDrawCache();
            DrawAdditiveCache();
            DrawEntityTargets();
            DrawAboveWaterProjectiles();
            Main.spriteBatch.End();
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) && Lighting.NotRetro)
                Main.PlayerRenderer.DrawPlayers(Main.Camera, Main.player.Where(p => p.active && !p.dead && p.Calamity().ZoneAbyssLayer4));
        }

        internal static void DrawAdditiveCache()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            ThingsToDrawOnTopOfBlurAdditive.EmptyDrawCache();
        }

        internal static void DrawEntityTargets()
        {
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || ShadowIllusionDrawSystem.ShadowProjectilesExist)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawTarget();
                ShadowIllusionDrawSystem.DrawTarget();
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < AEWEyesDrawCache.Count; i++)
            {
                AEWEyesDrawCache[i] = AEWEyesDrawCache[i] with
                {
                    position = AEWEyesDrawCache[i].position + Main.screenPosition - Main.screenLastPosition
                };
            }

            AEWEyesDrawCache.EmptyDrawCache();
        }
    }
}
