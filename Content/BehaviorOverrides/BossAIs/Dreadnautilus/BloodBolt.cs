using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class BloodBolt : ModProjectile
    {
        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Blood Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 230;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            if (Projectile.velocity.Length() < 33f)
                Projectile.velocity *= 1.033f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                int x = (int)Projectile.oldPos[i].X;
                int y = (int)Projectile.oldPos[i].Y;
                if (new Rectangle(x, y, 20, 20).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            int dustCount = 20;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust blood = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2);
                blood.fadeIn = 1f;
                blood.noGravity = true;
                blood.alpha = 100;
                blood.color = Color.Lerp(Color.Red, Color.White, Main.rand.NextFloat(0.3f));
                if (i % 4 == 0)
                {
                    blood.velocity = angularOffset.ToRotationVector2() * 3.2f;
                    blood.scale = 2.3f;
                }
                else if (i % 2 == 0)
                {
                    blood.velocity = angularOffset.ToRotationVector2() * 1.8f;
                    blood.scale = 1.9f;
                }
                else
                {
                    blood.velocity = angularOffset.ToRotationVector2();
                    blood.scale = 1.6f;
                }
                angularOffset += TwoPi / dustCount;
                blood.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;
            for (int i = 0; i < Projectile.oldPos.Length; ++i)
            {
                float afterimageRot = Projectile.oldRot[i];
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                Color afterimageColor = Color.Red * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                Main.spriteBatch.Draw(texture, drawPos, null, afterimageColor, afterimageRot, origin, Projectile.scale * 0.5f, 0, 0f);

                if (i > 0)
                {
                    for (float j = 0.2f; j < 0.8f; j += 0.2f)
                    {
                        drawPos = Vector2.Lerp(Projectile.oldPos[i - 1], Projectile.oldPos[i], j) +
                            Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                        Main.spriteBatch.Draw(texture, drawPos, null, afterimageColor, afterimageRot, origin, Projectile.scale * 0.5f, 0, 0f);
                    }
                }
            }

            Color color = Color.Red * 0.5f;
            color.A = 0;

            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale * 0.9f, 0, 0);
            Color bigGleamColor = color;
            Color smallGleamColor = color * 0.5f;
            float opacity = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) *
                Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) *
                (1f + 0.2f * Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * Pi * 6f)) * 0.8f;
            Vector2 bigGleamScale = new Vector2(0.5f, 5f) * opacity;
            Vector2 smallGleamScale = new Vector2(0.5f, 2f) * opacity;
            bigGleamColor *= opacity;
            smallGleamColor *= opacity;

            Main.spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 1.57079637f, origin, bigGleamScale, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 0f, origin, smallGleamScale, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 1.57079637f, origin, bigGleamScale * 0.6f, 0, 0);
            Main.spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 0f, origin, smallGleamScale * 0.6f, 0, 0);
            return false;
        }
    }
}
