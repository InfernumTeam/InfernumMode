using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RockPiece : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Rock");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.tileCollide = projectile.velocity.Y > 0f;

            if (projectile.velocity.Y < 24f)
                projectile.velocity.Y += 0.3f;
        }
    }
}
