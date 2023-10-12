using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using InfernumMode.Common.Graphics.Drawers.NPCDrawers;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers;

namespace InfernumMode.Common.Graphics.Drawers
{
    public class DrawerManager : ModSystem
    {
        private static List<BaseNPCDrawerSystem> NPCDrawers;

        private static List<BaseSceneDrawSystem> SceneDrawers;

        public override void Load()
        {
            if (Main.netMode is NetmodeID.Server)
                return;

            NPCDrawers = new();
            SceneDrawers = new();

            Type npcDrawerType = typeof(BaseNPCDrawerSystem);
            Type sceneDrawerType = typeof(BaseSceneDrawSystem);

            foreach (Type type in Mod.Code.GetTypes())
            {
                if (!type.IsAbstract && type.IsSubclassOf(npcDrawerType))
                {
                    BaseNPCDrawerSystem drawer = Activator.CreateInstance(type) as BaseNPCDrawerSystem;
                    drawer.Load();
                    NPCDrawers.Add(drawer);
                }
                else if (!type.IsAbstract && type.IsSubclassOf(sceneDrawerType))
                {
                    BaseSceneDrawSystem drawer = Activator.CreateInstance(type) as BaseSceneDrawSystem;
                    drawer.Load();
                    SceneDrawers.Add(drawer);
                }
            }


            Main.OnPreDraw += DrawToDrawerTargets;
            On_Main.DrawNPCs += DrawDrawerContents;
        }


        public override void Unload()
        {
            if (Main.netMode is NetmodeID.Server)
                return;

            Main.OnPreDraw -= DrawToDrawerTargets;
            On_Main.DrawNPCs -= DrawDrawerContents;
        }

        private void DrawToDrawerTargets(GameTime obj)
        {
            // If not ingame, leave.
            if (Main.gameMenu)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer);
            foreach (BaseNPCDrawerSystem drawer in NPCDrawers)
            {
                if (!drawer.ShouldDrawThisFrame || !NPC.AnyNPCs(drawer.AssosiatedNPCType) || !InfernumMode.CanUseCustomAIs)
                    continue;

                drawer.MainTarget.SwapToRenderTarget();
                drawer.DrawToMainTarget(Main.spriteBatch);
            }

            foreach (BaseSceneDrawSystem drawer in SceneDrawers)
            {
                if (!drawer.ShouldDrawThisFrame)
                    continue;

                drawer.Update();
                drawer.MainTarget.SwapToRenderTarget();
                drawer.DrawToMainTarget(Main.spriteBatch);
                drawer.DrawObjectsToMainTarget(Main.spriteBatch);
            }
            Main.spriteBatch.End();
            Main.instance.GraphicsDevice.SetRenderTarget(null);
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
        }

        public static T GetSceneDrawer<T>() where T : BaseSceneDrawSystem
        {
            if (Main.netMode is NetmodeID.Server || !SceneDrawers.Any())
                return null;

            return (T)SceneDrawers.First(mc => mc.GetType() == typeof(T));
        }
    }
}
