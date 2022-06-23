using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class DarkMagicCircle : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public ref float Time => ref Projectile.ai[0];
        public const int TotalMagicPiecesInCircle = 6;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Magic");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 270;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 6f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 6f, Time, true);
            Projectile.rotation += (Projectile.identity % 2 == 1).ToDirectionInt() * 0.056f;
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < TotalMagicPiecesInCircle; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / TotalMagicPiecesInCircle + Projectile.rotation).ToRotationVector2() * Projectile.Opacity * Projectile.width;
                if (CalamityUtils.CircularHitboxCollision(Projectile.Center + drawOffset, 8f, targetHitbox))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < TotalMagicPiecesInCircle; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / TotalMagicPiecesInCircle + Projectile.rotation).ToRotationVector2() * Projectile.Opacity * Projectile.width;

                for (int j = 0; j < Projectile.oldPos.Length; j++)
                {
                    float orbRotation = Projectile.oldRot[j] * 0.1f;
                    float scaleFactor = 1f - j / (float)(Projectile.oldPos.Length - 1f);
                    Vector2 drawPosition = Vector2.Lerp(Projectile.oldPos[j], Projectile.oldPos[0], 0.4f) + origin - Main.screenPosition;
                    Color color = Projectile.GetAlpha(new Color(1f, 1f, 1f, 0.5f)) * scaleFactor;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, orbRotation, origin, Projectile.scale, 0, 0f);
                }
            }
            return false;
        }
    }
}
