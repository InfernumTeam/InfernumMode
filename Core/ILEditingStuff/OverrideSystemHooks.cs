using System.Linq;
using CalamityMod.NPCs;
using CalamityMod.UI;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Luminance.Core.Balancing;
using Luminance.Core.Hooking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using static InfernumMode.Core.ILEditingStuff.HookManager;

namespace InfernumMode.Core.ILEditingStuff
{
    public class OverrideSystemHooks : ICustomDetourProvider
    {
        void ICustomDetourProvider.ModifyMethods()
        {
            HookHelper.ModifyMethodWithDetour(FindFrameMethod, OverrideSystemHooks.FindFrameDetourMethod);
            HookHelper.ModifyMethodWithDetour(CalPreAIMethod, OverrideSystemHooks.CalPreAIDetourMethod);
            HookHelper.ModifyMethodWithDetour(CalGlobalNPCPredrawMethod, OverrideSystemHooks.CalGlobalNPCPredrawDetourMethod);
            HookHelper.ModifyMethodWithDetour(CalGlobalNPCPostdrawMethod, OverrideSystemHooks.CalGlobalNPCPostdrawDetourMethod);
            InternalBalancingManager.AfterHPBalancingEvent += InternalBalancingManager_AfterHPBalancingEvent;
        }

        // This event is called after orig in NPCLoader.SetDefaults(NPC npc, bool createModNPC) and luminances hp balancing, making it the perfect place to run our own non overriden defaults afterwards.
        private static void InternalBalancingManager_AfterHPBalancingEvent(NPC npc)
        {
            // This exists to only set them once at the end, as opposed to inside orig as well.
            GlobalNPCOverrides.ShouldSetDefaults = true;

            if (InfernumMode.CanUseCustomAIs && npc.TryGetGlobalNPC<GlobalNPCOverrides>(out var global))
                global.SetDefaults(npc);

            GlobalNPCOverrides.ShouldSetDefaults = false;

            if (BossHealthBarManager.Bars.Any(b => b.NPCIndex == npc.whoAmI))
                BossHealthBarManager.Bars.First(b => b.NPCIndex == npc.whoAmI).InitialMaxLife = npc.lifeMax;
        }

        // Don't let Calamity's PreAI run on vanilla bosses to avoid ai conflicts.
        internal static bool CalPreAIDetourMethod(Orig_CalPreAIDelegate orig, CalamityGlobalNPC self, NPC npc)
        {
            var container = NPCBehaviorOverride.BehaviorOverrideSet[npc.type];

            if (InfernumMode.CanUseCustomAIs && container is not null && container.HasPreAI && npc.ModNPC == null)
                return false;

            return orig(self, npc);
        }

        // Only run Infernum's findframe if it exists.
        internal static void FindFrameDetourMethod(Orig_FindFrameDelegate orig, NPC npc, int frameHeight)
        {
            var container = NPCBehaviorOverride.BehaviorOverrideSet[npc.type];
            if (InfernumMode.CanUseCustomAIs && container is not null && container.HasFindFrame && !npc.IsABestiaryIconDummy)
            {
                if (npc.TryGetGlobalNPC<GlobalNPCOverrides>(out var global))
                {
                    global.FindFrame(npc, frameHeight);
                    return;
                }
            }

            orig(npc, frameHeight);
        }

        internal static bool CalGlobalNPCPredrawDetourMethod(Orig_CalGlobalNPCPredrawMethod orig, CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (npc.type == NPCID.GolemHeadFree)
                    return false;
            }

            return orig(self, npc, spriteBatch, screenPos, drawColor);
        }

        internal static void CalGlobalNPCPostdrawDetourMethod(Orig_CalGlobalNPCPostdrawMethod orig, CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (npc.type == NPCID.BrainofCthulhu)
                    return;
                if (npc.type == NPCID.Creeper)
                    return;
            }

            orig(self, npc, spriteBatch, screenPos, drawColor);
        }
    }
}
