using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.UI;
using CalamityMod.World;
using InfernumMode.FuckYouModeAIs.MainAI;
using InfernumMode.FuckYouModeAIs.Perforators;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.ILEditingStuff
{
	public class ILEditingChanges
    {
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

        public static void ILEditingLoad()
        {
            On.Terraria.Gore.NewGore += RemoveCultistGore;
            IL.Terraria.Player.ItemCheck += ItemCheckChange;
            IL.Terraria.Main.DrawTiles += WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += WoFLavaColorChange2;
            ModifyPreAINPC += NPCPreAIChange;
            ModifySetDefaultsNPC += NPCSetDefaultsChange;
            ModifyFindFrameNPC += NPCFindFrameChange;
			ModifyCheckDead += NPCCheckDeadChange;
            ModifyPreAIProjectile += ProjectilePreAIChange;
			ModeIndicatorUIDraw += DrawInfernumIcon;
        }

		public static void ILEditingUnload()
        {
            On.Terraria.Gore.NewGore -= RemoveCultistGore;
            IL.Terraria.Player.ItemCheck -= ItemCheckChange;
            IL.Terraria.Main.DrawTiles -= WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= WoFLavaColorChange2;
            ModifyPreAINPC -= NPCPreAIChange;
            ModifySetDefaultsNPC -= NPCSetDefaultsChange;
            ModifyFindFrameNPC -= NPCFindFrameChange;
            ModifyCheckDead -= NPCCheckDeadChange;
            ModifyPreAIProjectile -= ProjectilePreAIChange;
            ModeIndicatorUIDraw -= DrawInfernumIcon;
        }

        internal static int RemoveCultistGore(On.Terraria.Gore.orig_NewGore orig, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (PoDWorld.InfernumMode && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return Main.maxDust;

            return orig(Position, Velocity, Type, Scale);
        }

        internal static Color BlendLavaColors(Color baseColor)
        {
            if (PoDWorld.InfernumMode && Main.wof >= 0 && Main.npc[Main.wof].active)
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
                if (liquidType == 1 && PoDWorld.InfernumMode && Main.wof >= 0 && Main.npc[Main.wof].active)
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
                if (liquidType == 1 && PoDWorld.InfernumMode && Main.wof >= 0 && Main.npc[Main.wof].active)
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
                        OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type) && PoDWorld.InfernumMode)
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
                    npc.GetGlobalNPC<FuckYouModeDrawEffects>().SetDefaults(npc);
                }
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
                if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(type) && PoDWorld.InfernumMode)
                {
                    npc.GetGlobalNPC<FuckYouModeDrawEffects>().FindFrame(npc, frameHeight);
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
                    if (g is FuckYouModeAIsGlobal)
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

        private static void DrawInfernumModeUI()
		{
            // The mode indicator should only be displayed when the inventory is open, to prevent obstruction.
            if (!Main.playerInventory)
                return;

            // TODO - Replace this with Malice when it's added.
            bool defiledOn = false;

            Texture2D outerAreaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumBG");
            if (CalamityWorld.armageddon)
                outerAreaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumArmaBG");

            float pulseRate = 11f;
            if (CalamityPlayer.areThereAnyDamnBosses)
                pulseRate = 25f;
            Rectangle areaFrame = outerAreaTexture.Frame(1, 13, 0, (int)(Main.GlobalTime * pulseRate) % 13);
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
    }
}