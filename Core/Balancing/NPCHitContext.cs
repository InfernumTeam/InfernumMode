using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.Balancing
{
    public struct NPCHitContext
    {
        public int Pierce;
        public int Damage;
        public int? ProjectileIndex;
        public int? ProjectileType;
        public bool IsStealthStrike;
        public ClassType? Class;
        public DamageSourceType DamageSource;

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

            return new NPCHitContext()
            {
                Pierce = proj.penetrate,
                Damage = proj.damage,
                ProjectileType = proj.type,
                ProjectileIndex = proj.whoAmI,
                Class = classType,
                IsStealthStrike = proj.active && proj.Calamity().stealthStrike,
                DamageSource = DamageSourceType.FriendlyProjectile
            };
        }
    }
}