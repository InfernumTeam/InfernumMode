using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.UI;
using CalamityMod.World;
using InfernumMode.Balancing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class NerfAdrenalineHook : IHookEdit
    {
        internal static void NerfAdrenalineRates(ILContext context)
        {
            ILCursor c = new(context);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchStfld<CalamityPlayer>("adrenaline")))
                return;
            if (!c.TryGotoPrev(MoveType.After, i => i.MatchLdloc(out _)))
                return;

            c.EmitDelegate<Func<float>>(() => InfernumMode.CanUseCustomAIs && !Main.LocalPlayer.Calamity().adrenalineModeActive ? BalancingChangesManager.AdrenalineChargeTimeFactor : 1f);
            c.Emit(OpCodes.Div);
        }

        public void Load() => UpdateRippers += NerfAdrenalineRates;
        public void Unload() => UpdateRippers -= NerfAdrenalineRates;
    }

    public class DrawBlackEffectHook : IHookEdit
    {
        public static List<int> DrawCacheBeforeBlack = new(Main.maxProjectiles);
        public static List<int> DrawCacheProjsOverSignusBlackening = new(Main.maxProjectiles);
        public static List<int> DrawCacheAdditiveLighting = new(Main.maxProjectiles);
        internal static void DrawBlackout(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Main>("DrawBackgroundBlackFill")))
                return;

            cursor.EmitDelegate<Action>(() =>
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

            cursor.EmitDelegate<Action>(() =>
            {
                float fadeToBlack = 0f;
                if (CalamityGlobalNPC.signus != -1)
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

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                for (int i = 0; i < DrawCacheAdditiveLighting.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheAdditiveLighting[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheAdditiveLighting[i]].active = false;
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawCacheAdditiveLighting.Clear();
            });
        }

        public void Load()
        {
            DrawCacheProjsOverSignusBlackening = new List<int>();
            DrawCacheAdditiveLighting = new List<int>();
            IL.Terraria.Main.DoDraw += DrawBlackout;
        }

        public void Unload()
        {
            DrawCacheProjsOverSignusBlackening = DrawCacheAdditiveLighting = null;
            IL.Terraria.Main.DoDraw -= DrawBlackout;
        }
    }

    public class DisableMoonLordBuildingHook : IHookEdit
    {
        internal static void DisableMoonLordBuilding(ILContext instructionContext)
        {
            var c = new ILCursor(instructionContext);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdcI4(ItemID.SuperAbsorbantSponge)))
                return;

            c.EmitDelegate<Action>(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs)
                    Main.LocalPlayer.noBuilding = true;
            });
        }

        public void Load() => IL.Terraria.Player.ItemCheck += DisableMoonLordBuilding;

        public void Unload() => IL.Terraria.Player.ItemCheck -= DisableMoonLordBuilding;
    }

    public class ChangeHowMinibossesSpawnInDD2EventHook : IHookEdit
    {
        internal static int GiveDD2MinibossesPointPriority(On.Terraria.GameContent.Events.DD2Event.orig_GetMonsterPointsWorth orig, int slainMonsterID)
        {
            if (OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID) && minibossID != NPCID.DD2Betsy && InfernumMode.CanUseCustomAIs)
                return slainMonsterID == minibossID ? 99999 : 0;

            return orig(slainMonsterID);
        }

        public void Load() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth += GiveDD2MinibossesPointPriority;

        public void Unload() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth -= GiveDD2MinibossesPointPriority;
    }

    public class DrawVoidBackgroundDuringMLFightHook : IHookEdit
    {
        public static void PrepareShaderForBG(On.Terraria.Main.orig_DrawSurfaceBG orig, Main self)
        {
            int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
            bool useShader = InfernumMode.CanUseCustomAIs && moonLordIndex >= 0 && moonLordIndex < Main.maxNPCs && !Main.gameMenu;

            try
            {
                orig(self);
            }
            catch (IndexOutOfRangeException) { }

            if (useShader)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

                Rectangle arena = Main.npc[moonLordIndex].Infernum().arenaRectangle;
                Vector2 topLeft = (arena.TopLeft() + Vector2.One * 8f - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight) / Main.GameViewMatrix.Zoom;
                Vector2 bottomRight = (arena.BottomRight() + Vector2.One * 16f - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight) / Main.GameViewMatrix.Zoom;
                Matrix zoomMatrix = Main.GameViewMatrix.TransformationMatrix;

                Vector2 scale = new Vector2(Main.screenWidth, Main.screenHeight) / TextureAssets.MagicPixel.Value.Size() * Main.GameViewMatrix.Zoom;
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].Shader.Parameters["uTopLeftFreeArea"].SetValue(topLeft);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].Shader.Parameters["uBottomRightFreeArea"].SetValue(bottomRight);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].Shader.Parameters["uZoomMatrix"].SetValue(zoomMatrix);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].UseColor(Color.Gray);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].UseSecondaryColor(Color.Turquoise);
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/CultistRayMap"));
                GameShaders.Misc["Infernum:MoonLordBGDistortion"].Apply();
                Vector2 hell = new(Main.screenWidth * (Main.GameViewMatrix.Zoom.X - 1f), Main.screenHeight * (Main.GameViewMatrix.Zoom.Y - 1f));
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hell * -0.5f, null, Color.White, 0f, Vector2.Zero, scale, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin();
            }
        }

        public void Load() => On.Terraria.Main.DrawSurfaceBG += PrepareShaderForBG;

        public void Unload() => On.Terraria.Main.DrawSurfaceBG -= PrepareShaderForBG;
    }
}