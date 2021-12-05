using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DesertScourge
{
    public class SandstormBlast : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Blast");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 360;
            projectile.alpha = 255;
        }

        public override void AI()
        {
            projectile.tileCollide = projectile.timeLeft < 240;
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.125f, 0f, 1f);

            projectile.velocity *= 1.00502515f;
            if (Collision.SolidCollision(projectile.position - Vector2.One * 5f, 10, 10))
            {
                projectile.scale *= 0.9f;
                projectile.velocity *= 0.25f;
                if (projectile.scale < 0.5f)
                    projectile.Kill();
            }
            else
                projectile.velocity.Y = (float)Math.Sin(projectile.position.X * MathHelper.TwoPi / 999f) * 2f + 3f;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }
    }
}
