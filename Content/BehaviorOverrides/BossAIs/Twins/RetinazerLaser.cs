using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class RetinazerLaser : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Death Laser");
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 600;
            Projectile.scale = 0.75f;
            Projectile.Opacity = 0f;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }
        public override void AI()
        {
            // Create a small puff of laser dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 18; i++)
                {
                    Dust laser = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f) - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Projectile.scale * 20f, 182);
                    laser.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.19f) * Main.rand.NextFloat(4f, 12f);
                    laser.noGravity = true;
                    laser.scale *= 1.25f;
                }
                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (lightColor.A != 255)
                return lightColor * Projectile.Opacity;

            return Color.White * Projectile.Opacity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return LumUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 backOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.scale * -30f;
            Vector2 oldCenter = Projectile.Center;

            float materializeInterpolant = Utils.GetLerpValue(0f, 64f, Time, true);
            if (Projectile.ai[1] == 0f)
                materializeInterpolant = 1f;

            for (int i = 0; i < (Projectile.ai[1] == 0f ? 1 : 6); i++)
            {
                Vector2 drawOffset = (TwoPi * i / 6f).ToRotationVector2() * (1f - materializeInterpolant) * 12f + backOffset;
                lightColor = Color.Lerp(lightColor, Color.Wheat with { A = 0 }, (1f - materializeInterpolant) * 0.6f) * Utils.GetLerpValue(0.1f, 0.5f, materializeInterpolant, true);
                Projectile.Center = oldCenter + drawOffset;
                Utilities.DrawProjectileWithBackglowTemp(Projectile, Color.OrangeRed with { A = 0 } * Pow(materializeInterpolant, 4f), lightColor, 2f);
            }
            Projectile.Center = oldCenter;
            return false;
        }
    }
}
