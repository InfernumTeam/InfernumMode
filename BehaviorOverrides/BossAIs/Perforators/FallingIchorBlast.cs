using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class FallingIchorBlast : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ichor Blob");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = 52;
            projectile.height = 56;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (projectile.position.Y > projectile.ai[1] - 32f)
                projectile.tileCollide = true;

            // Add yellow light based on alpha
            Lighting.AddLight(projectile.Center, (255 - projectile.alpha) * 0.2f / 255f, (255 - projectile.alpha) * 0.16f / 255f, (255 - projectile.alpha) * 0.04f / 255f);

            // Rotate.
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Reduce x velocity every frame.
            projectile.velocity.X *= 0.9925f;

            // Die if water or lava is hit.
            if (projectile.wet || projectile.lavaWet)
                projectile.Kill();
            else
            {
                // Fall.
                projectile.velocity.Y += 0.26f;
                if (projectile.velocity.Y > 9f)
                    projectile.velocity.Y = 9f;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = true;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(projectile.Center, 16f, targetHitbox);

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, projectile.alpha);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Ichor, 240);
    }
}
