using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralStar : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public ref float AngerFactor => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
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
            Projectile.timeLeft = 480;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Time, true);

            if (Time < 45f)
                Projectile.velocity *= 1.015f;
            else if (Time < 150f)
                Projectile.velocity *= 0.97f;
            else
            {
                if (!Projectile.WithinRange(closestPlayer.Center, 220f))
                    Projectile.velocity = Projectile.velocity.MoveTowards(Projectile.SafeDirectionTo(closestPlayer.Center) * Projectile.velocity.Length(), 0.45f) * (1.03f + AngerFactor * 0.012f);
            }

            if (Time > 205f)
            {
                float angularOffset = (float)Math.Cos((Projectile.Center * new Vector2(1.4f, 1f)).Length() / 175f + Projectile.identity * 0.89f) * 0.024f;
                float acceleration = BossRushEvent.BossRushActive ? 1.04f : 1.013f;
                float baseMaxSpeed = BossRushEvent.BossRushActive ? 24f : 13f;
                Projectile.velocity = Projectile.velocity.RotatedBy(angularOffset);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(Projectile.velocity.Length() * acceleration, 7f, baseMaxSpeed + AngerFactor * 6.5f);
            }

            if (Time == 215f && Projectile.identity % 3 == 0)
                SoundEngine.PlaySound(SoundID.Item103, Projectile.Center);

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

            if (Time < 185f || Projectile.velocity.Length() < 9f)
                Main.spriteBatch.Draw(starTexture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, starTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            else
            {
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
            }

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
            color.A = 0;
            return color * Projectile.Opacity;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);

        public override bool? CanDamage() => Time > 75f ? null : false;
    }
}
