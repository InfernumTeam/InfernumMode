using CalamityMod.Events;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class BossRushDamagePlayer : ModPlayer
    {
        public const int MinBossRushDamage = 500;

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (modifiers.FinalDamage.Base <= 0f)
                return;

            if (InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                modifiers.FinalDamage.Base = Clamp(modifiers.FinalDamage.Base, MinBossRushDamage + Main.rand.Next(35), float.MaxValue);
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (modifiers.FinalDamage.Base <= 0f)
                return;

            if (InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                modifiers.FinalDamage.Base = Clamp(modifiers.FinalDamage.Base, MinBossRushDamage + Main.rand.Next(35), float.MaxValue);
        }
    }
}
