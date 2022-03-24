using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class FistBulletTelegraph : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.hostile = false;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 45;
        }

        public override void AI()
        {
            projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / 45f);
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 start = projectile.Center;
            Vector2 end = projectile.Center + projectile.velocity * 7000f;
            spriteBatch.DrawLineBetter(start, end, Color.Orange, projectile.scale * 4f);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
