using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class ElectricSparkParticle : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spark");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 16;
        }

        public override void SetDefaults()
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 180;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(180f, 170f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 16f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Vector2 acceleration = (CalamityUtils.PerlinNoise2D(projectile.Center.X * 0.015f, projectile.Center.Y * 0.016f, 3, projectile.identity) * MathHelper.Pi).ToRotationVector2() * 1.1f;
            projectile.velocity = (projectile.velocity + acceleration).ClampMagnitude(0f, 16f);
        }

        public static float PrimitiveWidthFunction(float completionRatio) => (1f - completionRatio) * 5f;

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Utils.InverseLerp(1f, 0.67f, completionRatio, true) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
