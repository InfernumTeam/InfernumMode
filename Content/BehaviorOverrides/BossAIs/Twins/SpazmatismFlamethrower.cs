using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class SpazmatismFlamethrower : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public const int Lifetime = 150;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flamethrower");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -3;
            Projectile.MaxUpdates = 10;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.3f, Projectile.Opacity * 0.65f, Projectile.Opacity * 0.03f);

            // Emit fire particles.
            float lifetimeInterpolant = Timer / Lifetime;
            float particleScale = MathHelper.Lerp(0.03f, 1.67f, MathF.Pow(lifetimeInterpolant, 0.64f));
            float opacity = Utils.GetLerpValue(0.96f, 0.7f, lifetimeInterpolant, true);
            float fadeToBlack = Utils.GetLerpValue(0.5f, 0.84f, lifetimeInterpolant, true);

            // Start with a random color between green and red. The variance from this leads to a pseudo-gradient look.
            Color fireColor = Color.Lerp(Color.Lime, Color.Red, Main.rand.NextFloat(0.2f, 0.8f));

            // Have the fire color dissipate into smoke as it reaches death.
            fireColor = Color.Lerp(fireColor, Color.DarkGray, fadeToBlack);

            // Use a blue flame at the start of the flame's life, indicating cursed flames.
            fireColor = Color.Lerp(fireColor, Color.ForestGreen, Utils.GetLerpValue(0.5f, 0.2f, lifetimeInterpolant, true));

            // Emit light.
            Lighting.AddLight(Projectile.Center, fireColor.ToVector3() * opacity);

            if (Projectile.timeLeft % 5 == 0)
            {
                var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(2f, 2f), fireColor, 16, particleScale, opacity, 0.05f, Main.rand.NextFloat() > Math.Pow(fadeToBlack, 0.2), 0f, true);
                GeneralParticleHandler.SpawnParticle(particle);
            }

            // Adjust the hitbox.
            Projectile.Size = Vector2.One * particleScale * 40f;

            Timer++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center - Projectile.velocity * Projectile.MaxUpdates * 2f, targetHitbox, Projectile.Size.Length() * 0.707f);
        }
    }
}
