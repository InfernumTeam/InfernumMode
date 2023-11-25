using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances
{
    // Please keep these in alphabetical order if they're updated. -Dominic
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        public delegate void BossHeadSlotDelegate(NPC npc, ref int index);

        public static event BossHeadSlotDelegate BossHeadSlotEvent;

        public delegate void HitEffectsDelegate(NPC npc, ref NPC.HitInfo hit);

        public static event HitEffectsDelegate HitEffectsEvent;

        public delegate void OnKillDelegate(NPC npc);

        public static event OnKillDelegate OnKillEvent;

        public delegate bool StrikeNPCDelegate(NPC npc, ref NPC.HitModifiers modifiers);

        public static event StrikeNPCDelegate StrikeNPCEvent;

        public delegate void ModifyHitByProjectileDelegate(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers);

        public static event ModifyHitByProjectileDelegate ModifyHitByProjectileEvent;
    }
}
