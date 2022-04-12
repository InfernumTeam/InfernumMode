using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class ExpandingFireball : ModProjectile
    {
        public int TotalPointsInExplosion => (int)Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fireball");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = (Projectile.frameCounter / 5) % Main.projFrames[Projectile.type];
        }

        public override void Kill(int timeLeft)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < TotalPointsInExplosion; i++)
                {
                    float angle = MathHelper.TwoPi / TotalPointsInExplosion * i;
                    Utilities.NewProjectileBetter(Projectile.Center, angle.ToRotationVector2() * 12f, ProjectileID.CultistBossFireBall, Projectile.damage, 0);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<LethalLavaBurn>(), 180);
        }
    }
}
