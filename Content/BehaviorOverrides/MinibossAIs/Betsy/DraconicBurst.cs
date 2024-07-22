using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Betsy
{
    public class DraconicBurst : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Draconic Burst");

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 0.4f;
    }
}
