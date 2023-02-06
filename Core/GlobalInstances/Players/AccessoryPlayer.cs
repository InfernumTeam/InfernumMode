using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class AccessoryPlayer : ModPlayer
    {
        public bool Purity;

        public override void ResetEffects()
        {
            Purity = false;
        }

        public override void UpdateDead()
        {
            Purity = false;
        }

        public override void PostUpdateEquips()
        {
            if (Purity)
            {
                Player.GetDamage<GenericDamageClass>() += 0.3f;
                Player.GetAttackSpeed<GenericDamageClass>() += 0.3f;
                Player.buffImmune[ModContent.BuffType<Nightwither>()] = true;
            }
        }

        public override void ModifyHitNPC(Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            if (Purity && crit)
                crit = false;
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (Purity && crit)
                crit = false;
        }
    }
}
