using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyLightShard : ModProjectile
    {
        public static Color StreakBaseColor => Color.Pink;

        public const int ShootDelay = 42;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Light");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 25f && Time >= ShootDelay)
                Projectile.velocity *= 1.026f;

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.tileCollide = Time > 120f;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphInterpolant = Utils.GetLerpValue(0f, ShootDelay, Time, true);
            if (telegraphInterpolant >= 1f)
                telegraphInterpolant = 0f;

            Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 6000f, Color.Wheat * telegraphInterpolant, telegraphInterpolant * 7.5f);

            if (Time >= ShootDelay)
            {
                Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
                for (int i = 1; i < Projectile.oldPos.Length; i++)
                {
                    if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float completionRatio = i / (float)Projectile.oldPos.Length;
                    float fade = (float)Math.Pow(completionRatio, 2D);
                    float scale = Projectile.scale * MathHelper.Lerp(1.3f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) *
                        MathHelper.Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                    Color drawColor = Color.Lerp(StreakBaseColor, new Color(229, 255, 255), fade) * (1f - fade) * Projectile.Opacity;
                    drawColor.A = 0;

                    Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                    Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                    Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }

            return false;
        }

        public override bool? CanDamage() => Time >= ShootDelay;

        public override bool ShouldUpdatePosition() => Time >= ShootDelay;
    }
}