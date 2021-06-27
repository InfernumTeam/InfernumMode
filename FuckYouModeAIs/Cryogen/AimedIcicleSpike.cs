using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Cryogen
{
    public class AimedIcicleSpike : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float AimAheadFactor => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Icicle Spike");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true) * Utils.InverseLerp(0f, 12f, projectile.timeLeft, true);

            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time < 60f)
            {
                float spinSlowdown = Utils.InverseLerp(56f, 40f, Time, true);
                projectile.velocity *= 0.95f;
                projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * spinSlowdown * 0.3f;
                if (spinSlowdown < 1f)
                {
                    Vector2 aimAhead = closestPlayer.velocity * AimAheadFactor;
                    projectile.rotation = projectile.rotation.AngleLerp(projectile.AngleTo(closestPlayer.Center + aimAhead) - MathHelper.PiOver2, (1f - spinSlowdown) * 0.6f);
                }
            }

            if (Time == 60f)
                projectile.velocity = projectile.SafeDirectionTo(closestPlayer.Center + closestPlayer.velocity * AimAheadFactor) * 11f;
            if (Time > 60f && projectile.velocity.Length() < 25f)
                projectile.velocity *= 1.014f;

            Lighting.AddLight(projectile.Center, Vector3.One * projectile.Opacity * 0.4f);
            Time++;
        }

		public override bool CanDamage() => projectile.alpha < 20;
    }
}
