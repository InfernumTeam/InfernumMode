using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class DustDevil : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float Variant => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dust Devil");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Play a wind sound.
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_BookStaffCast, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.075f, 0f, 1f);

            // Bob up and down vertically.
            float idealVerticalVelocity = (float)Math.Sin(Time / 75f + Projectile.identity) * 6f;
            Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, idealVerticalVelocity, 0.04f);

            // Speed up horizontally.
            if (Math.Abs(Projectile.velocity.X) < 11f)
                Projectile.velocity.X *= 1.01f;

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(5, Main.projFrames[Projectile.type], (int)Variant, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
