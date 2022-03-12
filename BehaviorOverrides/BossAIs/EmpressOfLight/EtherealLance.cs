using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
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
                Color color = Main.hslToRgb(projectile.ai[1] % 1f, 1f, 0.5f) * projectile.Opacity * 1.3f;
                if (EmpressOfLightNPC.ShouldBeEnraged)
                    color = Main.OurFavoriteColor * 1.35f;

                color.A /= 8;
                return color;
            }
        }

        public ref float Time => ref projectile.localAI[0];

        public const int FireDelay = 60;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ethereal Lance");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 10;
        }

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 120;
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
            if (Time >= FireDelay)
            {
                projectile.velocity = projectile.ai[0].ToRotationVector2() * 60f;
                if (Main.rand.NextBool(3))
                {
                    Dust rainbowMagic = Dust.NewDustPerfect(projectile.Center, 267);
                    rainbowMagic.fadeIn = 1f;
                    rainbowMagic.noGravity = true;
                    rainbowMagic.alpha = 100;
                    rainbowMagic.color = Color.Lerp(MyColor, Color.White, Main.rand.NextFloat() * 0.4f);
                    rainbowMagic.noLight = true;
                    rainbowMagic.scale *= 1.5f;
                }
            }
            projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Utils.InverseLerp(0f, 20f, Time, true));
            projectile.rotation = projectile.ai[0];
            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.RotatingHitboxCollision(projectile, targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool CanDamage() => Time >= FireDelay;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D telegraphTex = ModContent.GetTexture("InfernumMode/ExtraTextures/Line");
            Texture2D tex = Main.projectileTexture[projectile.type];

            int telegraphSize = 3600;
            if (projectile.localAI[1] > 0f)
                telegraphSize = (int)projectile.localAI[1];

            Vector2 drawPos = projectile.Center - Main.screenPosition;            
            Vector2 telegraphOrigin = telegraphTex.Size() * new Vector2(0f, 0.5f);
            Vector2 outerTelegraphScale = new Vector2(telegraphSize / (float)telegraphTex.Width, 4f);
            Vector2 innerTelegraphScale = new Vector2(telegraphSize / (float)telegraphTex.Width * 0.5f, 4f);

            Color lanceColor = MyColor;
            Color telegraphColor = MyColor;
            lanceColor.A = 0;
            telegraphColor.A /= 2;

            Color fadedLanceColor = lanceColor * Utils.InverseLerp(FireDelay, FireDelay - 5f, Time, true) * Utils.InverseLerp(0f, 10f, Time, true);
            Color outerLanceColor = Color.White * Utils.InverseLerp(0f, 20f, Time, true);
            outerLanceColor.A /= 2;

            spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor, projectile.rotation, telegraphOrigin, innerTelegraphScale, 0, 0f);
            spriteBatch.Draw(telegraphTex, drawPos, null, fadedLanceColor * 0.3f, projectile.rotation, telegraphOrigin, outerTelegraphScale, 0, 0f);

            Vector2 origin = tex.Size() / 2f;
            float scale = MathHelper.Lerp(0.7f, 1f, Utils.InverseLerp(FireDelay - 5f, FireDelay, Time, true));
            float telegraphInterpolant = Utils.InverseLerp(10f, FireDelay, Time, false);
            if (telegraphInterpolant > 0f)
            {
                for (float i = 1f; i > 0f; i -= 1f / 16f)
                {
                    Vector2 lineOffset = projectile.rotation.ToRotationVector2() * Utils.InverseLerp(0f, 1f, projectile.velocity.Length(), true) * i * -120f;
                    spriteBatch.Draw(tex, drawPos + lineOffset, null, lanceColor * telegraphInterpolant * (1f - i), projectile.rotation, origin, scale, 0, 0f);
                    spriteBatch.Draw(tex, drawPos + lineOffset, null, new Color(255, 255, 255, 0) * telegraphInterpolant * (1f - i) * 0.15f, projectile.rotation, origin, scale * 0.85f, 0, 0f);
                }
                for (float i = 0f; i < 1f; i += 0.25f)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i + projectile.rotation).ToRotationVector2() * scale * 2f;
                    spriteBatch.Draw(tex, drawPos + drawOffset, null, telegraphColor * telegraphInterpolant, projectile.rotation, origin, scale, 0, 0f);
                }
                spriteBatch.Draw(tex, drawPos, null, telegraphColor * telegraphInterpolant, projectile.rotation, origin, scale * 1.1f, 0, 0f);
            }
            spriteBatch.Draw(tex, drawPos, null, outerLanceColor, projectile.rotation, origin, scale, 0, 0f);
            return false;
        }
    }
}
