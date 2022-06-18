using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class AuroraSpirit : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aurora Spirit");
            Main.projFrames[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 28;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 300;
            projectile.Opacity = 0f;
            projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
        }

        public override void AI()
        {
            // Handle frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.Pi;
            projectile.spriteDirection = (Math.Cos(projectile.rotation) < 0f).ToDirectionInt();
            if (projectile.spriteDirection == 1)
                projectile.rotation -= MathHelper.Pi;

            if (Time == 0f)
            {
                Time = Main.rand.NextFloat(1f, 45f);
                projectile.netUpdate = true;
            }

            // Periodically descend via a squashed, downward-only sine.
            // After enough time has passed this stops in favor of horizontal acceleration.
            if (Time < 110f)
                projectile.velocity.Y = (float)Math.Pow(Math.Sin(Time / 29f), 10D) * 14.5f;
            else
                projectile.velocity.Y *= 0.93f;

            // Accelerate after enough time has passed.
            if (Time > 60f && Math.Abs(projectile.velocity.X) < 19.5f)
                projectile.velocity.X *= 1.0065f;

            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.02f, 0f, 1f);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color endColor = Color.Lerp(lightColor * projectile.Opacity, Color.White, 0.55f);
            return Color.Lerp(new Color(128, 88, 160, 0) * 0.45f, endColor, projectile.Opacity);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }
    }
}
