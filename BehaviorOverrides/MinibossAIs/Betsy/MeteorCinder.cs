using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Betsy
{
    public class MeteorCinder : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cinder");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            if (Time > 4f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 6, 0f, 0f, 100, default, 0.75f);
                    fire.noGravity = true;
                    fire.velocity = Vector2.Zero;
                    fire.scale *= 1.9f;
                }
            }

            // If this projectile is not close to death, home in.
            if (Time > 35f)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                if (!projectile.WithinRange(target.Center, 50f))
                    projectile.velocity = (projectile.velocity * 59f + projectile.SafeDirectionTo(target.Center) * 19f) / 60f;
            }
            Time++;
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
