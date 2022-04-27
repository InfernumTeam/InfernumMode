using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
	public class ElectricSparkParticle : ModProjectile
    {
        public PrimitiveTrailCopy SparkDrawer = null;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spark");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 2;
            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(180f, 170f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            float noise = CalamityUtils.PerlinNoise2D(Projectile.Center.X * 0.05f, Projectile.Center.Y * 0.05f, 4, Projectile.identity);
            Vector2 acceleration = (noise * MathHelper.Pi * 3f).ToRotationVector2() * 0.23f;
            Projectile.velocity = (Projectile.velocity + acceleration).ClampMagnitude(0f, 6f);
        }

        public static float PrimitiveWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, 0.1f, completionRatio, true)) * 2f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(1f, 0.47f, completionRatio, true) * Projectile.Opacity;
            Color c = Color.Lerp(Color.Cyan, Color.White, Projectile.identity / 8f % 0.6f) * opacity * 1.5f;
            c.A = 0;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (SparkDrawer is null)
                SparkDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction);

            SparkDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 16);
            return false;
        }
    }
}
