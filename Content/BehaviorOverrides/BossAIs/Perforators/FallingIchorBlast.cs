using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class FallingIchorBlast : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ichor Blob");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 56;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.position.Y > Projectile.ai[1] - 32f)
                Projectile.tileCollide = true;

            // Add yellow light based on alpha
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.2f / 255f, (255 - Projectile.alpha) * 0.16f / 255f, (255 - Projectile.alpha) * 0.04f / 255f);

            // Rotate.
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            // Reduce x velocity every frame.
            Projectile.velocity.X *= 0.9925f;

            // Die if water or lava is hit.
            if (Projectile.wet || Projectile.lavaWet)
                Projectile.Kill();
            else
            {
                // Fall.
                Projectile.velocity.Y += 0.26f;
                if (Projectile.velocity.Y > 9f)
                    Projectile.velocity.Y = 9f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = true;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => LumUtils.CircularHitboxCollision(Projectile.Center, 16f, targetHitbox);

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, Projectile.alpha);

        public override bool PreDraw(ref Color lightColor)
        {
            LumUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
