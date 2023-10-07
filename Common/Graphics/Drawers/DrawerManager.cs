using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfernumMode.Common.Graphics.Metaballs;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Common.Graphics.Drawers
{
    public class DrawerManager : ModSystem
    {
        private List<BaseDrawerSystem> Drawers;

        public override void Load()
        {
            if (Main.netMode is NetmodeID.Server)
                return;

            Drawers = new();

            Type baseType = typeof(BaseDrawerSystem);

            foreach (Type type in Mod.Code.GetTypes())
            {
                if (!type.IsAbstract && type.IsSubclassOf(baseType))
                {
                    BaseDrawerSystem drawer = Activator.CreateInstance(type) as BaseDrawerSystem;
                    drawer.Load();
                    Drawers.Add(drawer);
                }
            }

            Main.OnPreDraw += DrawToDrawerTargets;
            On_Main.DrawNPCs += DrawDrawerContents;
        }


        public override void Unload()
        {
            if (Main.netMode is NetmodeID.Server)
                return;

            foreach (BaseDrawerSystem drawer in Drawers)
                drawer.Unload();

            Main.OnPreDraw -= DrawToDrawerTargets;
            On_Main.DrawNPCs -= DrawDrawerContents;
        }

        private void DrawToDrawerTargets(GameTime obj)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer);
            foreach (BaseDrawerSystem drawer in Drawers)
            {
                if (!drawer.ShouldDrawThisFrame || !NPC.AnyNPCs(drawer.AssosiatedNPCType) || !InfernumMode.CanUseCustomAIs)
                    continue;

                drawer.MainTarget.SwapToRenderTarget();
                drawer.DrawToMainTarget(Main.spriteBatch);
            }
            Main.spriteBatch.End();
        }

        private void DrawDrawerContents(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            orig(self, behindTiles);

            if (behindTiles)
                return;

            foreach (BaseDrawerSystem drawer in Drawers)
            {
                if (drawer.ShouldDrawThisFrame && NPC.AnyNPCs(drawer.AssosiatedNPCType) && InfernumMode.CanUseCustomAIs)
                    drawer.DrawMainTargetContents(Main.spriteBatch);
            }
        }
    }
}
