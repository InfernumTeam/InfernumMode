using CalamityMod.Buffs.DamageOverTime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonHeatFlashFireball : ModProjectile
    {
        public const int Lifetime = 720;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 50;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            if (Projectile.frameCounter++ % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            Projectile.gfxOffY = -36;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.timeLeft < Lifetime - 30;

        public override void Kill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, 6);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<LethalLavaBurn>(), 180);
        }
    }
}