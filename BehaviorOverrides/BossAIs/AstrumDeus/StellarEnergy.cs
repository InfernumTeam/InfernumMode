using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class StellarEnergy : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stellar Energy");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 54;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 420;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Time, true);

            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();
            if (stars.Count == 0 || stars.First().scale > 7f)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.WithinRange(stars.First().Center, (stars.First().ModProjectile as GiantAstralStar).Radius * 0.925f))
            {
                stars.First().scale += 0.085f;
                stars.First().netUpdate = true;
                Projectile.Kill();
            }

            Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(stars.First().Center), 0.085f) * 1.02f;
            Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D starTexture = Utilities.ProjTexture(Projectile.type);
            Vector2 largeScale = new Vector2(0.8f, 4f) * Projectile.Opacity * 0.5f;
            Vector2 smallScale = new Vector2(0.8f, 1.25f) * Projectile.Opacity * 0.5f;
            Main.spriteBatch.Draw(starTexture, drawPosition, null, Projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(starTexture, drawPosition, null, Projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(starTexture, drawPosition, null, Projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale * 0.6f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(starTexture, drawPosition, null, Projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale * 0.6f, SpriteEffects.None, 0);

            for (int i = 0; i < Projectile.oldPos.Length - 1; ++i)
            {
                float afterimageRot = Projectile.oldRot[i];
                SpriteEffects sfxForThisAfterimage = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                Color color = Projectile.GetAlpha(lightColor) * ((float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, Projectile.scale, sfxForThisAfterimage, 0f);

                drawPos += (Projectile.oldPos[i + 1] - Projectile.oldPos[i]) * 0.5f;
                Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, Projectile.scale, sfxForThisAfterimage, 0f);
            }

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
            color.A = 0;
            return color * Projectile.Opacity;
        }
    }
}
