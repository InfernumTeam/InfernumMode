using CalamityMod;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BaseEntities
{
    public abstract class BaseWaveExplosionProjectile : ModProjectile
    {
        public float Radius
        {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        public virtual float MinScale { get; } = 1.2f;
        public virtual float MaxScale { get; } = 5f;
        public virtual Texture2D ExplosionNoiseTexture => ModContent.GetTexture("Terraria/Misc/Perlin");
        public abstract int Lifetime { get; }
        public abstract float MaxRadius { get; }
        public abstract float RadiusExpandRateInterpolant { get; }
        public abstract float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer);
        public abstract Color DetermineExplosionColor(float lifetimeCompletionRatio);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = Lifetime;
            projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // Do screen shake effects.
            float distanceFromPlayer = projectile.Distance(Main.LocalPlayer.Center);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = DetermineScreenShakePower(1f - projectile.timeLeft / (float)Lifetime, distanceFromPlayer);

            // Cause the wave to expand outward, along with its hitbox.
            Radius = MathHelper.Lerp(Radius, MaxRadius, RadiusExpandRateInterpolant);
            projectile.scale = MathHelper.Lerp(MinScale, MaxScale, Utils.InverseLerp(Lifetime, 0f, projectile.timeLeft, true));
            CalamityGlobalProjectile.ExpandHitboxBy(projectile, (int)(Radius * projectile.scale), (int)(Radius * projectile.scale));
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            Vector2 scale = new Vector2(1.5f, 1f);
            Vector2 drawPosition = projectile.Center - Main.screenPosition + projectile.Size * scale * 0.5f;
            DrawData explosionDrawData = new DrawData(
                ExplosionNoiseTexture,
                drawPosition,
                new Rectangle(0, 0, projectile.width, projectile.height),
                new Color(new Vector4(1f - (float)Math.Sqrt(1f - projectile.timeLeft / (float)Lifetime))) * 0.7f * projectile.Opacity,
                projectile.rotation,
                projectile.Size,
                scale,
                SpriteEffects.None,
                0);

            GameShaders.Misc["ForceField"].UseColor(DetermineExplosionColor(1f - projectile.timeLeft / (float)Lifetime));
            GameShaders.Misc["ForceField"].Apply(explosionDrawData);
            explosionDrawData.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
