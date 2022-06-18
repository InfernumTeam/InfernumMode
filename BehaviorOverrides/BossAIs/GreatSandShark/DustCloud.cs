using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class DustCloud : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float Variant => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dust Cloud");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 18;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Fade in.
            projectile.Opacity = Utils.InverseLerp(600f, 530f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 30f, projectile.timeLeft, true);

            // Decelerate.
            projectile.velocity = projectile.velocity.MoveTowards(Vector2.Zero, 0.02f) * 0.985f;

            // Determine frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 7 % Main.projFrames[projectile.type];

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(5, Main.projFrames[projectile.type], (int)Variant, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(1f, 1f, 1f, 0f) * projectile.Opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, frame, Color.White * projectile.Opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => projectile.timeLeft < 505;
    }
}
