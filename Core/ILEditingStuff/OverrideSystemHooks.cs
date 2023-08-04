using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static InfernumMode.Core.ILEditingStuff.HookManager;

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
                GlobalHookList<GlobalNPC> list = (GlobalHookList<GlobalNPC>)typeof(NPCLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                foreach (GlobalNPC global in list.Enumerate(npc))
                {
                    Type type = global.GetType().BaseType;
                    bool overridableNPC = global is null or CalamityGlobalNPC;

                    if (InfernumMode.EmodeIsActive)
                    {
                        Type emodeGlobalNPCType = InfernumMode.FargowiltasSouls.Code.GetType("FargowiltasSouls.EternityMode.EModeNPCBehaviour");
                        overridableNPC |= global.GetType().IsSubclassOf(emodeGlobalNPCType);
                    }

                    if (overridableNPC && OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                        continue;

                    result &= global.PreAI(npc);
                }
                if (result && npc.ModNPC != null)
                    return npc.ModNPC.PreAI();
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
                    npc.Infernum().SetDefaults(npc);
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
                GlobalHookList<GlobalNPC> list = (GlobalHookList<GlobalNPC>)typeof(NPCLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
                    return npc.Infernum().PreDraw(npc, spriteBatch, screenPosition, drawColor);

                foreach (GlobalNPC global in list.Enumerate(npc))
                {
                    if (!global.Instance(npc).PreDraw(npc, spriteBatch, screenPosition, drawColor))
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
                GlobalHookList<GlobalNPC> list = (GlobalHookList<GlobalNPC>)typeof(NPCLoader).GetField("HookFindFrame", Utilities.UniversalBindingFlags).GetValue(null);

                int type = npc.type;
                if (npc.ModNPC != null && npc.ModNPC.AnimationType > 0)
                    npc.type = npc.ModNPC.AnimationType;

                if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(type) && InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
                {
                    npc.Infernum().FindFrame(npc, frameHeight);
                    return;
                }

                npc.VanillaFindFrame(frameHeight, npc.isLikeATownNPC, npc.ModNPC?.AnimationType is > 0 ? npc.ModNPC.AnimationType : npc.type);
                npc.type = type;
                npc.ModNPC?.FindFrame(frameHeight);

                foreach (GlobalNPC global in list.Enumerate(npc))
                    global.Instance(npc).FindFrame(npc, frameHeight);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        internal static void NPCCheckDeadChange(ILContext context)
        {
            ILCursor cursor = new(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                GlobalHookList<GlobalNPC> list = (GlobalHookList<GlobalNPC>)typeof(NPCLoader).GetField("HookCheckDead", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                if (npc.ModNPC != null)
                    result = npc.ModNPC.CheckDead();

                foreach (GlobalNPC global in list.Enumerate(npc))
                {
                    if (global is GlobalNPCOverrides globalOverrides)
                    {
                        bool result2 = globalOverrides.CheckDead(npc);
                        if (!result2)
                            return false;
                    }
                    result &= global.Instance(npc).CheckDead(npc);
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
                GlobalHookList<GlobalProjectile> list = (GlobalHookList<GlobalProjectile>)typeof(ProjectileLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);

                bool result = true;
                foreach (GlobalProjectile global in list.Enumerate(projectile))
                {
                    bool overridableProjectile = global is null || global is CalamityGlobalProjectile || global.GetType().FullName.Contains("EModeGlobalProjectile");
                    if (overridableProjectile && OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                        continue;

                    result &= global.PreAI(projectile);
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
            GlobalHookList<GlobalProjectile> list = (GlobalHookList<GlobalProjectile>)typeof(ProjectileLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);

            bool result = true;
            foreach (GlobalProjectile global in list.Enumerate(projectile))
            {
                // The InfernumMode.CanUseCustomAIs check is necessary to ensure that Calamity's global shroomed effect isn't disabled when the mod is enabled for Infernum itself isn't in the world.
                bool overridableProjectile = global is null || global is CalamityGlobalProjectile || global.GetType().FullName.Contains("EModeGlobalProjectile");
                if (overridableProjectile && InfernumMode.CanUseCustomAIs)
                    continue;

                result &= global.PreDraw(projectile, ref lightColor);
            }
            if (result && projectile.ModProjectile != null)
                return projectile.ModProjectile.PreDraw(ref lightColor);

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
