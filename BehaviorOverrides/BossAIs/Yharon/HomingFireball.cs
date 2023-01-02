using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class HomingFireball : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        private readonly int Lifetime = 180;

        public float LifetimeCompletion => Timer / Lifetime;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Homing Fireball");
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = 1;
        }
        public override void AI()
        {
            // Get a player to home to.
            Player closestTarget = null;
            for (int k = 0; k < 255; k++)
            {
                if (Main.player[k].active && !Main.player[k].dead)
                {
                    Vector2 playerCenter = Main.player[k].Center;
                    float disanceToPlayer = Vector2.Distance(playerCenter, Projectile.Center);
                    if ((disanceToPlayer < 2000) && Collision.CanHit(Projectile.Center, 1, 1, playerCenter, 1, 1))
                    {
                        closestTarget = Main.player[k];
                        break;
                    }
                }
            }

            // If its null, dont do this, but still create the visuals.
            if (closestTarget is not null)
            {
                // Vanilla code.
                float rotation = Projectile.velocity.ToRotation();
                Vector2 directionToPlayer = closestTarget.Center - Projectile.Center;

                float targetRotation = directionToPlayer.ToRotation();
                if (directionToPlayer == Vector2.Zero)
                    targetRotation = rotation;
                float angleRotation = rotation.AngleLerp(targetRotation, 0.015f);

                Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0f).RotatedBy(angleRotation);
            }
            float particleScale = Utils.GetLerpValue(0f, 0.05f, LifetimeCompletion, true);

            for(int j = 0; j < 3; j++)
            {
                Color fireColor = Color.Lerp(Color.Orange, new Color(255, 231, 108), Main.rand.NextFloat(0.2f, 0.3f));
                float angularVelocity = Main.rand.NextFloat(0.035f, 0.08f);
                FireballParticle fire = new(Projectile.Center, Projectile.velocity * 0.8f, fireColor, 10, Main.rand.NextFloat(0.52f, 0.68f) * particleScale, 1f, Main.rand.NextBool().ToDirectionInt() * angularVelocity);
                GeneralParticleHandler.SpawnParticle(fire);
            }
            
            if (LifetimeCompletion is > 0.05f and < 0.9f)
            {
                Lighting.AddLight(Projectile.Center, 1.1f, 0.9f, 0.4f);

                // Create fire and smoke dust effects.
                if (Timer % 12f == 11f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 fireSpawnOffset = Vector2.UnitX * Projectile.width * -0.5f;
                        fireSpawnOffset += -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 12f) * new Vector2(8f, 16f);
                        fireSpawnOffset = fireSpawnOffset.RotatedBy((double)(Projectile.rotation - 1.57079637f));
                        Dust fire = Dust.NewDustDirect(Projectile.Center, 0, 0, 6, 0f, 0f, 160, default, 1f);
                        fire.scale = 1.1f;
                        fire.noGravity = true;
                        fire.position = Projectile.Center + fireSpawnOffset;
                        fire.velocity = Projectile.velocity * 0.1f;
                        fire.velocity = Vector2.Normalize(Projectile.Center - Projectile.velocity * 3f - fire.position) * 1.25f;
                    }
                }
                if (Main.rand.NextBool(4))
                {
                    Vector2 offsetDirection = -Vector2.UnitX.RotatedByRandom(MathHelper.Pi / 12f).RotatedBy(Projectile.velocity.ToRotation());
                    Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, 0f, 0f, 100, default, 1f);
                    smoke.velocity *= 0.1f;
                    smoke.position = Projectile.Center + offsetDirection * Projectile.width / 2f;
                    smoke.fadeIn = 0.9f;
                }
                if (Main.rand.NextBool(32))
                {
                    Vector2 offsetDirection = -Vector2.UnitX.RotatedByRandom(MathHelper.Pi / 8f).RotatedBy(Projectile.velocity.ToRotation());
                    Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, 0f, 0f, 155, default, 0.8f);
                    smoke.velocity *= 0.3f;
                    smoke.position = Projectile.Center + offsetDirection * Projectile.width / 2f;
                    if (Main.rand.NextBool(2))
                        smoke.fadeIn = 1.4f;
                }
                if (Main.rand.NextBool(4))
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offsetDirection = -Vector2.UnitX.RotatedByRandom(MathHelper.PiOver4).RotatedBy((double)Projectile.velocity.ToRotation());
                        Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 0, default, 1.2f);
                        fire.velocity *= 0.3f;
                        fire.noGravity = true;
                        fire.position = Projectile.Center + offsetDirection * Projectile.width / 2f;
                        if (Main.rand.NextBool(2))
                            fire.fadeIn = 1.4f;
                    }
                }
            }
            Timer++;
        }
    }
}
