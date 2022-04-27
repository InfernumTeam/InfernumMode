using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
	public class FanTelegraphLine : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
            Main.projFrames[Projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 25;
        }

        public override void AI()
        {
            Projectile.Opacity = CalamityUtils.Convert01To010(Projectile.timeLeft / 25f) * 0.7f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override bool? CanDamage() => false ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2700f;
            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, Color.Red * Projectile.Opacity, Projectile.Opacity * 6f);
            return false;
        }
    }
}
