using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class DustDevil : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float Variant => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dust Devil");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 38;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Play a wind sound.
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.DD2_BookStaffCast, projectile.Center);
                projectile.localAI[0] = 1f;
            }

            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.075f, 0f, 1f);

            // Bob up and down vertically.
            float idealVerticalVelocity = (float)Math.Sin(Time / 75f + projectile.identity) * 6f;
            projectile.velocity.Y = MathHelper.Lerp(projectile.velocity.Y, idealVerticalVelocity, 0.04f);

            // Speed up horizontally.
            if (Math.Abs(projectile.velocity.X) < 11f)
                projectile.velocity.X *= 1.01f;

            // Determine frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(5, Main.projFrames[projectile.type], (int)Variant, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, frame, Color.White * projectile.Opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
