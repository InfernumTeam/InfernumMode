using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class FanTelegraphLine : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
            Main.projFrames[projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 25;
        }

        public override void AI()
        {
            projectile.Opacity = CalamityUtils.Convert01To010(projectile.timeLeft / 25f) * 0.7f;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override bool CanDamage() => false;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 end = projectile.Center + projectile.velocity.SafeNormalize(Vector2.UnitY) * 2700f;
            spriteBatch.DrawLineBetter(projectile.Center, end, Color.Red * projectile.Opacity, projectile.Opacity * 6f);
            return false;
        }
    }
}
