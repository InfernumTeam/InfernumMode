using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class AcceleratingPrismaticBolt : ModProjectile
    {
        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(projectile.ai[1] % 1f, 1f, 0.5f) * projectile.Opacity * 1.3f;
                if (EmpressOfLightNPC.ShouldBeEnraged)
                    color = Main.OurFavoriteColor * 1.35f;

                color.A /= 8;
                return color;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Bolt");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 230;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.GetLerpValue(240f, 220f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 60f)
                projectile.velocity *= 1.06f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < projectile.oldPos.Length; i++)
            {
                int x = (int)projectile.oldPos[i].X;
                int y = (int)projectile.oldPos[i].Y;
                if (new Rectangle(x, y, 30, 30).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 20;
            float angularOffset = projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust rainbowMagic = Dust.NewDustPerfect(projectile.Center, 267);
                rainbowMagic.fadeIn = 1f;
                rainbowMagic.noGravity = true;
                rainbowMagic.alpha = 100;
                rainbowMagic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat(0.3f));
                if (i % 4 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 3.2f;
                    rainbowMagic.scale = 2.3f;
                }
                else if (i % 2 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 1.8f;
                    rainbowMagic.scale = 1.9f;
                }
                else
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2();
                    rainbowMagic.scale = 1.6f;
                }
                angularOffset += MathHelper.TwoPi / dustCount;
                rainbowMagic.velocity += projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;
            for (int i = 0; i < projectile.oldPos.Length; ++i)
            {
                float afterimageRot = projectile.oldRot[i];
                Vector2 drawPos = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
                Color afterimageColor = MyColor * ((projectile.oldPos.Length - i) / (float)projectile.oldPos.Length);
                spriteBatch.Draw(texture, drawPos, null, afterimageColor, afterimageRot, origin, projectile.scale, 0, 0f);

                if (i > 0)
                {
                    for (float j = 0.2f; j < 0.8f; j += 0.2f)
                    {
                        drawPos = Vector2.Lerp(projectile.oldPos[i - 1], projectile.oldPos[i], j) +
                            projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
                        spriteBatch.Draw(texture, drawPos, null, afterimageColor, afterimageRot, origin, projectile.scale, 0, 0f);
                    }
                }
            }

            Color color = MyColor * 0.5f;
            color.A = 0;

            spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, origin, projectile.scale * 0.9f, 0, 0);
            Color bigGleamColor = color;
            Color smallGleamColor = color * 0.5f;
            float opacity = Utils.GetLerpValue(15f, 30f, projectile.timeLeft, true) * 
                Utils.GetLerpValue(240f, 200f, projectile.timeLeft, true) * 
                (1f + 0.2f * (float)Math.Cos(Main.GlobalTime % 30f / 0.5f * MathHelper.Pi * 6f)) * 0.8f;
            Vector2 bigGleamScale = new Vector2(0.5f, 5f) * opacity;
            Vector2 smallGleamScale = new Vector2(0.5f, 2f) * opacity;
            bigGleamColor *= opacity;
            smallGleamColor *= opacity;

            spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 1.57079637f, origin, bigGleamScale, 0, 0);
            spriteBatch.Draw(texture, drawPosition, null, bigGleamColor, 0f, origin, smallGleamScale, 0, 0);
            spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 1.57079637f, origin, bigGleamScale * 0.6f, 0, 0);
            spriteBatch.Draw(texture, drawPosition, null, smallGleamColor, 0f, origin, smallGleamScale * 0.6f, 0, 0);
            return false;
        }
    }
}
