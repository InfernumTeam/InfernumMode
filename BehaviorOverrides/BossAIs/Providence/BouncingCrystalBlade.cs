using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class BouncingCrystalBlade : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Crystal Blade");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 720;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * 0.5f;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust crystal = Dust.NewDustPerfect(Projectile.Center, 68);
                crystal.velocity = Main.rand.NextVector2Circular(6f, 6f);
                crystal.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float oldScale = Projectile.scale;
            Projectile.scale *= 1.2f;
            lightColor = Color.Lerp(lightColor, Main.hslToRgb(Projectile.identity / 7f % 1f, 1f, 0.5f), 0.9f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.scale = oldScale;

            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;
            return false;
        }

        public override bool? CanDamage() => Projectile.alpha < 20 ? null : false;
    }
}
