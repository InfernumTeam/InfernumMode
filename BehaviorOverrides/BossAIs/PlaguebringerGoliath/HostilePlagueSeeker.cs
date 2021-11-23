using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class HostilePlagueSeeker : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Plague Seeker");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 150;
        }

        public override void AI()
        {
            if (Time > 4f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust plague = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 107, 0f, 0f, 100, default, 0.75f);
                    plague.noGravity = true;
                    plague.velocity = Vector2.Zero;
                }
            }

            // If this projectile is not close to death, home in.
            if (Time > 55f)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                if (!projectile.WithinRange(target.Center, 50f))
                    projectile.velocity = (projectile.velocity * 69f + projectile.SafeDirectionTo(target.Center) * 16f) / 70f;
            }
            Time++;
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
