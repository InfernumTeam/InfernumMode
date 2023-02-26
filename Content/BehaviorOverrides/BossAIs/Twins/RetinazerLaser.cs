using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class RetinazerLaser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Death Laser");
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 600;
            Projectile.scale = 0.75f;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
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

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.scale * -30f;
            Projectile.Center += drawOffset;
            Utilities.DrawProjectileWithBackglowTemp(Projectile, Color.OrangeRed with { A = 0 }, lightColor, 2f);
            Projectile.Center -= drawOffset;
            return false;
        }
    }
}
