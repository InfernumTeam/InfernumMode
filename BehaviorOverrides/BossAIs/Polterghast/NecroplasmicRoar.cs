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
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(300f, 275f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity * 2f;
            Projectile.velocity *= 1.015f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 scale = new Vector2(135f / texture.Height, 1f) * Projectile.scale;
            GameShaders.Misc["Infernum:NecroplasmicRoar"].UseOpacity(Projectile.Opacity);

            Color pulseColor = Color.Lerp(Color.Cyan, Color.Magenta, 0.2f);
            pulseColor = Color.Lerp(pulseColor, Color.Red, Projectile.identity % 5f / 5f * 0.25f + 0.25f);
            GameShaders.Misc["Infernum:NecroplasmicRoar"].UseColor(pulseColor);
            GameShaders.Misc["Infernum:NecroplasmicRoar"].Apply();

            spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.Cyan, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * Projectile.scale * 60f;
            Vector2 start = Projectile.Center - offset;
            Vector2 end = Projectile.Center + offset;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * 105f, ref _))
                return true;
            return false;
        }

        public override bool CanDamage() => Projectile.Opacity >= 1f;
    }
}
