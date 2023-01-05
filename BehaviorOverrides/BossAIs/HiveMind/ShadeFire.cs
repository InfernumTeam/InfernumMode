using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class ShadeFire : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.15f, 0f, Projectile.Opacity * 0.2f);
            if (Projectile.timeLeft > 80)
                Projectile.timeLeft = 80;

            // Start with a random color between green and a muted purple. The variance from this leads to a pseudo-gradient look.
            float lifetimeInterpolant = 1f - Projectile.timeLeft / 80f;
            float particleScale = MathHelper.Lerp(0.03f, 1.2f, (float)Math.Pow(lifetimeInterpolant, 0.53));
            float opacity = Utils.GetLerpValue(0.96f, 0.7f, lifetimeInterpolant, true) * 0.84f;
            float fadeToBlack = Utils.GetLerpValue(5f, 32f, Projectile.timeLeft, true);
            Color fireColor = Color.Lerp(Color.MediumPurple, Color.ForestGreen, Main.rand.NextFloat(0.2f, 0.67f));

            // Have the fire color dissipate into smoke as it reaches death.
            fireColor = Color.Lerp(fireColor, Color.MediumPurple, fadeToBlack);

            // Use a lime flame at the start of the flame's life, indicating extraordinary quantities of heat.
            fireColor = Color.Lerp(fireColor, Color.LimeGreen, Utils.GetLerpValue(0.29f, 0f, lifetimeInterpolant, true) * 0.85f);

            fireColor = Color.Lerp(fireColor, Color.Green, 0.18f);

            // Emit light.
            Lighting.AddLight(Projectile.Center, fireColor.ToVector3() * opacity);

            if (Main.rand.NextBool(2))
            {
                var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.4f, 0.4f), fireColor, 30, particleScale, opacity, 0.05f, true, 0f, true);
                GeneralParticleHandler.SpawnParticle(particle);
            }
        }
    }
}
