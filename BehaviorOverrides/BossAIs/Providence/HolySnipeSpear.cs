using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class HolySnipeSpear : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public const int ProjectileSpawnRate = 5;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            if (Main.myPlayer == Projectile.owner && Time % 15f == 14f)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    float shootSpeed = 15.5f;
                    Vector2 shootVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(35f) * i + MathHelper.Pi) * shootSpeed;
                    Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<HolySpear3>(), 250, 0f);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
