using CalamityMod.NPCs;
using InfernumMode.GlobalInstances;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class OverrideSystemHooks : IHookEdit
    {
        private static readonly object hookPreAI = typeof(NPCLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);
        private static readonly FieldInfo hookListArrayField = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags);

        internal static void NPCPreAIChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                GlobalNPC[] arr = hookListArrayField.GetValue(hookPreAI) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityGlobalNPC &&
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

        internal static void NPCSetDefaultsChange(ILContext context)
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
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
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

        internal static void NPCPreDrawChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(new Func<NPC, SpriteBatch, Color, bool>((npc, spriteBatch, drawColor) =>
            {
                object instance = typeof(NPCLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];

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

        internal static void NPCFindFrameChange(ILContext context)
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
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    globalNPC.Instance(npc).FindFrame(npc, frameHeight);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCCheckDeadChange(ILContext context)
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
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
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

        internal static void ProjectilePreAIChange(ILContext context)
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

        internal static void ProjectilePreDrawChange(ILContext context)
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

        public void Load()
        {
            ModifyPreAINPC += NPCPreAIChange;
            ModifySetDefaultsNPC += NPCSetDefaultsChange;
            ModifyFindFrameNPC += NPCFindFrameChange;
            ModifyPreDrawNPC += NPCPreDrawChange;
            ModifyCheckDead += NPCCheckDeadChange;
            ModifyPreAIProjectile += ProjectilePreAIChange;
            ModifyPreDrawProjectile += ProjectilePreDrawChange;
        }

        public void Unload()
        {
            ModifyPreAINPC -= NPCPreAIChange;
            ModifySetDefaultsNPC -= NPCSetDefaultsChange;
            ModifyFindFrameNPC -= NPCFindFrameChange;
            ModifyPreDrawNPC -= NPCPreDrawChange;
            ModifyCheckDead -= NPCCheckDeadChange;
            ModifyPreAIProjectile -= ProjectilePreAIChange;
            ModifyPreDrawProjectile -= ProjectilePreDrawChange;
        }
    }
}