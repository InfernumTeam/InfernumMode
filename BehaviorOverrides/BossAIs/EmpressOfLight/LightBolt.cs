using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightBolt : ModProjectile
    {
        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(projectile.ai[1] % 1f, 1f, 0.56f) * projectile.Opacity * 1.3f;
                if (EmpressOfLightNPC.ShouldBeEnraged)
                    color = Main.OurFavoriteColor * 1.35f;

                color.A /= 10;
                return color;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Bolt");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 20;
        }

        public override void SetDefaults()
        {
            projectile.width = 16;
            projectile.height = 16;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 240;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(240f, 230f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 10;
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
            Vector2 scale = new Vector2(0.9f, projectile.velocity.Length() * 0.3f + 1.4f) * projectile.Size / texture.Size();
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Color color = MyColor;
            color.A = 0;
            spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, origin, scale, 0, 0f);

            color = Color.White * projectile.Opacity;
            color.A = 0;

            spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, origin, scale * 0.8f, 0, 0f);

            return false;
        }
    }
}
