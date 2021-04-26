using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Sentinels
{
    public class CeaselessEnergy : ModProjectile
    {
        Vector2 origin = default;
        float orbitalRadius = 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy");
        }

        public override void SetDefaults()
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.alpha = 255;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            const float maxVelocityMult = 13.5f;
            if (origin == default)
                origin = projectile.Center;
            projectile.ai[0] += 0.15f * (projectile.ai[1] > 0).ToDirectionInt();
            projectile.Center = origin + projectile.ai[0].ToRotationVector2() * orbitalRadius;
            if (orbitalRadius < 60f)
                orbitalRadius += 1f;
            origin += projectile.velocity;
            if (projectile.timeLeft < 280)
            {
                if (projectile.timeLeft > 140f)
                {
                    float timeLeftRatio = (projectile.timeLeft - 140f) / 140f;
                    projectile.velocity = Vector2.Normalize(projectile.velocity) * (maxVelocityMult - (timeLeftRatio * maxVelocityMult));
                }
                else
                {
                    float timeLeftRatio = projectile.timeLeft / 140f;
                    projectile.velocity = Vector2.Normalize(projectile.velocity) * (timeLeftRatio * maxVelocityMult);
                }
                projectile.velocity = projectile.velocity.RotatedBy(0.2f * (projectile.ai[1] > 0).ToDirectionInt());
            }
            Dust dust = Dust.NewDustPerfect(projectile.Center, 16, Vector2.Zero, 0, new Color(80, 31, 155));
            dust.noGravity = true;
        }
    }
}