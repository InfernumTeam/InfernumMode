using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerSpike : ModProjectile
    {
        public const float FlyingGravity = 1.4f;
        public const float FallingGravity = 0.35f;
        public static readonly float AverageGravity = (float)Math.Sqrt((FlyingGravity * FlyingGravity + FallingGravity * FallingGravity) * 0.5f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spike");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.tileCollide = Projectile.velocity.Y > 0f;

            if (Projectile.velocity.Y < 7f)
                Projectile.velocity.Y += Projectile.velocity.Y > 0f ? FallingGravity : FlyingGravity;
        }
    }
}
