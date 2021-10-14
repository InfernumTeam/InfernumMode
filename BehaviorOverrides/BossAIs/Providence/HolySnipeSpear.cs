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
        public ref float Time => ref projectile.ai[0];
        public const int ProjectileSpawnRate = 5;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Spear");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            if (Main.myPlayer == projectile.owner && Time % 20 == 19)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    float shootSpeed = 15.5f;
                    Vector2 shootVelocity = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(15f) * i + MathHelper.Pi) * shootSpeed;
                    Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<HolySpear3>(), 240, 0f);
                }
            }
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
		}

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
