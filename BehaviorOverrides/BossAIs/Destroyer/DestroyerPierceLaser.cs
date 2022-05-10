using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerPierceLaser : ModProjectile
    {
        public ref float Variant => ref projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laserbeam");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.extraUpdates = 100;
            projectile.timeLeft = 400;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Create an initial burst of dust on the first frame.
            if (projectile.ai[1] == 0f)
            {
                float minDustSpeed = 2.4f;
                float maxDustSpeed = 4.5f;

                for (int i = 0; i < 20; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 laserVelocity = (projectile.velocity.ToRotation() + Main.rand.NextFloat(-0.1f, 0.1f)).ToRotationVector2() * dustSpeed;

                    Dust laser = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 182, laserVelocity.X, laserVelocity.Y, 200, default, 0.85f);
                    laser.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * (float)Main.rand.NextDouble() * projectile.width / 2f;
                    laser.noGravity = true;
                    laser.velocity *= 3f;

                    laser = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 182, laserVelocity.X, laserVelocity.Y, 100, default, 0.9f);
                    laser.position = projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat() * projectile.width / 2f;
                    laser.velocity *= 2f;
                    laser.noGravity = true;
                    laser.fadeIn = 1f;
                    laser.color = Color.Red * 0.6f;
                }

                for (int i = 0; i < 10; i++)
                {
                    float dustSpeed = Main.rand.NextFloat(minDustSpeed, maxDustSpeed);
                    Vector2 laserVelocity = (projectile.velocity.ToRotation() + Main.rand.NextFloat(-0.1f, 0.1f)).ToRotationVector2() * dustSpeed;

                    Dust laser = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 182, laserVelocity.X, laserVelocity.Y, 0, default, 1.2f);
                    laser.position = projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(projectile.velocity.ToRotation()) * projectile.width / 3f;
                    laser.noGravity = true;
                    laser.velocity *= 0.5f;
                }

                Variant = Main.rand.Next(3);
                projectile.netUpdate = true;
                projectile.ai[1] = 1f;
                return;
            }

            // Otherwise create variable dust.
            Vector2 offsetFactor = new Vector2(5f, 10f);
            Vector2 spawnOffset;

            switch ((int)Variant)
            {
                case 0:
                    Dust laser = Dust.NewDustDirect(projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = projectile.Center;
                    laser.velocity = projectile.velocity;
                    break;
                case 1:
                    spawnOffset = Vector2.UnitY * offsetFactor * 0.5f;
                    laser = Dust.NewDustDirect(projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = projectile.Center + spawnOffset;
                    laser.velocity = projectile.velocity;
                    break;
                case 2:
                    spawnOffset = -Vector2.UnitY * offsetFactor * 0.5f;
                    laser = Dust.NewDustDirect(projectile.Center, 0, 0, 182, 0f, 0f, 160, default, 1.15f);
                    laser.noGravity = true;
                    laser.position = projectile.Center + spawnOffset;
                    laser.velocity = projectile.velocity;
                    break;
                default:
                    break;
            }
        }
    }
}
