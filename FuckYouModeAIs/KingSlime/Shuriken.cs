using CalamityMod.Projectiles;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.KingSlime
{
	public class Shuriken : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shuriken");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
		}

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.4f;
            projectile.tileCollide = projectile.timeLeft < 90;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft) => Collision.HitTiles(projectile.position, projectile.velocity, 24, 24);
    }
}
