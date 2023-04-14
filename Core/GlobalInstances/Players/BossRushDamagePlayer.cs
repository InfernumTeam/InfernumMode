using CalamityMod.Events;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class BossRushDamagePlayer : ModPlayer
    {
        public const int MinBossRushDamage = 500;

        public override void ModifyHitByProjectile(Projectile proj, ref int damage, ref bool crit)
        {
            if (damage <= 0)
                return;

            if (damage <= MinBossRushDamage && InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                damage = MinBossRushDamage + Main.rand.Next(35);
        }

        public override void ModifyHitByNPC(NPC npc, ref int damage, ref bool crit)
        {
            if (damage <= 0)
                return;

            if (damage <= MinBossRushDamage && InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                damage = MinBossRushDamage + Main.rand.Next(35);
        }
    }
}
