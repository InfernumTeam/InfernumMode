using CalamityMod;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops;
using InfernumMode.Content.Tiles;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Core.GlobalInstances.Systems.ScreenOverlaysSystem;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class ScreenSaturationBlurSystem : ModSystem
    {
        internal static Queue<Action> DrawActionQueue = new();

        public static bool ShouldEffectBeActive
        {
            get;
            set;
        }

        public static float Intensity
        {
            get;
            private set;
        }

        public static ManagedRenderTarget BloomTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget FinalScreenTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget DownscaledBloomTarget
        {
            get;
            private set;
        }

        public static ManagedRenderTarget TemporaryAuxillaryTarget
        {
            get;
            private set;
        }

        public static List<DrawData> ThingsToBeManuallyBlurred
        {
            get;
            private set;
        } = new();

        public static bool DebugDrawBloomMap => false;

        public static bool UseFastBlurPass => true;

        public static int TotalBlurIterations => 1;

        public static float DownscaleFactor => 32f;

        public static float BlurBrightnessFactor => 60f;

        public static float BlurBrightnessExponent => 4.3f;

        public static float BlurSaturationBiasInterpolant => 0.15f;

        public override void OnModLoad()
        {
            // Initialize target defintions.
            BloomTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            FinalScreenTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            DownscaledBloomTarget = new(true, new((width, height) =>
            {
                return new(Main.instance.GraphicsDevice, (int)(width / DownscaleFactor), (int)(height / DownscaleFactor), true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            }));
            TemporaryAuxillaryTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);

            Main.OnPreDraw += HandleDrawMainThreadQueue;
            On_FilterManager.EndCapture += GetFinalScreenShader;

            Main.QueueMainThreadAction(() =>
            {
                IL_Main.DoDraw += LetEffectsDrawOnBudgetLightSettings;
            });
            Main.OnPreDraw += PrepareBlurEffects;
            Filters.Scene.OnPostDraw += WhatTheFuck;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= HandleDrawMainThreadQueue;
            Main.OnPreDraw -= PrepareBlurEffects;
            Filters.Scene.OnPostDraw -= WhatTheFuck;
            On_FilterManager.EndCapture -= GetFinalScreenShader;
            Filters.Scene.OnPostDraw -= WhatTheFuck;
            On_FilterManager.EndCapture -= GetFinalScreenShader;

            Main.QueueMainThreadAction(() =>
            {
                IL_Main.DoDraw -= LetEffectsDrawOnBudgetLightSettings;
                if (BloomTarget is not null && !BloomTarget.IsDisposed)
                    BloomTarget.Dispose();
                if (FinalScreenTarget is not null && !FinalScreenTarget.IsDisposed)
                    FinalScreenTarget.Dispose();
                if (DownscaledBloomTarget is not null && !DownscaledBloomTarget.IsDisposed)
                    DownscaledBloomTarget.Dispose();
                if (TemporaryAuxillaryTarget is not null && !TemporaryAuxillaryTarget.IsDisposed)
                    TemporaryAuxillaryTarget.Dispose();
            });
        }

        private void LetEffectsDrawOnBudgetLightSettings(ILContext il)
        {
            ILCursor c = new(il);

            int localIndex = 0;
            c.GotoNext(i => i.MatchCallOrCallvirt<FilterManager>("BeginCapture"));
            c.GotoPrev(MoveType.After, i => i.MatchStloc(out localIndex));

            c.Emit(OpCodes.Ldloc, localIndex);
            c.EmitDelegate(() =>
            {
                bool fightingAEW = NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) && InfernumMode.CanUseCustomAIs;
                bool shadowProjectilesExist = ShadowIllusionDrawSystem.ShadowProjectilesExist;
                bool secondaryCondition = fightingAEW || shadowProjectilesExist;
                return secondaryCondition && !Main.mapFullscreen && !Main.gameMenu;
            });
            c.Emit(OpCodes.Or);
            c.Emit(OpCodes.Stloc, localIndex);
        }

        private void HandleDrawMainThreadQueue(GameTime gameTime)
        {
            while (DrawActionQueue is not null && DrawActionQueue.TryDequeue(out Action a))
                a();
        }

        internal static void GetFinalScreenShader(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            // Copy the contents of the screen target in the final screen target.
            FinalScreenTarget.Target.SwapToRenderTarget();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();

            ColosseumPortal.PortalCache.RemoveAll(p => CalamityUtils.ParanoidTileRetrieval(p.X, p.Y).TileType != ModContent.TileType<ColosseumPortal>());
            foreach (Point p in ColosseumPortal.PortalCache)
                ColosseumPortal.DrawSpecialEffects(p.ToWorldCoordinates());
            Main.instance.GraphicsDevice.SetRenderTarget(null);

            orig(self, finalTexture, Intensity > 0f ? screenTarget1 : FinalScreenTarget.Target, screenTarget2, clearColor);
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
                AEWShadowFormDrawSystem.DrawTarget();
                ShadowIllusionDrawSystem.DrawTarget();
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < AEWShadowFormDrawSystem.AEWEyesDrawCache.Count; i++)
            {
                AEWShadowFormDrawSystem.AEWEyesDrawCache[i] = AEWShadowFormDrawSystem.AEWEyesDrawCache[i] with
                {
                    position = AEWShadowFormDrawSystem.AEWEyesDrawCache[i].position + Main.screenPosition - Main.screenLastPosition
                };
            }

            AEWShadowFormDrawSystem.AEWEyesDrawCache.EmptyDrawCache();
        }

        internal static void PrepareBlurEffects(GameTime obj)
        {
            // Bullshit to ensure that the scene effect can always capture, thus preventing very bad screen flash effects.
            if (Intensity > 0f)
                Main.drawToScreen = false;
            else
                return;

            if (InfernumConfig.Instance is null || InfernumConfig.Instance.SaturationBloomIntensity <= 0f || Main.gameMenu || !Lighting.NotRetro || DownscaledBloomTarget.IsDisposed)
                return;

            // Get the downscaled texture.
            Main.instance.GraphicsDevice.SetRenderTarget(DownscaledBloomTarget.Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            Main.spriteBatch.Draw(FinalScreenTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f / DownscaleFactor, 0, 0f);
            Main.spriteBatch.End();

            // Upscale the texture again.
            Main.instance.GraphicsDevice.SetRenderTarget(BloomTarget.Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);
            Main.spriteBatch.Draw(DownscaledBloomTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, DownscaleFactor, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();
            while (ThingsToBeManuallyBlurred.Count > 0)
            {
                ThingsToBeManuallyBlurred[0].Draw(Main.spriteBatch);
                ThingsToBeManuallyBlurred.RemoveAt(0);
            }

            Main.spriteBatch.End();

            // Apply blur iterations.
            string blurPassName = UseFastBlurPass ? "DownsampleFastPass" : "DownsamplePass";
            for (int i = 0; i < TotalBlurIterations; i++)
            {
                Main.instance.GraphicsDevice.SetRenderTarget(TemporaryAuxillaryTarget.Target);
                Main.instance.GraphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

                var shader = InfernumEffectsRegistry.ScreenSaturationBlurScreenShader.GetShader().Shader;
                shader.Parameters["uImageSize1"].SetValue(BloomTarget.Target.Size());
                shader.Parameters["blurMaxOffset"].SetValue(136f);
                shader.CurrentTechnique.Passes[blurPassName].Apply();

                Main.spriteBatch.Draw(BloomTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
                Main.spriteBatch.End();

                BloomTarget.Target.CopyContentsFrom(TemporaryAuxillaryTarget.Target);
            }
        }

        private void WhatTheFuck()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            AEWHeadBehaviorOverride.TryToDrawAbyssalBlackHole();
            ThingsToDrawOnTopOfBlur.EmptyDrawCache();

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

            DrawAdditiveCache();
            DrawEntityTargets();
            DrawAboveWaterProjectiles();
            Main.spriteBatch.End();
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) && Lighting.NotRetro)
                Main.PlayerRenderer.DrawPlayers(Main.Camera, Main.player.Where(p => p.active && !p.dead && p.Calamity().ZoneAbyssLayer4));
        }

        public override void PostUpdateEverything()
        {
            // Don't mess with shaders server-side.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Update the intensity in accordance with the effect state.
            bool effectShouldBeActive = ShouldEffectBeActive && InfernumConfig.Instance.SaturationBloomIntensity > 0f && Lighting.NotRetro;
            Intensity = Clamp(Intensity + effectShouldBeActive.ToDirectionInt() * 0.05f, 0f, 1f);

            if (effectShouldBeActive)
            {
                if (!InfernumEffectsRegistry.ScreenSaturationBlurScreenShader.IsActive())
                    Filters.Scene.Activate("InfernumMode:ScreenSaturationBlur", Main.LocalPlayer.Center);
            }
            else if (InfernumEffectsRegistry.ScreenSaturationBlurScreenShader.IsActive() && Intensity <= 0f)
                InfernumEffectsRegistry.ScreenSaturationBlurScreenShader.Deactivate();

            ShouldEffectBeActive = false;
        }
    }
}
