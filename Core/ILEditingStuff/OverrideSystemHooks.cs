using System.Linq;
using System.Reflection;
using CalamityMod.NPCs;
using CalamityMod.UI;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Luminance.Core.Balancing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.ILEditingStuff
{
    internal sealed class OverrideSystemHooks : ModSystem
    {
        public static MethodInfo? FindFrameMethod = typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags);
        public delegate void Orig_FindFrameDelegate(NPC npc, int frameHeight);
        public static Hook? FindFrame_Detour_Hook;

        public static MethodInfo? CalamityGlobalNPCPreAIMethod = typeof(CalamityGlobalNPC).GetMethod("PreAI", Utilities.UniversalBindingFlags);
        public delegate bool Orig_CalPreAIDelegate(CalamityGlobalNPC self, NPC npc);
        public static Hook? CalPreAI_Detour_Hook;

        public static MethodInfo? CalGlobalNPCPredrawMethod = typeof(CalamityGlobalNPC).GetMethod("PreDraw", Utilities.UniversalBindingFlags);
        public delegate bool Orig_CalGlobalNPCPredrawMethod(CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);
        public static Hook? CalGlobalNPCPredraw_Detour_Hook;

        public static MethodInfo? CalGlobalNPCPostdrawMethod = typeof(CalamityGlobalNPC).GetMethod("PostDraw", Utilities.UniversalBindingFlags);
        public delegate void Orig_CalGlobalNPCPostdrawMethod(CalamityGlobalNPC self, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);
        public static Hook? CalGlobalNPCPostdraw_Detour_Hook;

        public override void OnModLoad()
        {
            if (FindFrameMethod != null)
            {
                FindFrame_Detour_Hook = new(FindFrameMethod, OverrideSystemHooks.FindFrameDetourMethod);
                FindFrame_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalamityGlobalNPCPreAIMethod != null)
            {
                CalPreAI_Detour_Hook = new(CalamityGlobalNPCPreAIMethod, OverrideSystemHooks.CalPreAIDetourMethod);
                CalPreAI_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalGlobalNPCPredrawMethod != null)
            {
                CalGlobalNPCPredraw_Detour_Hook = new(CalGlobalNPCPredrawMethod, OverrideSystemHooks.CalGlobalNPCPredrawDetourMethod);
                CalGlobalNPCPredraw_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

            if (CalGlobalNPCPostdrawMethod != null)
            {
                CalGlobalNPCPostdraw_Detour_Hook = new(CalGlobalNPCPostdrawMethod, OverrideSystemHooks.CalGlobalNPCPostdrawDetourMethod);
                CalGlobalNPCPostdraw_Detour_Hook?.Apply();
            }
            else InfernumMode.Instance.Logger.Error(this + " returned null on getting MethodInfo");

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
