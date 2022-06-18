using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class DarkMagicCircle : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public ref float Time => ref projectile.ai[0];
        public const int TotalMagicPiecesInCircle = 6;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 36;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 270;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 6f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 6f, Time, true);
            projectile.rotation += (projectile.identity % 2 == 1).ToDirectionInt() * 0.056f;
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < TotalMagicPiecesInCircle; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / TotalMagicPiecesInCircle + projectile.rotation).ToRotationVector2() * projectile.Opacity * projectile.width;
                if (CalamityUtils.CircularHitboxCollision(projectile.Center + drawOffset, 8f, targetHitbox))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < TotalMagicPiecesInCircle; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / TotalMagicPiecesInCircle + projectile.rotation).ToRotationVector2() * projectile.Opacity * projectile.width;

                for (int j = 0; j < projectile.oldPos.Length; j++)
                {
                    float orbRotation = projectile.oldRot[j] * 0.1f;
                    float scaleFactor = 1f - j / (float)(projectile.oldPos.Length - 1f);
                    Vector2 drawPosition = Vector2.Lerp(projectile.oldPos[j], projectile.oldPos[0], 0.4f) + origin - Main.screenPosition;
                    Color color = projectile.GetAlpha(new Color(1f, 1f, 1f, 0.5f)) * scaleFactor;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, orbRotation, origin, projectile.scale, 0, 0f);
                }
            }
            return false;
        }
    }
}
