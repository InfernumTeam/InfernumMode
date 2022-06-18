using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class RedirectingPlagueMissile : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Missile");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 24;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 12f, Time, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (projectile.Hitbox.Intersects(Target.Hitbox))
                projectile.Kill();

            if (Time < 30f)
                projectile.velocity *= 1.01f;
            if (Time >= 30f)
            {
                float newSpeed = MathHelper.Clamp(projectile.velocity.Length() * 1.003f, 11f, 18f);
                projectile.velocity = (projectile.velocity * 29f + projectile.SafeDirectionTo(Target.Center) * newSpeed) / 30f;
            }

            projectile.tileCollide = Time > 105f;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item14, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<LargePlagueExplosion>(), 160, 0f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlagueMissileGlowmask");
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            // Draw afterimages.
            for (int i = 0; i < 3; i++)
            {
                Vector2 afterimageOffset = projectile.velocity.SafeNormalize(Vector2.Zero) * i * -16f;
                Color afterimageColor = Color.Lime * (1f - i / 3f) * 0.7f;
                afterimageColor.A = 0;
                spriteBatch.Draw(texture, drawPosition + afterimageOffset, null, projectile.GetAlpha(afterimageColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmask, drawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * projectile.Opacity;

        public override bool CanDamage() => projectile.Opacity >= 0.8f;
    }
}
