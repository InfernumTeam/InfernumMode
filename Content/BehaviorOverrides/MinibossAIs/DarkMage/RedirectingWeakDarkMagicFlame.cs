using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class RedirectingWeakDarkMagicFlame : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy TrailDrawer = null;
        public ref float Time => ref Projectile.ai[0];
        public bool FromBuffedDarkMage => Projectile.ai[1] == 1f;
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
            Projectile.scale = 0.6f;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Time, true);

            if (Time < 20f)
                Projectile.velocity *= 1.01f;
            else if (Time < 40f)
                Projectile.velocity *= 0.97f;
            else
            {
                if (!Projectile.WithinRange(closestPlayer.Center, 220f))
                {
                    float acceleration = FromBuffedDarkMage ? 1.032f : 1.02f;
                    Projectile.velocity = Projectile.velocity.MoveTowards(Projectile.SafeDirectionTo(closestPlayer.Center) * Projectile.velocity.Length(), 0.45f) * acceleration;
                }
            }

            if (Time == 50f)
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);

            if (Time > 100f)
            {
                float minSpeed = 6f;
                float maxSpeed = 15f;
                float acceleration = 1.013f;
                if (FromBuffedDarkMage)
                {
                    minSpeed += 4.5f;
                    maxSpeed += 5.5f;
                    acceleration *= 1.3f;
                }

                float angularOffset = MathF.Cos((Projectile.Center * new Vector2(1.4f, 1f)).Length() / 275f + Projectile.identity * 0.89f) * 0.01f;
                Projectile.velocity = Projectile.velocity.RotatedBy(angularOffset);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(Projectile.velocity.Length() * acceleration, minSpeed, maxSpeed);
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
            float trailOpacity = Utils.GetLerpValue(0.75f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Pink, Color.Red, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Violet, new Color(1f, 1f, 1f, 1f), Projectile.identity / 5f * 0.6f));

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            TrailDrawer ??= new PrimitiveTrailCopy(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            TrailDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 30);
        }
    }
}
