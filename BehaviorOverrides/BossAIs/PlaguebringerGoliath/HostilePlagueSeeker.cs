using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class HostilePlagueSeeker : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        // TODO -- This projectile needs an actual texture. Hostile dust puffs are not OK.
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Plague Seeker");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
        }

        public override void AI()
        {
            if (Time > 4f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust plague = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 107, 0f, 0f, 100, default, 0.75f);
                    plague.noGravity = true;
                    plague.velocity = Vector2.Zero;
                }
            }

            // If not close to death, home in on the closest player.
            if (Time > 55f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (!Projectile.WithinRange(target.Center, 50f))
                    Projectile.velocity = (Projectile.velocity * 69f + Projectile.SafeDirectionTo(target.Center) * 16f) / 70f;
            }
            Time++;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 0.8f;
    }
}
