using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.KingSlime
{
    public class Shuriken : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shuriken");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.4f;
            projectile.tileCollide = projectile.timeLeft < 90;

            if (projectile.velocity.Length() < 9.5f)
                projectile.velocity *= 1.0145f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D shurikenTexture = Main.projectileTexture[projectile.type];
            Texture2D outlineTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/KingSlime/ShurikenOutline");
            float pulseOutwardness = MathHelper.Lerp(2f, 3f, (float)Math.Cos(Main.GlobalTime * 2.5f) * 0.5f + 0.5f);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * pulseOutwardness;
                Vector2 drawPosition = projectile.Center - Main.screenPosition + drawOffset;
                Color innerAfterimageColor = Color.Wheat * projectile.Opacity * 0.5f;
                innerAfterimageColor.A = 0;

                Color outerAfterimageColor = Color.Lerp(Color.DarkGray, Color.Black, 0.66f) * projectile.Opacity * 0.5f;
                spriteBatch.Draw(shurikenTexture, drawPosition, null, outerAfterimageColor, projectile.rotation, shurikenTexture.Size() * 0.5f, projectile.scale * 1.085f, SpriteEffects.None, 0f);
                spriteBatch.Draw(shurikenTexture, drawPosition, null, innerAfterimageColor, projectile.rotation, shurikenTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 outlineDrawPosition = projectile.Center - Main.screenPosition;
            spriteBatch.Draw(outlineTexture, outlineDrawPosition, null, Color.White * projectile.Opacity * 0.8f, projectile.rotation, outlineTexture.Size() * 0.5f, projectile.scale * 1.25f, SpriteEffects.None, 0f);
            spriteBatch.Draw(shurikenTexture, outlineDrawPosition, null, lightColor * projectile.Opacity, projectile.rotation, outlineTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft) => Collision.HitTiles(projectile.position, projectile.velocity, 24, 24);
    }
}
