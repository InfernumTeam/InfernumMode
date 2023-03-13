using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class ReaperSharkIceBreath : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public const int Lifetime = 132;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ice Breath");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 7;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.3f, Projectile.Opacity * 0.3f, Projectile.Opacity * 0.03f);

            // Emit fire particles.
            float lifetimeInterpolant = Timer / Lifetime;
            float particleScale = MathHelper.Lerp(0.03f, 1.2f, (float)Math.Pow(lifetimeInterpolant, 0.53));
            float opacity = Utils.GetLerpValue(0.96f, 0.7f, lifetimeInterpolant, true);
            float fadeToBlack = Utils.GetLerpValue(0.5f, 0.84f, lifetimeInterpolant, true);

            // Use a random color between blue and cyan. The variance from this leads to a pseudo-gradient look.
            Color iceColor = Color.Lerp(Color.Blue, Color.DeepSkyBlue, Main.rand.NextFloat(0.25f, 0.8f));

            // Emit light.
            Lighting.AddLight(Projectile.Center, iceColor.ToVector3() * opacity);

            var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.4f, 0.4f), iceColor, 30, particleScale, opacity, 0.05f, Main.rand.NextFloat() > Math.Pow(fadeToBlack, 0.2), 0f, true);
            GeneralParticleHandler.SpawnParticle(particle);

            // Randomly emit ice particles.
            if (Main.rand.NextBool(6) && lifetimeInterpolant < 0.6f)
            {
                Dust ice = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), 80);
                ice.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.6f) * 4f + Main.rand.NextVector2Circular(2f, 2f);
                ice.noGravity = true;
            }

            Timer++;
        }
    }
}
