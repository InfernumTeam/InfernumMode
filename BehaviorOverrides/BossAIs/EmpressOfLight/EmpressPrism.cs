using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressPrism : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
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
            DisplayName.SetDefault("Prism");
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 48;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 900;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(240f, 220f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 60f)
                projectile.velocity *= 1.06f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
            Vector2 origin = texture.Size() * 0.5f;

            float fadeInInterpolant = Utils.InverseLerp(900f, 855f, projectile.timeLeft, true);
            float fadeOffset = MathHelper.Lerp(45f, 6f, fadeInInterpolant);
            for (int i = 0; i < 8; i++)
            {
                Color color = Main.hslToRgb((i / 8f + Main.GlobalTime * 0.5f) % 1f, 1f, 0.5f) * (float)Math.Sqrt(fadeInInterpolant);
                color.A = 0;

                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + fadeInInterpolant * MathHelper.TwoPi + Main.GlobalTime * 1.5f).ToRotationVector2() * fadeOffset;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, 0f, origin, projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
