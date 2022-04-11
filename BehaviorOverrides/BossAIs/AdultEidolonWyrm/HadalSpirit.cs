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
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 240;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.Opacity = Utils.GetLerpValue(240f, 230f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float etherealnessFactor = Projectile.Opacity * 0.6f;
            float opacity = MathHelper.Lerp(1f, 0.75f, etherealnessFactor) * Projectile.Opacity;
            Color color = Color.Lerp(lightColor, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.7f % 1f, 1f, 0.85f), etherealnessFactor * 0.85f);
            color.A = (byte)(int)(255 - etherealnessFactor * 84f);
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            if (etherealnessFactor > 0f)
            {
                float etherealOffsetPulse = etherealnessFactor * 3f;

                for (int i = 0; i < 16; i++)
                {
                    Color baseColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 1.7f + i / 16f + Projectile.identity * 0.31f) % 1f, 1f, 0.9f);
                    Color etherealAfterimageColor = Color.Lerp(lightColor, baseColor, etherealnessFactor * 0.85f) * 0.32f;
                    etherealAfterimageColor.A = (byte)(int)(255 - etherealnessFactor * 255f);
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * etherealOffsetPulse;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, frame, etherealAfterimageColor * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
                }
            }

            for (int i = 0; i < (int)Math.Round(1f + etherealnessFactor); i++)
                spriteBatch.Draw(texture, drawPosition, frame, color * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
