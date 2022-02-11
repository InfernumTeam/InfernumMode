using CalamityMod.NPCs;
using InfernumMode.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.BehaviorOverrides.MinibossAIs.MiscAIs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class MakeGolemRoomInvariableHook : IHookEdit
    {
        internal static void MakeGolemRoomInvariable(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(106)))
                return;

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStloc(29)))
                return;

            cursor.Emit(OpCodes.Ldsfld, typeof(GolemBodyBehaviorOverride).GetField("ArenaWidth", Utilities.UniversalBindingFlags));
            cursor.Emit(OpCodes.Stloc, 31);
            cursor.Emit(OpCodes.Ldsfld, typeof(GolemBodyBehaviorOverride).GetField("ArenaHeight", Utilities.UniversalBindingFlags));
            cursor.Emit(OpCodes.Stloc, 32);
        }

        public void Load() => CalamityGenNewTemple += MakeGolemRoomInvariable;

        public void Unload() => CalamityGenNewTemple -= MakeGolemRoomInvariable;
    }

    public class DrawBlackEffectHook : IHookEdit
    {
        public static List<int> DrawCacheProjsOverSignusBlackening = new List<int>(Main.maxProjectiles);
        public static List<int> DrawCacheAdditiveLighting = new List<int>(Main.maxProjectiles);
        internal static void DrawBlackout(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<MoonlordDeathDrama>("DrawWhite")))
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
                    Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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

            if (!c.TryGotoNext(i => i.MatchLdcI4(ItemID.SuperAbsorbantSponge)))
                return;

            c.Index++;
            c.EmitDelegate<Action>(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && PoDWorld.InfernumMode)
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
            if (OldOnesArmyMinibossChanges.GetMinibossToSummon(out int minibossID) && PoDWorld.InfernumMode)
                return slainMonsterID == minibossID ? 99999 : 0;

            return orig(slainMonsterID);
        }

        public void Load() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth += GiveDD2MinibossesPointPriority;

        public void Unload() => On.Terraria.GameContent.Events.DD2Event.GetMonsterPointsWorth -= GiveDD2MinibossesPointPriority;
    }
}