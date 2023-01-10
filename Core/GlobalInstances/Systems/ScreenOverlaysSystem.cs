using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
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

        internal static void DrawBlackout(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Main>("DrawBackgroundBlackFill")))
                return;

            cursor.EmitDelegate(() =>
            {
                for (int i = 0; i < DrawCacheBeforeBlack.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheBeforeBlack[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheBeforeBlack[i]].active = false;
                    }
                }
                DrawCacheBeforeBlack.Clear();
            });

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<MoonlordDeathDrama>("DrawWhite")))
                return;

            cursor.EmitDelegate(() =>
            {
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

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

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

        public override void OnModLoad()
        {
            DrawCacheProjsOverSignusBlackening = new List<int>();
            IL.Terraria.Main.DoDraw += DrawBlackout;
        }

        public override void Unload()
        {
            DrawCacheProjsOverSignusBlackening = null;
            IL.Terraria.Main.DoDraw -= DrawBlackout;
        }
    }
}