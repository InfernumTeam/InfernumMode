using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class LostSoulProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Burning Soul");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += Pi;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            if (Projectile.velocity.Length() < 33f)
                Projectile.velocity *= 1.03f;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw a brief telegraph line.
            float telegraphInterpolant = Utils.GetLerpValue(300f, 265f, Projectile.timeLeft, true);
            if (telegraphInterpolant < 1f)
            {
                Color telegraphColor = Color.Red * Sqrt(telegraphInterpolant);
                float telegraphWidth = LumUtils.Convert01To010(telegraphInterpolant) * 3f;
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3600f, telegraphColor, telegraphWidth);
            }

            float oldScale = Projectile.scale;
            Projectile.scale *= 1.2f;
            lightColor = Color.Lerp(lightColor, Color.Red, 0.9f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.scale = oldScale;

            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);

            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;
    }
}
