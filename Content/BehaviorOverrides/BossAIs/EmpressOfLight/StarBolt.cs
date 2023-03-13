using CalamityMod.DataStructures;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class StarBolt : ModProjectile, IAdditiveDrawer
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            set;
        }

        public Color MyColor
        {
            get
            {
                Color color = Main.hslToRgb(Projectile.ai[1] * 4f % 1f, 1f, 0.53f) * Projectile.Opacity * 1.3f;
                if (EmpressOfLightBehaviorOverride.ShouldBeEnraged)
                    color = EmpressOfLightBehaviorOverride.GetDaytimeColor(Projectile.ai[1] % 1f) * Projectile.Opacity;

                color.A /= 10;
                return color;
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public static int FireDelay => 96;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Star Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 900;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 2;
            Projectile.ignoreWater = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public static Vector2 StarPolarEquation(int pointCount, float angle)
        {
            float spacedAngle = angle;

            // There should be a star point that looks directly upward. However, that isn't the case for non-even star counts.
            // To address this, a -90 degree rotation is performed.
            if (pointCount % 2 != 0)
                spacedAngle -= MathHelper.PiOver2;

            // Refer to desmos to view the resulting shape this creates. It's basically a black box of trig otherwise.
            float numerator = (float)Math.Cos(MathHelper.Pi * (pointCount + 1f) / pointCount);
            float starAdjustedAngle = (float)Math.Asin(Math.Cos(pointCount * spacedAngle)) * 2f;
            float denominator = (float)Math.Cos((starAdjustedAngle + MathHelper.PiOver2 * pointCount) / (pointCount * 2f));
            Vector2 result = angle.ToRotationVector2() * numerator / denominator / 1.732051f;
            return result;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Decelerate over time.
            if (Time >= 30f && Time < FireDelay)
                Projectile.velocity *= 0.94f;

            if (Time >= FireDelay && Projectile.velocity.Length() < 40f)
            {
                if (Projectile.velocity.Length() < 7f)
                    Projectile.velocity = StarPolarEquation(5, MathHelper.TwoPi * Projectile.ai[1]) * 13f;

                Projectile.velocity *= 1.0126f;
            }

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            int dustCount = 10;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust rainbowMagic = Dust.NewDustPerfect(Projectile.Center, 267);
                rainbowMagic.fadeIn = 1f;
                rainbowMagic.noGravity = true;
                rainbowMagic.color = Main.hslToRgb(Main.rand.NextFloat(), 0.9f, 0.6f) * 0.8f;
                if (i % 4 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 3.2f;
                    rainbowMagic.scale = 1.2f;
                }
                else if (i % 2 == 0)
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2() * 1.8f;
                    rainbowMagic.scale = 0.85f;
                }
                else
                {
                    rainbowMagic.velocity = angularOffset.ToRotationVector2();
                    rainbowMagic.scale = 0.8f;
                }
                angularOffset += MathHelper.TwoPi / dustCount;
                rainbowMagic.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public Color ColorFunction(float completionRatio)
        {
            Color rainbow = Main.hslToRgb((completionRatio - Main.GlobalTimeWrappedHourly * 1.4f) % 1f, 1f, 0.5f);
            Color c = Color.Lerp(MyColor with { A = 255 }, rainbow, completionRatio) * (1f - completionRatio) * Projectile.Opacity;
            return c * Utils.GetLerpValue(4.5f, 10.5f, Projectile.velocity.Length(), true);
        }

        public float WidthFunction(float completionRatio)
        {
            float fade = (1f - completionRatio) * Utils.GetLerpValue(-0.03f, 0.1f, completionRatio, true);
            return MathHelper.SmoothStep(0f, 1f, fade) * Projectile.Opacity * 10f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity.Length() < 4.5f || Time < FireDelay || InfernumConfig.Instance.ReducedGraphicsConfig)
                return false;

            // Initialize the telegraph drawer.
            TrailDrawer ??= new(WidthFunction, ColorFunction, specialShader: InfernumEffectsRegistry.PrismaticRayVertexShader);

            // Prepare trail data.
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseOpacity(0.2f);
            InfernumEffectsRegistry.PrismaticRayVertexShader.UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakSolid.Value;

            // Draw the afterimage trail.
            Rectangle cutoffRegion = new(-50, -50, Main.screenWidth + 100, Main.screenHeight + 100);
            Main.spriteBatch.EnforceCutoffRegion(cutoffRegion, Main.GameViewMatrix.TransformationMatrix, SpriteSortMode.Immediate, BlendState.Additive);

            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f + Projectile.velocity.SafeNormalize(Vector2.Zero) * 6f - Main.screenPosition, 11);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            // Draw the gleam.
            Texture2D sparkleTexture = InfernumTextureRegistry.LargeStar.Value;
            Color sparkleColor = Color.Lerp(MyColor, Color.White, 0.4f) with { A = 255 };
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            Vector2 origin = sparkleTexture.Size() * 0.5f;
            Vector2 sparkleScale = new Vector2(0.5f, 1f) * Projectile.Opacity * Projectile.scale * 0.18f;
            Vector2 orthogonalsparkleScale = new Vector2(0.5f, 1.6f) * Projectile.Opacity * Projectile.scale * 0.18f;
            spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale, 0, 0f);
            spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, Projectile.rotation, origin, sparkleScale, 0, 0f);
            spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, MathHelper.PiOver2 + Projectile.rotation, origin, orthogonalsparkleScale * 0.6f, 0, 0f);
            spriteBatch.Draw(sparkleTexture, drawCenter, null, sparkleColor, Projectile.rotation, origin, sparkleScale * 0.6f, 0, 0f);
        }
    }
}
