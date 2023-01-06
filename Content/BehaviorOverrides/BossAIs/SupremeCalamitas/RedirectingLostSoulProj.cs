using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedirectingLostSoulProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Burning Soul");
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
            Projectile.timeLeft = 230;
        }

        public override void AI()
        {
            bool canRotate = false;
            bool canMoveTowardsTarget = false;
            float stopMovingTime = 140f;
            float dissipateTime = 30f;

            if (Projectile.timeLeft > stopMovingTime)
            {
                canRotate = true;
            }
            else if (Projectile.timeLeft > dissipateTime)
            {
                canMoveTowardsTarget = true;
            }
            if (canRotate)
            {
                float offsetInterpolant = (float)Math.Cos(Projectile.whoAmI % 6f / 6f + Projectile.position.X / 380f + Projectile.position.Y / 160f);
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi * offsetInterpolant / 120f) * 0.98f;
            }

            if (canMoveTowardsTarget)
            {
                int targetIndex = (int)Projectile.ai[0];
                Vector2 idealVelocity = Projectile.velocity;
                if (Projectile.hostile && Main.player.IndexInRange(targetIndex))
                {
                    idealVelocity = Projectile.SafeDirectionTo(Main.player[targetIndex].Center) * 41f;
                    if (Projectile.localAI[0] > 0f)
                        idealVelocity *= Projectile.localAI[0];
                }

                float amount = MathHelper.Lerp(0.056f, 0.12f, Utils.GetLerpValue(stopMovingTime, 30f, Projectile.timeLeft, true));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, idealVelocity, amount);
            }
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw a brief telegraph line.
            float telegraphInterpolant = Utils.GetLerpValue(300f, 275f, Projectile.timeLeft, true);
            if (telegraphInterpolant < 1f)
            {
                Color telegraphColor = Color.Red * (float)Math.Sqrt(telegraphInterpolant);
                float telegraphWidth = CalamityUtils.Convert01To010(telegraphInterpolant) * 3f;
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
