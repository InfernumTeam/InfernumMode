using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class PlagueMissile2 : ModProjectile
    {
        public ref float Time => ref projectile.ai[1];
        public Player Target => Main.player[(int)projectile.ai[0]];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Missile");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (projectile.Hitbox.Intersects(Target.Hitbox))
                projectile.Kill();

            projectile.tileCollide = projectile.Center.Y > Target.Center.Y;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item14, projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(35f, 35f), 89);
                dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                dust.scale = Main.rand.NextFloat(1.1f, 1.35f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D glowmask = ModContent.GetTexture(Texture.Replace("2", string.Empty) + "Glowmask");
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            // Draw afterimages.
            for (int i = 0; i < 6; i++)
            {
                Vector2 afterimageOffset = projectile.velocity.SafeNormalize(Vector2.Zero) * i * -16f;
                Color afterimageColor = Color.Lime * (1f - i / 6f) * 0.7f;
                afterimageColor.A = 0;
                spriteBatch.Draw(texture, drawPosition + afterimageOffset, null, projectile.GetAlpha(afterimageColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmask, drawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
