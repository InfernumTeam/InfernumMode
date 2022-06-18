using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedirectingHellfireSCal : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Hellfire");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            CreateVisuals();
            PerformMovement();
            Time++;
        }

        public void CreateVisuals()
        {
            if (Main.dedServ)
                return;

            // Emit idle dust.
            for (int i = 0; i < 2; i++)
            {
                Dust hellfire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(7f, 7f), 267);
                hellfire.velocity = Main.rand.NextVector2Circular(2f, 2f) - projectile.velocity.SafeNormalize(Vector2.Zero) * 1.6f;
                hellfire.scale = Main.rand.NextFloat(0.7f, 0.8f);
                hellfire.color = Color.Lerp(Color.OrangeRed, Color.Red, Main.rand.NextFloat());
                hellfire.noGravity = true;
            }

            // Emit crimson light.
            Lighting.AddLight(projectile.Center, 0.25f, 0f, 0f);

            // Spin depending on direction.
            projectile.rotation += Math.Sign(projectile.velocity.X) * -0.15f;
        }


        public void PerformMovement()
        {
            int initialAccelerationTime = 30;
            int redirectTime = 70;
            float maxRedirectSpeed = 16f;
            float maxAccelerateSpeed = 42f;
            float currentSpeed = projectile.velocity.Length();

            // Accelerate a little bit prior to redirecting.
            if (Time < initialAccelerationTime && currentSpeed < maxRedirectSpeed)
                projectile.velocity *= 1.012f;

            // Try to home in on the target after redirecting. Since this is supposed to be a redirect and not a proper homing attack
            // homing does not happen when close to the target.
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time >= initialAccelerationTime && Time < initialAccelerationTime + redirectTime && !projectile.WithinRange(target.Center, 380f))
            {
                projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.SafeDirectionTo(target.Center) * currentSpeed, 0.09f);
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * currentSpeed;
            }

            // After redirecting, accelerate a bit.
            if (Time >= initialAccelerationTime + redirectTime && currentSpeed < maxAccelerateSpeed)
                projectile.velocity *= 1.032f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Color.Lerp(lightColor, Color.White, 0.5f);
            lightColor.A = 128;
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }
    }
}
