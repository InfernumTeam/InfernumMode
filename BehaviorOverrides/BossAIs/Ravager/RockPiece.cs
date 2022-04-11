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
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.tileCollide = Projectile.velocity.Y > 0f;

            if (Projectile.velocity.Y < 24f)
                Projectile.velocity.Y += 0.3f;
        }
    }
}
