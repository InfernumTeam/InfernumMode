using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class GoreSpike : ModProjectile
    {
        public Player Target => Main.player[Projectile.owner];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spike");

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center) * 8f, 0.02f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f;
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Lighting.AddLight(Projectile.Center, Color.PaleVioletRed.ToVector3() * 0.5f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        // Use a rotating hitbox on this spike. Not doing do can result in oddities unless the hitbox is abnormally small to compensate.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size(), (Projectile.rotation - PiOver2).ToRotationVector2()))
                return null;
            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust boneDust = Dust.NewDustPerfect(Projectile.Center, 26);
                boneDust.velocity = Main.rand.NextVector2Circular(3f, 3f);
                boneDust.scale = Main.rand.NextFloat(1f, 1.5f);
            }
        }
    }
}
