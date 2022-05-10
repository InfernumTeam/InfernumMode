using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class ExpandingFireball : ModProjectile
    {
        public int TotalPointsInExplosion => (int)projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fireball");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 36;
            projectile.height = 36;
            projectile.hostile = true;
            projectile.timeLeft = 180;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = (projectile.frameCounter / 5) % Main.projFrames[projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            if (Main.myPlayer == projectile.owner)
            {
                for (int i = 0; i < TotalPointsInExplosion; i++)
                {
                    float angle = MathHelper.TwoPi / TotalPointsInExplosion * i;
                    Utilities.NewProjectileBetter(projectile.Center, angle.ToRotationVector2() * 12f, ProjectileID.CultistBossFireBall, projectile.damage, 0);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<LethalLavaBurn>(), 180);
        }
    }
}
