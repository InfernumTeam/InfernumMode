using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class RedirectingDarkMagicFlame : ModProjectile
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Flame");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 480;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Time, true);

            if (Time < 25f)
                Projectile.velocity *= 1.015f;
            else if (Time < 50f)
                Projectile.velocity *= 0.97f;
            else
            {
                if (!Projectile.WithinRange(closestPlayer.Center, 220f))
                    Projectile.velocity = Projectile.velocity.MoveTowards(Projectile.SafeDirectionTo(closestPlayer.Center) * Projectile.velocity.Length(), 0.45f) * 1.05f;
            }

            if (Time == 50f)
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);

            if (Time > 100f)
            {
                float angularOffset = (float)Math.Cos((Projectile.Center * new Vector2(1.4f, 1f)).Length() / 175f + Projectile.identity * 0.89f) * 0.024f;
                Projectile.velocity = Projectile.velocity.RotatedBy(angularOffset);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(Projectile.velocity.Length() * 1.013f, 12f, 27f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Time++;
        }


        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(20f, 5f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Maroon, Color.Red, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A = 184;
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
