using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class RisingFireball : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fireball");

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.84f);

            Projectile.Opacity = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 300f) * 10f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (Projectile.frameCounter++ % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Projectile.velocity.X *= 0.996f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y - 0.37f, -27f, 17f);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity > 0.75f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, 6);
                dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
    }
}
