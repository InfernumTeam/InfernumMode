using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using InfernumMode.Common.Graphics.Drawers.NPCDrawers;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers;
using InfernumMode.Content.Cutscenes;

namespace InfernumMode.Common.Graphics.Drawers
{
    public class DrawerManager : ModSystem
    {
        internal static readonly List<BaseNPCDrawerSystem> NPCDrawers = new();

        internal static readonly List<BaseSceneDrawSystem> SceneDrawers = new();

        public override void Load()
        {
            Main.OnPreDraw += PrepareDrawerTargets;
            On_Main.DrawNPCs += DrawDrawerContents;
        }


        public override void Unload()
        {
            Main.OnPreDraw -= PrepareDrawerTargets;
            On_Main.DrawNPCs -= DrawDrawerContents;
        }

        private void PrepareDrawerTargets(GameTime obj)
        {
            // If not in-game, leave.
            if (Main.gameMenu)
                return;

            foreach (BaseNPCDrawerSystem drawer in NPCDrawers)
            {
                if (!drawer.ShouldDrawThisFrame || !NPC.AnyNPCs(drawer.AssosiatedNPCType) || !InfernumMode.CanUseCustomAIs)
                    continue;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer);

                drawer.MainTarget.SwapToRenderTarget();
                drawer.DrawToMainTarget(Main.spriteBatch);

                Main.spriteBatch.End();
            }

            Main.instance.GraphicsDevice.SetRenderTargets(null);

            foreach (BaseSceneDrawSystem drawer in SceneDrawers)
            {
                if (!drawer.ShouldDrawThisFrame)
                    continue;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer);

                drawer.Update();
                drawer.MainTarget.SwapToRenderTarget();
                drawer.DrawToMainTarget(Main.spriteBatch);
                drawer.DrawObjectsToMainTarget(Main.spriteBatch);

                Main.spriteBatch.End();
            }

            Main.instance.GraphicsDevice.SetRenderTargets(null);
        }

        private void DrawDrawerContents(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            orig(self, behindTiles);

            if (behindTiles)
                return;

            foreach (BaseNPCDrawerSystem drawer in NPCDrawers)
            {
                if (drawer.ShouldDrawThisFrame && NPC.AnyNPCs(drawer.AssosiatedNPCType) && InfernumMode.CanUseCustomAIs)
                    drawer.DrawMainTargetContents(Main.spriteBatch);
            }

            // Draw Cutscenes.
            CutsceneManager.DrawToWorld();
        }

        public static T GetNPCDrawer<T>() where T : BaseNPCDrawerSystem
        {
            if (Main.netMode is NetmodeID.Server || !NPCDrawers.Any())
                return null;

            return (T)NPCDrawers.First(mc => mc.GetType() == typeof(T));
        }

        public static T GetSceneDrawer<T>() where T : BaseSceneDrawSystem
        {
            if (Main.netMode is NetmodeID.Server || !SceneDrawers.Any())
                return null;

            return (T)SceneDrawers.First(mc => mc.GetType() == typeof(T));
        }
    }
}
