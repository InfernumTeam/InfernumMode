using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class DarkBoltLarge : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 230;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
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
                float offsetInterpolant = Cos(Projectile.whoAmI % 6f / 6f + Projectile.position.X / 320f + Projectile.position.Y / 160f);
                Projectile.velocity = Projectile.velocity.RotatedBy(Pi * offsetInterpolant / 120f) * 0.98f;
            }

            if (canMoveTowardsTarget)
            {
                int targetIndex = (int)Projectile.ai[0];
                Vector2 idealVelocity = Projectile.velocity;
                if (Projectile.hostile && Main.player.IndexInRange(targetIndex))
                    idealVelocity = Projectile.SafeDirectionTo(Main.player[targetIndex].Center) * 34.5f;

                float amount = Lerp(0.056f, 0.12f, Utils.GetLerpValue(stopMovingTime, 30f, Projectile.timeLeft, true));
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, idealVelocity, amount);
            }
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;



        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, new(0.45f, 1f, 0.64f), 0.55f);
            lightColor.A /= 3;
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            return false;
        }
    }
}
