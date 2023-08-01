using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Betsy
{
    public class MeteorCinder : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Cinder");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Time > 4f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 100, default, 0.75f);
                    fire.noGravity = true;
                    fire.velocity = Vector2.Zero;
                    fire.scale *= 1.9f;
                }
            }

            // If this projectile is not close to death, home in.
            if (Time > 35f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (!Projectile.WithinRange(target.Center, 50f))
                    Projectile.velocity = (Projectile.velocity * 59f + Projectile.SafeDirectionTo(target.Center) * 19f) / 60f;
            }
            Time++;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 0.8f;
    }
}
