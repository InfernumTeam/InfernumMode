using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class OverrideSystemHooks : IHookEdit
    {
        internal static void NPCPreAIChange(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                var globalNPCs = (Instanced<GlobalNPC>[])typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).GetValue(npc);
                HookList<GlobalNPC> list = (HookList<GlobalNPC>)typeof(NPCLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                foreach (GlobalNPC g in list.Enumerate(globalNPCs))
                {
                    Type t = g.GetType().BaseType;
                    bool overridableNPC = g is null or CalamityGlobalNPC;
                    if (InfernumMode.EmodeIsActive)
                    {
                        Type emodeGlobalNPCType = InfernumMode.FargowiltasSouls.Code.GetType("FargowiltasSouls.EternityMode.EModeNPCBehaviour");
                        overridableNPC |= g.GetType().IsSubclassOf(emodeGlobalNPCType);
                    }

                    if (overridableNPC && OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                        continue;

                    result &= g.PreAI(npc);
                }
                if (result && npc.ModNPC != null)
                {
                    return npc.ModNPC.PreAI();
                }
                return result;
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCSetDefaultsChange(ILContext context)
        {
            ILCursor cursor = new(context);
            while (cursor.TryGotoNext(MoveType.Before, c => c.MatchRet())) { }

            cursor.Remove();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, bool>((npc, createModNPC) =>
            {
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().SetDefaults(npc);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCPreDrawChange(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.Emit(OpCodes.Ldarg_3);
            cursor.EmitDelegate(new Func<NPC, SpriteBatch, Vector2, Color, bool>((npc, spriteBatch, screenPosition, drawColor) =>
            {
                var globalNPCs = (Instanced<GlobalNPC>[])typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).GetValue(npc);
                HookList<GlobalNPC> list = (HookList<GlobalNPC>)typeof(NPCLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
                    return npc.GetGlobalNPC<GlobalNPCDrawEffects>().PreDraw(npc, spriteBatch, screenPosition, drawColor);

                foreach (GlobalNPC g in list.Enumerate(globalNPCs))
                {
                    if (!g.Instance(npc).PreDraw(npc, spriteBatch, screenPosition, drawColor))
                        return false;
                }
                return npc.ModNPC == null || npc.ModNPC.PreDraw(spriteBatch, screenPosition, drawColor);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCFindFrameChange(ILContext context)
        {
            ILCursor cursor = new(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, int>((npc, frameHeight) =>
            {
                var globalNPCs = (Instanced<GlobalNPC>[])typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).GetValue(npc);
                HookList<GlobalNPC> list = (HookList<GlobalNPC>)typeof(NPCLoader).GetField("HookFindFrame", Utilities.UniversalBindingFlags).GetValue(null);

                int type = npc.type;
                if (npc.ModNPC != null && npc.ModNPC.AnimationType > 0)
                {
                    npc.type = npc.ModNPC.AnimationType;
                }
                if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(type) && InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().FindFrame(npc, frameHeight);
                    return;
                }
                npc.VanillaFindFrame(frameHeight, npc.isLikeATownNPC, npc.ModNPC?.AnimationType is > 0 ? npc.ModNPC.AnimationType : npc.type);
                npc.type = type;
                npc.ModNPC?.FindFrame(frameHeight);
                foreach (GlobalNPC g in list.Enumerate(globalNPCs))
                    g.Instance(npc).FindFrame(npc, frameHeight);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCCheckDeadChange(ILContext context)
        {
            ILCursor cursor = new(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                var globalNPCs = (Instanced<GlobalNPC>[])typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).GetValue(npc);
                HookList<GlobalNPC> list = (HookList<GlobalNPC>)typeof(NPCLoader).GetField("HookCheckDead", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                if (npc.ModNPC != null)
                {
                    result = npc.ModNPC.CheckDead();
                }
                foreach (GlobalNPC g in list.Enumerate(globalNPCs))
                {
                    if (g is GlobalNPCOverrides g2)
                    {
                        bool result2 = g2.CheckDead(npc);
                        if (!result2)
                            return false;
                    }
                    result &= g.Instance(npc).CheckDead(npc);
                }
                return result;
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void ProjectilePreAIChange(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<Projectile, bool>(projectile =>
            {
                var globalProjectiles = (Instanced<GlobalProjectile>[])typeof(Projectile).GetField("globalProjectiles", Utilities.UniversalBindingFlags).GetValue(projectile);
                HookList<GlobalProjectile> list = (HookList<GlobalProjectile>)typeof(ProjectileLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                foreach (GlobalProjectile g in list.Enumerate(globalProjectiles))
                {
                    bool overridableProjectile = g is null || g is CalamityGlobalProjectile || g.GetType().FullName.Contains("EModeGlobalProjectile");
                    if (overridableProjectile && OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                        continue;

                    result &= g.PreAI(projectile);
                }
                if (result && projectile.ModProjectile != null)
                    return projectile.ModProjectile.PreAI();
                return result;
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal delegate bool PreDrawDelegate(Projectile projectile, ref Color lightColor);

        internal static bool ProjectilePreDrawDelegateFuckYou(Projectile projectile, ref Color lightColor)
        {
            var globalProjectiles = (Instanced<GlobalProjectile>[])typeof(Projectile).GetField("globalProjectiles", Utilities.UniversalBindingFlags).GetValue(projectile);
            HookList<GlobalProjectile> list = (HookList<GlobalProjectile>)typeof(ProjectileLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);

            bool result = true;
            if (globalProjectiles != null)
            {
                foreach (GlobalProjectile g in list.Enumerate(globalProjectiles))
                {
                    // "Fixed so good that it happened to me and now I need to redo everything"
                    // Well, have fun with your shroomed nohits, I suppose.
                    bool overridableProjectile = g is null || g is CalamityGlobalProjectile || g.GetType().FullName.Contains("EModeGlobalProjectile");
                    if (overridableProjectile && InfernumMode.CanUseCustomAIs)
                        continue;

                    result &= g.PreDraw(projectile, ref lightColor);
                }
            }
            if (result && projectile.ModProjectile != null)
            {
                return projectile.ModProjectile.PreDraw(ref lightColor);
            }
            return result;
        }

        internal static void ProjectilePreDrawChange(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<PreDrawDelegate>(ProjectilePreDrawDelegateFuckYou);
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