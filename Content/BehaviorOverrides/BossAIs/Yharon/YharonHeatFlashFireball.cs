using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonHeatFlashFireball : ModProjectile
    {
        public const int Lifetime = 720;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Fire");
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
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.frameCounter++ % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            Projectile.gfxOffY = -36;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.timeLeft < Lifetime - 30;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, lightColor, 6f);
            return false;
        }
    }
}
