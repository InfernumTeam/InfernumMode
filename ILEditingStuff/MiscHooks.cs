using CalamityMod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using static InfernumMode.ILEditingStuff.HookManager;

namespace InfernumMode.ILEditingStuff
{
    public class PermitOldDukeRainHook : IHookEdit
    {
        internal static void PermitODRain(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStsfld<Main>("raining")))
                return;

            int start = cursor.Index - 1;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<CalamityNetcode>("SyncWorld")))
                return;

            int end = cursor.Index;
            cursor.Goto(start);
            cursor.RemoveRange(end - start);
            cursor.Emit(OpCodes.Nop);
        }

        public void Load() => CalamityWorldPostUpdate += PermitODRain;

        public void Unload() => CalamityWorldPostUpdate -= PermitODRain;
    }

    public class NerfShellfishStaffDebuffHook : IHookEdit
    {
        internal static void NerfShellfishStaff(ILContext il)
        {
            ILCursor cursor = new(il);

            for (int j = 0; j < 2; j++)
            {
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(250)))
                    return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 150);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(50)))
                return;

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 30);
        }

        public void Load() => CalamityNPCLifeRegen += NerfShellfishStaff;

        public void Unload() => CalamityNPCLifeRegen -= NerfShellfishStaff;
    }

    /*
    public class UseDeathContactDamageHook : IHookEdit
    {
        internal static FieldInfo EnemyStatsField = typeof(NPCStats).GetNestedType("EnemyStats", Utilities.UniversalBindingFlags).GetField("ContactDamageValues", Utilities.UniversalBindingFlags);
        internal static void UseDeathContactDamageInInfernum(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.EmitDelegate<Action<NPC>>(CalculateContactDamage);
            cursor.Emit(OpCodes.Ret);
        }

        internal static void CalculateContactDamage(NPC npc)
        {
            double damageAdjustment = NPCStats.GetExpertDamageMultiplier(npc) * 2D;

            // Safety check: If for some reason the contact damage array is not initialized yet, set the NPC's damage to 1.
            SortedDictionary<int, int[]> enemyStats = (SortedDictionary<int, int[]>)EnemyStatsField.GetValue(null);
            bool exists = enemyStats.TryGetValue(npc.type, out int[] contactDamage);
            if (!exists)
                npc.damage = 1;

            int normalDamage = contactDamage[0];
            int expertDamage = contactDamage[1] == -1 ? -1 : (int)Math.Round(contactDamage[1] / damageAdjustment);
            int revengeanceDamage = contactDamage[2] == -1 ? -1 : (int)Math.Round(contactDamage[2] / damageAdjustment);
            int deathDamage = contactDamage[3] == -1 ? -1 : (int)Math.Round(contactDamage[3] / damageAdjustment);

            // If the assigned value would be -1, don't actually assign it. This allows for conditionally disabling the system.
            int damageToUse = (CalamityWorld.death || WorldSaveSystem.InfernumMode) ? deathDamage : CalamityWorld.revenge ? revengeanceDamage : Main.expertMode ? expertDamage : normalDamage;
            if (CalamityWorld.malice && damageToUse != -1)
                damageToUse = (int)Math.Round(damageToUse * CalamityGlobalNPC.MaliceModeDamageMultiplier);
            if (damageToUse != -1)
                npc.damage = damageToUse;
        }

        public void Load() => NPCStatsDefineContactDamage += UseDeathContactDamageInInfernum;

        public void Unload() => NPCStatsDefineContactDamage -= UseDeathContactDamageInInfernum;
    }
    */
}