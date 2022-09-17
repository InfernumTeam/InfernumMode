using CalamityMod;
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
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public virtual float MinScale { get; } = 1.2f;
        public virtual float MaxScale { get; } = 5f;
        public virtual Texture2D ExplosionNoiseTexture => ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
        public abstract int Lifetime { get; }
        public abstract float MaxRadius { get; }
        public abstract float RadiusExpandRateInterpolant { get; }
        public abstract float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer);
        public abstract Color DetermineExplosionColor(float lifetimeCompletionRatio);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Explosion");

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // Do screen shake effects.
            float distanceFromPlayer = Projectile.Distance(Main.LocalPlayer.Center);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = DetermineScreenShakePower(1f - Projectile.timeLeft / (float)Lifetime, distanceFromPlayer);

            // Cause the wave to expand outward, along with its hitbox.
            Radius = MathHelper.Lerp(Radius, MaxRadius, RadiusExpandRateInterpolant);
            Projectile.scale = MathHelper.Lerp(MinScale, MaxScale, Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));
            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            Vector2 scale = new(1.5f, 1f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f;
            DrawData explosionDrawData = new(
                ExplosionNoiseTexture,
                drawPosition,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f - (float)Math.Sqrt(1f - Projectile.timeLeft / (float)Lifetime))) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None,
                0);

            GameShaders.Misc["ForceField"].UseColor(DetermineExplosionColor(1f - Projectile.timeLeft / (float)Lifetime));
            GameShaders.Misc["ForceField"].Apply(explosionDrawData);
            explosionDrawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
