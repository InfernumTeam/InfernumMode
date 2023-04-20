using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    // Please keep these in alphabetical order if they're updated. -Dominic
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public delegate void HitEffectsDelegate(NPC npc, int hitDirection, double damage);

        public static event HitEffectsDelegate HitEffectsEvent;

        public delegate void OnKillDelegate(NPC npc);

        public static event OnKillDelegate OnKillEvent;

        public delegate bool StrikeNPCDelegate(NPC npc, ref double damage, int realDamage, int defense, ref float knockback, int hitDirection, ref bool crit);

        public static event StrikeNPCDelegate StrikeNPCEvent;
    }
}
