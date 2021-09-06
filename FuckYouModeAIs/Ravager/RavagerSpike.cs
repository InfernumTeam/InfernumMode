using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class RavagerSpike : ModProjectile
    {
        public const float FlyingGravity = 1.4f;
        public const float FallingGravity = 0.35f;
        public static readonly float AverageGravity = (float)Math.Sqrt((FlyingGravity * FlyingGravity + FallingGravity * FallingGravity) * 0.5f); 
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spike");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 6;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.tileCollide = projectile.velocity.Y > 0f;

            if (projectile.velocity.Y < 14f)
                projectile.velocity.Y += projectile.velocity.Y > 0f ? FallingGravity : FlyingGravity;
        }
    }
}
