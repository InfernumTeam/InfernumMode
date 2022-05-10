using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class HadalSpirit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hadal Spirit");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 240;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(240f, 230f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 8f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            float etherealnessFactor = projectile.Opacity * 0.6f;
            float opacity = MathHelper.Lerp(1f, 0.75f, etherealnessFactor) * projectile.Opacity;
            Color color = Color.Lerp(lightColor, Main.hslToRgb(Main.GlobalTime * 0.7f % 1f, 1f, 0.85f), etherealnessFactor * 0.85f);
            color.A = (byte)(int)(255 - etherealnessFactor * 84f);
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            if (etherealnessFactor > 0f)
            {
                float etherealOffsetPulse = etherealnessFactor * 3f;

                for (int i = 0; i < 16; i++)
                {
                    Color baseColor = Main.hslToRgb((Main.GlobalTime * 1.7f + i / 16f + projectile.identity * 0.31f) % 1f, 1f, 0.9f);
                    Color etherealAfterimageColor = Color.Lerp(lightColor, baseColor, etherealnessFactor * 0.85f) * 0.32f;
                    etherealAfterimageColor.A = (byte)(int)(255 - etherealnessFactor * 255f);
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * etherealOffsetPulse;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, frame, etherealAfterimageColor * opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
                }
            }

            for (int i = 0; i < (int)Math.Round(1f + etherealnessFactor); i++)
                spriteBatch.Draw(texture, drawPosition, frame, color * opacity, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
