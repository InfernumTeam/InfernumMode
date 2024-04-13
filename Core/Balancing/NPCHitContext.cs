using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Balancing
{
    public readonly struct NPCHitContext(int pierce, int damage, int? projectileIndex, int? projectileType, bool isStealthStrike, ClassType? classType, DamageSourceType damageSource)
    {
        public readonly int Pierce = pierce;

        public readonly int Damage = damage;

        public readonly int? ProjectileIndex = projectileIndex;

        public readonly int? ProjectileType = projectileType;

        public readonly bool IsStealthStrike = isStealthStrike;

        public readonly ClassType? Class = classType;

        public readonly DamageSourceType DamageSource = damageSource;

        public static NPCHitContext ConstructFromProjectile(Projectile proj)
        {
            ClassType? classType = null;
            if (proj.active)
            {
                if (proj.CountsAsClass<MeleeDamageClass>())
                    classType = ClassType.Melee;
                if (proj.CountsAsClass<RangedDamageClass>())
                    classType = ClassType.Ranged;
                if (proj.CountsAsClass<MagicDamageClass>())
                    classType = ClassType.Magic;
                if (proj.minion)
                    classType = ClassType.Summon;
                if (proj.CountsAsClass<RogueDamageClass>())
                    classType = ClassType.Rogue;
            }

            return new NPCHitContext(proj.penetrate, proj.damage, proj.whoAmI, proj.type, proj.active && proj.Calamity().stealthStrike, classType, DamageSourceType.FriendlyProjectile);
        }
    }
}
