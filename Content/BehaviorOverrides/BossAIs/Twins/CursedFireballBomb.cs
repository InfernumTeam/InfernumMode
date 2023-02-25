using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class CursedFireballBomb : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Fireball Bomb");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }

        public override void AI()
        {
            // Create a burst of dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust cursedFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                    cursedFire.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    cursedFire.noGravity = true;
                    cursedFire.velocity *= 3f;

                    cursedFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                    cursedFire.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    cursedFire.velocity *= 2f;

                    cursedFire.noGravity = true;
                    cursedFire.fadeIn = 1f;
                    cursedFire.color = Color.Green * 0.5f;
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust cursedFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                    cursedFire.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                    cursedFire.noGravity = true;
                    cursedFire.velocity *= 0.5f;
                }

                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.04f, 0f, 0.8f);

            Projectile.velocity *= 1.018f;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Collide with tiles after alive for long enough.
            Projectile.tileCollide = Time >= 20f;
            Time++;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.8f);
            lightColor.A = 0;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 25; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                int randomDustType = Main.rand.NextBool() ? 107 : 110;
                Dust cursedFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                cursedFire.noGravity = true;
                cursedFire.velocity *= 0.67f;
            }
        }

        public override bool? CanDamage() => false;
    }
}
