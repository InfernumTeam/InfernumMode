using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
	public class NecroplasmicRoar : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.friendly = false;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(300f, 275f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            projectile.scale = projectile.Opacity * 2f;
            projectile.velocity *= 1.015f;
            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 scale = new Vector2(135f / texture.Height, 1f) * projectile.scale;
            GameShaders.Misc["Infernum:NecroplasmicRoar"].UseOpacity(projectile.Opacity);

            Color pulseColor = Color.Lerp(Color.Cyan, Color.Magenta, 0.2f);
            pulseColor = Color.Lerp(pulseColor, Color.Red, projectile.identity % 5f / 5f * 0.25f + 0.25f);
            GameShaders.Misc["Infernum:NecroplasmicRoar"].UseColor(pulseColor);
            GameShaders.Misc["Infernum:NecroplasmicRoar"].Apply();

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, null, Color.Cyan, projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            spriteBatch.ExitShaderRegion();
            return false;
        }

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 offset = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * projectile.scale * 60f;
            Vector2 start = projectile.Center - offset;
            Vector2 end = projectile.Center + offset;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, projectile.scale * 105f, ref _))
                return true;
            return false;
		}

		public override bool CanDamage() => projectile.Opacity >= 1f;
    }
}
