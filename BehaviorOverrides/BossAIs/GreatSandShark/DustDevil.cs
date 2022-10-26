using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class DustDevil : ModProjectile
    {
        public bool FastAcceleration => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dust Devil");
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];
            
            Projectile.rotation = Projectile.velocity.X * 0.01f;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.125f, 0f, 1f);

            float wrappedAttackTimer = Projectile.ai[1] % 120f;
            if (wrappedAttackTimer < 45f)
                Projectile.velocity *= FastAcceleration ? 1.033f : 1.024f;
            if (wrappedAttackTimer >= 90f)
                Projectile.velocity *= 0.96f;

            if (Projectile.velocity.Length() > 20f)
                Projectile.velocity *= 0.94f;
            Projectile.ai[1]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3f;
                Color backlightColor = Projectile.GetAlpha(new Color(1f, 1f, 1f, 0f)) * Lighting.Brightness((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f)) * 0.65f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, backlightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
