using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
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
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 1f, 0.56f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 10;
                return color;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(240f, 230f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 10;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
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
                rainbowMagic.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 scale = new Vector2(0.9f, Projectile.velocity.Length() * 0.3f + 1.4f) * Projectile.Size / texture.Size();
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = MyColor;
            color.A = 0;
            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, scale, 0, 0f);

            color = Color.White * Projectile.Opacity;
            color.A = 0;

            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, scale * 0.8f, 0, 0f);

            return false;
        }
    }
}
