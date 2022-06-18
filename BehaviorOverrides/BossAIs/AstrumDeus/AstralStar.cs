using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralStar : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public ref float AngerFactor => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Star");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 24;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 54;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = 1;
            projectile.timeLeft = 480;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            projectile.Opacity = Utils.InverseLerp(0f, 20f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 20f, Time, true);

            if (Time < 45f)
                projectile.velocity *= 1.015f;
            else if (Time < 150f)
                projectile.velocity *= 0.97f;
            else
            {
                if (!projectile.WithinRange(closestPlayer.Center, 220f))
                    projectile.velocity = projectile.velocity.MoveTowards(projectile.SafeDirectionTo(closestPlayer.Center) * projectile.velocity.Length(), 0.45f) * (1.03f + AngerFactor * 0.012f);
            }

            if (Time > 205f)
            {
                float angularOffset = (float)Math.Cos((projectile.Center * new Vector2(1.4f, 1f)).Length() / 175f + projectile.identity * 0.89f) * 0.024f;
                float acceleration = BossRushEvent.BossRushActive ? 1.04f : 1.013f;
                float baseMaxSpeed = BossRushEvent.BossRushActive ? 24f : 13f;
                projectile.velocity = projectile.velocity.RotatedBy(angularOffset);
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(projectile.velocity.Length() * acceleration, 7f, baseMaxSpeed + AngerFactor * 6.5f);
            }

            if (Time == 215f && projectile.identity % 3 == 0)
                Main.PlaySound(SoundID.Item103, projectile.Center);

            projectile.rotation = projectile.rotation.AngleLerp(projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Texture2D starTexture = Main.projectileTexture[projectile.type];
            Vector2 largeScale = new Vector2(0.8f, 4f) * projectile.Opacity * 0.5f;
            Vector2 smallScale = new Vector2(0.8f, 1.25f) * projectile.Opacity * 0.5f;
            spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale, SpriteEffects.None, 0);
            spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale, SpriteEffects.None, 0);
            spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale * 0.6f, SpriteEffects.None, 0);
            spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale * 0.6f, SpriteEffects.None, 0);

            if (Time < 185f || projectile.velocity.Length() < 9f)
                spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, starTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            else
            {
                for (int i = 0; i < projectile.oldPos.Length - 1; ++i)
                {
                    float afterimageRot = projectile.oldRot[i];
                    SpriteEffects sfxForThisAfterimage = projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                    Vector2 drawPos = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
                    Color color = projectile.GetAlpha(lightColor) * ((float)(projectile.oldPos.Length - i) / projectile.oldPos.Length);
                    Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, projectile.scale, sfxForThisAfterimage, 0f);

                    drawPos += (projectile.oldPos[i + 1] - projectile.oldPos[i]) * 0.5f;
                    Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, projectile.scale, sfxForThisAfterimage, 0f);
                }
            }

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
            color.A = 0;
            return color * projectile.Opacity;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override bool CanDamage() => Time > 75f;
    }
}
