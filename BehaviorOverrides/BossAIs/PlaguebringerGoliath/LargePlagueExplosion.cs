using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class LargePlagueExplosion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
        }

        public override void AI()
        {
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(35f, 35f), 89);
                dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                dust.scale = Main.rand.NextFloat(1.1f, 1.35f);
                dust.noGravity = true;
            }
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 7;
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.Kill();
        }
    }
}
