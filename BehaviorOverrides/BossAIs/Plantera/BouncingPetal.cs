using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class BouncingPetal : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Petal");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 480;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(0f, 15f, projectile.timeLeft, true);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Bounce after hitting a tile.
            projectile.velocity = -oldVelocity * 0.35f;
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
    }
}
