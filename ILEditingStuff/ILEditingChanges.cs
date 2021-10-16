using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.UI;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs;
using InfernumMode.GlobalInstances;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.ILEditingStuff
{
    public class ILEditingChanges
    {
        public static List<int> DrawCacheProjsOverSignusBlackening = new List<int>(Main.maxProjectiles);
        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyTextUtility
        {
            add => HookEndpointManager.Modify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DevourerOfGodsPhase2SkyFade
        {
            add => HookEndpointManager.Modify(typeof(DoGSkyS).GetMethod("GetIntensity", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DoGSkyS).GetMethod("GetIntensity", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => HookEndpointManager.Modify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => HookEndpointManager.Modify(typeof(CalamityWorld).GetMethod("PostUpdate", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityWorld).GetMethod("PostUpdate", Utilities.UniversalBindingFlags), value);
        }

        public static void ILEditingLoad()
        {
            On.Terraria.Gore.NewGore += RemoveCultistGore;
            IL.Terraria.Player.ItemCheck += ItemCheckChange;
            IL.Terraria.Main.DrawTiles += WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += WoFLavaColorChange2;
            // IL.Terraria.Main.DoDraw += DrawSignusBlack;
			On.Terraria.Player.KillMe += RemoveTheDamnCancerousDoGInstakill;
            ModifyPreAINPC += NPCPreAIChange;
            ModifySetDefaultsNPC += NPCSetDefaultsChange;
            ModifyFindFrameNPC += NPCFindFrameChange;
            ModifyPreDrawNPC += NPCPreDrawChange;
            ModifyCheckDead += NPCCheckDeadChange;
            ModifyPreAIProjectile += ProjectilePreAIChange;
            ModifyPreDrawProjectile += ProjectilePreDrawChange;
            ModeIndicatorUIDraw += DrawInfernumIcon;
            CalamityWorldPostUpdate += PermitODRain;
        }

		public static void ILEditingUnload()
        {
            On.Terraria.Gore.NewGore -= RemoveCultistGore;
            IL.Terraria.Player.ItemCheck -= ItemCheckChange;
            IL.Terraria.Main.DrawTiles -= WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= WoFLavaColorChange2;
            // IL.Terraria.Main.DoDraw -= DrawSignusBlack;
            On.Terraria.Player.KillMe -= RemoveTheDamnCancerousDoGInstakill;
            ModifyPreAINPC -= NPCPreAIChange;
            ModifySetDefaultsNPC -= NPCSetDefaultsChange;
            ModifyFindFrameNPC -= NPCFindFrameChange;
            ModifyPreDrawNPC -= NPCPreDrawChange;
            ModifyCheckDead -= NPCCheckDeadChange;
            ModifyPreAIProjectile -= ProjectilePreAIChange;
            ModifyPreDrawProjectile -= ProjectilePreDrawChange;
            ModeIndicatorUIDraw -= DrawInfernumIcon;
            CalamityWorldPostUpdate -= PermitODRain;
        }

        // Why.
        internal static void RemoveTheDamnCancerousDoGInstakill(On.Terraria.Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
        {
            if (damageSource.SourceCustomReason == self.name + "'s essence was consumed by the devourer.")
                return;

            orig(self, damageSource, dmg, hitDirection, pvp);
        }

        internal static int RemoveCultistGore(On.Terraria.Gore.orig_NewGore orig, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return Main.maxDust;

            return orig(Position, Velocity, Type, Scale);
        }

        internal static Color BlendLavaColors(Color baseColor)
        {
            if (InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
            {
                float lifeRatio = Main.npc[Main.wof].life / (float)Main.npc[Main.wof].lifeMax;
                float fade = (float)Math.Pow(1f - lifeRatio, 0.4f);
                Color endColor = Color.Lerp(Color.DarkRed, Color.Black, 0.56f);

                return Color.Lerp(baseColor, endColor, fade);
            }
            return baseColor;
        }

        private static void WoFLavaColorChange(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(i => i.MatchLdsfld<Main>("liquidTexture")))
                return;

            if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchLdsfld<Main>("spriteBatch")))
                return;

            cursor.Emit(OpCodes.Ldloc, 151);
            cursor.Emit(OpCodes.Ldloc, 155);
            cursor.EmitDelegate<Func<int, Color, Color>>((liquidType, baseColor) =>
            {
                if (liquidType == 1 && InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
                    baseColor = BlendLavaColors(baseColor);

                return baseColor;
            });
            cursor.Emit(OpCodes.Stloc, 155);
        }

        private static void WoFLavaColorChange2(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCallvirt<TileBatch>("Draw")))
                return;

            if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchLdsfld<Main>("tileBatch")))
                return;

            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.Emit(OpCodes.Ldloc, 9);
            cursor.EmitDelegate<Func<int, VertexColors, VertexColors>>((liquidType, vertexColor) =>
            {
                if (liquidType == 1 && InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
				{
                    vertexColor.BottomLeftColor = BlendLavaColors(vertexColor.BottomLeftColor);
                    vertexColor.BottomRightColor = BlendLavaColors(vertexColor.BottomRightColor);
                    vertexColor.TopLeftColor = BlendLavaColors(vertexColor.TopLeftColor);
                    vertexColor.TopRightColor = BlendLavaColors(vertexColor.TopRightColor);
                }
                return vertexColor;
            });
            cursor.Emit(OpCodes.Stloc, 9);
        }

        private static void ItemCheckChange(ILContext instructionContext)
        {
            var c = new ILCursor(instructionContext);
            // Attempt to match a section of code which involves the value 3032. Other values can be used, including not just numbers.
            if (!c.TryGotoNext(i => i.MatchLdcI4(3032)))
                return;
            c.Index++;
            c.EmitDelegate<Action>(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && PoDWorld.InfernumMode)
                {
                    Main.LocalPlayer.noBuilding = true;
                }
            });
        }

        private static void NPCPreAIChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                object instance = typeof(NPCLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityMod.NPCs.CalamityGlobalNPC &&
                        OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(npc).PreAI(npc))
                    {
                        return false;
                    }
                }
                return npc.modNPC == null || npc.modNPC.PreAI();
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCSetDefaultsChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, bool>((npc, createModNPC) =>
            {
                GlobalNPC[] instancedGlobals = typeof(NPCLoader).GetField("InstancedGlobals", Utilities.UniversalBindingFlags).GetValue(null) as GlobalNPC[];
                if (npc.type >= 580)
                {
                    if (createModNPC)
                    {
                        typeof(NPC).GetProperty("modNPC", Utilities.UniversalBindingFlags).SetValue(npc, NPCLoader.GetNPC(npc.type).NewInstance(npc));
                    }
                    else
                    {
                        Array.Resize(ref npc.buffImmune, BuffLoader.BuffCount);
                    }
                }
                typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).SetValue(npc, (from g in instancedGlobals.ToList() select g.NewInstance(npc)).ToArray());
                npc.modNPC?.SetDefaults();
                object instance = typeof(NPCLoader).GetField("HookSetDefaults", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    globalNPC.Instance(npc).SetDefaults(npc);
                }
                int oldLifeMax = npc.lifeMax;
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().SetDefaults(npc);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCPreDrawChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(new Func<NPC, SpriteBatch, Color, bool>((npc, spriteBatch, drawColor) =>
            {
                object instance = typeof(NPCLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalNPC[];

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                    return npc.GetGlobalNPC<GlobalNPCDrawEffects>().PreDraw(npc, spriteBatch, drawColor);

                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    if (!globalNPC.Instance(npc).PreDraw(npc, spriteBatch, drawColor))
                        return false;
                }
                return npc.modNPC == null || npc.modNPC.PreDraw(spriteBatch, drawColor);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCFindFrameChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, int>((npc, frameHeight) =>
            {
                int type = npc.type;
                if (npc.modNPC != null && npc.modNPC.animationType > 0)
                {
                    npc.type = npc.modNPC.animationType;
                }
                if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(type) && InfernumMode.CanUseCustomAIs)
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().FindFrame(npc, frameHeight);
                    return;
                }
                npc.VanillaFindFrame(frameHeight);
                npc.type = type;
                npc.modNPC?.FindFrame(frameHeight);
                object instance = typeof(NPCLoader).GetField("HookFindFrame", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    globalNPC.Instance(npc).FindFrame(npc, frameHeight);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCCheckDeadChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                bool result = true;
                if (npc.modNPC != null)
                {
                    result = npc.modNPC.CheckDead();
                }
                object instance = typeof(NPCLoader).GetField("HookCheckDead", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalNPC[];
                foreach (GlobalNPC g in arr)
                {
                    if (g is GlobalNPCOverrides)
                        return g.Instance(npc).CheckDead(npc);
                    result &= g.Instance(npc).CheckDead(npc);
                }
                return result;
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void ProjectilePreAIChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<Projectile, bool>(projectile =>
            {
                object instance = typeof(ProjectileLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalProjectile[] arr = typeof(ProjectileLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalProjectile[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalProjectile globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityMod.Projectiles.CalamityGlobalProjectile &&
                        OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(projectile).PreAI(projectile))
                    {
                        return false;
                    }
                }
                return projectile.modProjectile == null || projectile.modProjectile.PreAI();
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void ProjectilePreDrawChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(new Func<Projectile, SpriteBatch, Color, bool>((projectile, spriteBatch, lightColor) =>
            {
                object instance = typeof(ProjectileLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalProjectile[] arr = typeof(ProjectileLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalProjectile[];
                if (OverridingListManager.InfernumProjectilePreDrawOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreDrawOverrideList[projectile.type].DynamicInvoke(projectile, spriteBatch, lightColor);
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalProjectile globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityMod.Projectiles.CalamityGlobalProjectile)
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(projectile).PreDraw(projectile, spriteBatch, lightColor))
                    {
                        return false;
                    }
                }
                return projectile.modProjectile == null || projectile.modProjectile.PreDraw(spriteBatch, lightColor);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        public static float frameNumber = 0f;
        private static void DrawInfernumModeUI()
        {
            // The mode indicator should only be displayed when the inventory is open, to prevent obstruction.
            if (!Main.playerInventory)
                return;

            //3 times the number of total frames, so that each frame gets displayed 3 times
            if (frameNumber >= 186f)
                frameNumber = 0f;
            float row = (float)Math.Floor(frameNumber / 3);
            if (row >= 27)
                row = row - (((float)Math.Floor(row / 27)) * 27);

            // TODO - Replace this with Malice when it's added.
            bool defiledOn = false;

            Texture2D outerAreaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumBG");
            Texture2D armaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumArma");

            Rectangle areaFrame = outerAreaTexture.Frame(3, 27, (int)Math.Floor(frameNumber / 81), (int)row);
            Vector2 drawCenter = new Vector2(Main.screenWidth - 400f, 72f) + areaFrame.Size() * 0.5f;

            if (CalamityPlayer.areThereAnyDamnBosses)
            {
                Color drawColor = Color.Red * 0.4f;
                drawColor.A = 0;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTime * 4f).ToRotationVector2() * 5f;
                    Main.spriteBatch.Draw(outerAreaTexture, drawCenter + drawOffset, areaFrame, drawColor, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }
            Main.spriteBatch.Draw(outerAreaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (CalamityWorld.armageddon)
                Main.spriteBatch.Draw(armaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            frameNumber++;
            //Displays each frame for 2 ticks instead of 3 if there are any bosses by skipping the 3rd frame
            if (CalamityPlayer.areThereAnyDamnBosses && (frameNumber + 4) % 3 == 0)
                frameNumber++;
        }

        private static void DrawInfernumIcon(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Go to the last Ret and leave a marker to return to so that manual
            // drawing can be done.
            while (cursor.TryGotoNext(i => i.MatchRet())) { }

            ILLabel endOfMethod = cursor.DefineLabel();
            cursor.MarkLabel(endOfMethod);

            cursor.Index = 0;
            cursor.EmitDelegate<Action>(() =>
            {
                if (PoDWorld.InfernumMode)
                    DrawInfernumModeUI();
            });

            cursor.Emit(OpCodes.Ldsfld, typeof(PoDWorld).GetField("InfernumMode"));
            cursor.Emit(OpCodes.Brtrue, endOfMethod);
        }

        private static void DrawSignusBlack(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<MoonlordDeathDrama>("DrawWhite")))
                return;

            cursor.EmitDelegate<Action>(() =>
            {
                if (CalamityGlobalNPC.signus != -1)
                {
                    float fadeToBlack = Main.npc[CalamityGlobalNPC.signus].Infernum().ExtraAI[9];
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
            });
        }

        private static void PermitODRain(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStsfld<Main>("raining")))
                return;

            int start = cursor.Index - 1;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<CalamityNetcode>("SyncWorld")))
                return;

            int end = cursor.Index;
            cursor.Goto(start);
            cursor.RemoveRange(end - start);
            cursor.Emit(OpCodes.Nop);
        }
    }
}