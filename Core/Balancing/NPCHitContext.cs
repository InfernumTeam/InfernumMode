using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Balancing
{
    public readonly struct NPCHitContext
    {
        public readonly int Pierce;

        public readonly int Damage;

        public readonly int? ProjectileIndex;

        public readonly int? ProjectileType;

        public readonly bool IsStealthStrike;

        public readonly ClassType? Class;

        public readonly DamageSourceType DamageSource;

        public NPCHitContext(int pierce, int damage, int? projectileIndex, int? projectileType, bool isStealthStrike, ClassType? classType,  DamageSourceType damageSource)
        {
            Pierce = pierce;
            Damage = damage;
            ProjectileIndex = projectileIndex;
            ProjectileType = projectileType;
            IsStealthStrike = isStealthStrike;
            Class = classType;
            DamageSource = damageSource;
        }

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
