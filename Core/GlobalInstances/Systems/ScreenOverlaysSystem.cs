using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class ScreenOverlaysSystem : ModSystem
    {
        public static List<int> DrawCacheBeforeBlack
        {
            get;
            private set;
        } = new(Main.maxProjectiles);

        public static List<int> DrawCacheProjsOverSignusBlackening
        {
            get;
            private set;
        } = new(Main.maxProjectiles);

        public static List<DrawData> ThingsToDrawOnTopOfBlur
        {
            get;
            private set;
        } = new();

        public static List<DrawData> ThingsToDrawOnTopOfBlurAdditive
        {
            get;
            private set;
        } = new();

        internal static void DrawBlackout(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Main>("DrawBackgroundBlackFill")))
                return;

            cursor.EmitDelegate(() =>
            {
                if (Main.gameMenu)
                    return;

                for (int i = 0; i < DrawCacheBeforeBlack.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawNPC(DrawCacheBeforeBlack[i], false);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.npc[DrawCacheBeforeBlack[i]].active = false;
                    }
                }
                DrawCacheBeforeBlack.Clear();
            });

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<MoonlordDeathDrama>("DrawWhite")))
                return;

            cursor.EmitDelegate(() =>
            {
                if (Main.gameMenu)
                    return;

                float fadeToBlack = 0f;
                if (CalamityGlobalNPC.signus != -1 && Main.npc[CalamityGlobalNPC.signus].active)
                    fadeToBlack = Main.npc[CalamityGlobalNPC.signus].Infernum().ExtraAI[9];
                if (InfernumMode.BlackFade > 0f)
                    fadeToBlack = InfernumMode.BlackFade;

                if (fadeToBlack > 0f)
                {
                    Color color = Color.Black * fadeToBlack;
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < DrawCacheProjsOverSignusBlackening.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheProjsOverSignusBlackening[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheProjsOverSignusBlackening[i]].active = false;
                    }
                }
                DrawCacheProjsOverSignusBlackening.Clear();

                DrawCulledProjectiles();
                DrawSpecializedProjectileGroups();

                // Draw the madness effect.
                if (InfernumMode.CanUseCustomAIs && NPC.AnyNPCs(NPCID.Deerclops))
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                    InfernumEffectsRegistry.MadnessScreenShader.GetShader().UseSecondaryColor(Color.DarkViolet with { A = 20 });
                    InfernumEffectsRegistry.MadnessScreenShader.Apply();
                    Main.spriteBatch.Draw(ModContent.Request<Texture2D>("Terraria/Images/Misc/noise").Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), Color.White);
                    Main.spriteBatch.ExitShaderRegion();
                }
            });
        }

        internal static void DrawCulledProjectiles()
        {
            Main.spriteBatch.End();

            RasterizerState rasterizer = Main.Rasterizer;
            rasterizer.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.ScissorRectangle = new(-50, -50, Main.screenWidth + 100, Main.screenHeight + 100);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].ModProjectile is not IScreenCullDrawer drawer)
                    continue;

                drawer.CullDraw(Main.spriteBatch);
            }

            Main.spriteBatch.ExitShaderRegion();
        }

        internal static void DrawSpecializedProjectileGroups()
        {
            List<Projectile> specialProjectiles = Main.projectile.Take(Main.maxProjectiles).Where(p => p.active && p.ModProjectile is not null and ISpecializedDrawRegion).ToList();

            // Don't mess with the spritebatch if there are no specialized projectiles.
            if (!specialProjectiles.Any())
                return;

            foreach (var projectileGroup in specialProjectiles.GroupBy(p => p.type))
            {
                ISpecializedDrawRegion regionProperties = projectileGroup.First().ModProjectile as ISpecializedDrawRegion;
                regionProperties.PrepareSpriteBatch(Main.spriteBatch);

                foreach (var proj in projectileGroup)
                    ((ISpecializedDrawRegion)proj.ModProjectile).SpecialDraw(Main.spriteBatch);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        internal static void DrawAboveWaterProjectiles()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || Main.projectile[i].ModProjectile is not IAboveWaterProjectileDrawer drawer)
                    continue;

                drawer.DrawAboveWater(Main.spriteBatch);
            }
        }

        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                DrawCacheProjsOverSignusBlackening = new List<int>();
                IL_Main.DoDraw += DrawBlackout;
            });
        }

        public override void Unload()
        {
            Main.QueueMainThreadAction(() =>
            {
                DrawCacheProjsOverSignusBlackening = null;
                IL_Main.DoDraw -= DrawBlackout;
            });
        }
    }
}
