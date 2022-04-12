using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerPierceLaser : ModProjectile
    {
        public ref float Variant => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laserbeam");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 100;
            Projectile.timeLeft = 400;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Create an initial burst of dust on the first frame.
            if (Projectile.ai[1] == 0f)
            {
                float minDustSpeed = 2.4f;
                float maxDustSpeed = 4.5f;

                for (int i = 0; i < 20; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 laserVelocity = (Projectile.velocity.ToRotation() + Main.rand.NextFloat(-0.1f, 0.1f)).ToRotationVector2() * dustSpeed;

                    Dust laser = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 182, laserVelocity.X, laserVelocity.Y, 200, default, 0.85f);
                    laser.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    laser.noGravity = true;
                    laser.velocity *= 3f;

                    laser = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 182, laserVelocity.X, laserVelocity.Y, 100, default, 0.9f);
                    laser.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat() * Projectile.width / 2f;
                    laser.velocity *= 2f;
                    laser.noGravity = true;
                    laser.fadeIn = 1f;
                    laser.color = Color.Red * 0.6f;
                }

                for (int i = 0; i < 10; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 laserVelocity = (Projectile.velocity.ToRotation() + Main.rand.NextFloat(-0.1f, 0.1f)).ToRotationVector2() * dustSpeed;

                    Dust laser = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 182, laserVelocity.X, laserVelocity.Y, 0, default, 1.2f);
                    laser.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                    laser.noGravity = true;
                    laser.velocity *= 0.5f;
                }

                Variant = Main.rand.Next(3);
                Projectile.netUpdate = true;
                Projectile.ai[1] = 1f;
                return;
            }

            // Otherwise create variable dust.
            Vector2 offsetFactor = new(5f, 10f);
            Vector2 spawnOffset;

            switch ((int)Variant)
            {
                case 0:
                    Dust laser = Dust.NewDustDirect(Projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = Projectile.Center;
                    laser.velocity = Projectile.velocity;
                    break;
                case 1:
                    spawnOffset = Vector2.UnitY * offsetFactor * 0.5f;
                    laser = Dust.NewDustDirect(Projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = Projectile.Center + spawnOffset;
                    laser.velocity = Projectile.velocity;
                    break;
                case 2:
                    spawnOffset = -Vector2.UnitY * offsetFactor * 0.5f;
                    laser = Dust.NewDustDirect(Projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = Projectile.Center + spawnOffset;
                    laser.velocity = Projectile.velocity;
                    break;
                default:
                    break;
            }
        }
    }
}
