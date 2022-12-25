using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EtherealLance : ModProjectile
    {
        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] % 1f, 1f, 0.5f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 8;
                return color;
            }
        }

        public ref float Time => ref Projectile.localAI[0];

        public const int FireDelay = 60;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ethereal Lance");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 120;
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
            if (Time >= FireDelay)
            {
                Projectile.velocity = Projectile.ai[0].ToRotationVector2() * 60f;
                if (Main.rand.NextBool(3))
                {
                    Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
                    rainbowMagic.fadeIn = 1f;
                    rainbowMagic.noGravity = true;
                    rainbowMagic.alpha = 100;
                    rainbowMagic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat() * 0.4f);
                    rainbowMagic.noLight = true;
                    rainbowMagic.scale *= 1.5f;
                }
            }
            Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Utils.GetLerpValue(0f, 20f, Time, true));
            Projectile.rotation = Projectile.ai[0];
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.RotatingHitboxCollision(Projectile, targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Time >= FireDelay ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D telegraphTex = InfernumTextureRegistry.Line.Value;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;

            int telegraphSize = 3600;
            if (Projectile.localAI[1] > 0f)
                telegraphSize = (int)Projectile.localAI[1];

            Vector2 drawPos = Projectile.Center - Main.screenPosition;            
            Vector2 telegraphOrigin = telegraphTex.Size() * new Vector2(0f, 0.5f);
            Vector2 outerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width, 4f);
            Vector2 innerTelegraphScale = new(telegraphSize / (float)telegraphTex.Width * 0.5f, 4f);

            Color lanceColor = MyColor;
            Color telegraphColor = MyColor;
            lanceColor.A = 0;
            telegraphColor.A /= 2;

            Color fadedLanceColor = lanceColor * Utils.GetLerpValue(FireDelay, FireDelay - 5f, Time, true) * Utils.GetLerpValue(0f, 10f, Time, true);
            Color outerLanceColor = Color.White * Utils.GetLerpValue(0f, 20f, Time, true);
            outerLanceColor.A /= 2;

            Main.spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor * 0.65f, Projectile.rotation, telegraphOrigin, innerTelegraphScale, 0, 0f);
            Main.spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor * 0.24f, Projectile.rotation, telegraphOrigin, outerTelegraphScale, 0, 0f);

            Vector2 origin = tex.Size() / 2f;
            float scale = MathHelper.Lerp(0.7f, 1f, Utils.GetLerpValue(FireDelay - 5f, FireDelay, Time, true));
            float telegraphInterpolant = Utils.GetLerpValue(10f, FireDelay, Time, false);
            if (telegraphInterpolant > 0f)
            {
                for (float i = 1f; i > 0f; i -= 1f / 16f)
                {
                    Vector2 lineOffset = Projectile.rotation.ToRotationVector2() * Utils.GetLerpValue(0f, 1f, Projectile.velocity.Length(), true) * i * -120f;
                    Main.spriteBatch.Draw(tex, drawPos + lineOffset, null, lanceColor * telegraphInterpolant * (1f - i), Projectile.rotation, origin, scale, 0, 0f);
                    Main.spriteBatch.Draw(tex, drawPos + lineOffset, null, new Color(255, 255, 255, 0) * telegraphInterpolant * (1f - i) * 0.15f, Projectile.rotation, origin, scale * 0.85f, 0, 0f);
                }
                for (float i = 0f; i < 1f; i += 0.25f)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i + Projectile.rotation).ToRotationVector2() * scale * 2f;
                    Main.spriteBatch.Draw(tex, drawPos + drawOffset, null, telegraphColor * telegraphInterpolant, Projectile.rotation, origin, scale, 0, 0f);
                }
                Main.spriteBatch.Draw(tex, drawPos, null, telegraphColor * telegraphInterpolant, Projectile.rotation, origin, scale * 1.1f, 0, 0f);
            }
            Main.spriteBatch.Draw(tex, drawPos, null, outerLanceColor, Projectile.rotation, origin, scale, 0, 0f);
            return false;
        }
    }
}
