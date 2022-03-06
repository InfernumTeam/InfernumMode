using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.MaxUpdates = 2;
            projectile.alpha = 255;
            projectile.timeLeft = 180;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(180f, 170f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 16f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            float noise = CalamityUtils.PerlinNoise2D(projectile.Center.X * 0.05f, projectile.Center.Y * 0.05f, 4, projectile.identity);
            Vector2 acceleration = (noise * MathHelper.Pi * 3f).ToRotationVector2() * 0.23f;
            projectile.velocity = (projectile.velocity + acceleration).ClampMagnitude(0f, 6f);
        }

        public static float PrimitiveWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(0f, 1f, Utils.InverseLerp(0f, 0.1f, completionRatio, true)) * 2f;
        }

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Utils.InverseLerp(1f, 0.47f, completionRatio, true) * projectile.Opacity;
            Color c = Color.Lerp(Color.Cyan, Color.White, projectile.identity / 8f % 0.6f) * opacity * 1.5f;
            c.A = 0;
            return c;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (SparkDrawer is null)
                SparkDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction);

            SparkDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 16);
            return false;
        }
    }
}
