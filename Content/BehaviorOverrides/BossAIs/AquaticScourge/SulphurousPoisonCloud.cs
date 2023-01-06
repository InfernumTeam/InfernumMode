using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class SulphurousPoisonCloud : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Cloud");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1800;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.4f, 0.54f, 0.21f);

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 9 % Main.projFrames[Projectile.type];

            Projectile.velocity *= 0.995f;

            if (Projectile.timeLeft < 180)
            {
                Projectile.damage = 0;
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.03f, 0f, 1f);
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();
            }
            else if (Projectile.alpha > 30)
            {
                Projectile.alpha -= 30;
                if (Projectile.alpha < 30)
                {
                    Projectile.alpha = 30;
                }
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.timeLeft >= 180;
    }
}
