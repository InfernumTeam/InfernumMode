using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.UI;
using CalamityMod.World;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using Terraria.ModLoader;

namespace InfernumMode.ILEditingStuff
{
    public static partial class HookManager
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

        public static event ILContext.Manipulator CalamityPlayerModifyHitByProjectile
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayer).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityNPCLifeRegen
        {
            add => HookEndpointManager.Modify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => HookEndpointManager.Modify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => HookEndpointManager.Modify(typeof(DriedSeafood).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DriedSeafood).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => HookEndpointManager.Modify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator NPCStatsDefineContactDamage
        {
            add => HookEndpointManager.Modify(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCStats).GetMethod("GetNPCDamage", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechIsPresent
        {
            add => HookEndpointManager.Modify(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(Draedon).GetMethod("get_ExoMechIsPresent", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechSelectionUIDraw
        {
            add => HookEndpointManager.Modify(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ExoMechSelectionUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ExoMechDropLoot
        {
            add => HookEndpointManager.Modify(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AresBody).GetMethod("DropExoMechLoot", Utilities.UniversalBindingFlags), value);
        }
    }
}