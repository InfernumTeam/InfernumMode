using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CloudElemental
{
    // Not going to use the primitive lighting projectile until the lag is fixed.
    public class CloudLightning : BaseLaserbeamProjectile
    {
        public PrimitiveTrailCopy LightningDrawer { get; private set; } = null;

        public const int TelegraphFadeTime = 15;

        public const int TelegraphTotalTime = 30;

        public const int LightningTime = 60;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override float Lifetime => TelegraphTotalTime + LightningTime;

        public override float MaxScale => 1f;

        public override float MaxLaserLength => 3000;

        public override Texture2D LaserBeginTexture => null;

        public override Texture2D LaserMiddleTexture => null;

        public override Texture2D LaserEndTexture => null;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 25;
            Projectile.hostile = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => Time > TelegraphTotalTime;

        public override bool PreDraw(ref Color lightColor)
        {
            LightningDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVertexShader);
            Vector2 basePos = Projectile.Center - Main.screenPosition;
            // Telegraphs.
            if (Time <= TelegraphTotalTime)
            {
                float yScale = 2f;
                if (Time < TelegraphFadeTime)
                    yScale = MathHelper.Lerp(0f, 2f, Time / 15f);
                if (Time > TelegraphTotalTime - TelegraphFadeTime)
                    yScale = MathHelper.Lerp(2f, 0f, (Time - (TelegraphTotalTime - TelegraphFadeTime)) / 15f);

                Vector2 scaleInner = new(2500 / InfernumTextureRegistry.TelegraphLine.Value.Width, yScale);
                Vector2 origin = InfernumTextureRegistry.TelegraphLine.Value.Size() * new Vector2(0f, 0.5f);
                Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);

                Color colorOuter = Color.Lerp(Color.LightBlue, Color.Cyan, Time / TelegraphTotalTime * 2f % 1f);
                Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f);

                colorOuter *= 0.6f;
                colorInner *= 0.6f;

                Main.spriteBatch.Draw(InfernumTextureRegistry.TelegraphLine.Value, basePos, null, colorInner, Projectile.velocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(InfernumTextureRegistry.TelegraphLine.Value, basePos, null, colorOuter, Projectile.velocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);
            }
            // Draw the lightning itself.
            else if (Time > TelegraphTotalTime)
            {
                InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.LightningStreak);
                InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(Color.White);

                Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
                Vector2[] baseDrawPoints = new Vector2[8];
                for (int i = 0; i < baseDrawPoints.Length; i++)
                    baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

                LightningDrawer.Draw(baseDrawPoints, -Main.screenPosition, 10);
            }
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float widthScalar = MathHelper.Clamp(MathHelper.Lerp(0, 1, (Time - TelegraphTotalTime) / 20), 0 ,1);
            float baseWidth = Projectile.width * Projectile.scale * 2 * widthScalar;
            return MathHelper.Lerp(baseWidth, baseWidth * 0.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Cyan;
        }
    }
}
