using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class MagicSpiralCrystalShot : ModProjectile, IScreenCullDrawer
    {
        public static readonly Color[] ColorSet =
        [
            // Pale pink crystal.
            new Color(181, 136, 177),

            // Profaned fire.
            new Color(255, 191, 73),

            // Yellow-orange crystal.
            new Color(255, 194, 161),
        ];

        public ref float Timer => ref Projectile.ai[0];

        public Color StreakBaseColor => LumUtils.MulticolorLerp(Projectile.localAI[0] % 0.999f, ColorSet);

        public ref float Direction => ref Projectile.ai[1];

        public Vector2 InitialVelocity;

        public Vector2 InitialCenter;

        public float RotationAmount => Lerp(0.034f, 0.001f, Timer / 300f);

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystalline Light");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Timer == 0)
            {
                InitialVelocity = Projectile.velocity;
                InitialCenter = Projectile.Center;
                Projectile.velocity = Vector2.Zero;
            }

            if (Timer < 20)
            {
                Projectile.Center = InitialCenter;
                Timer++;
                return;
            }
            if (Timer == 20)
                Projectile.velocity = InitialVelocity;

            Projectile.velocity = Projectile.velocity.RotatedBy(Direction * RotationAmount);

            Projectile.velocity *= 1.01f;

            if (Projectile.timeLeft < 15)
                Projectile.damage = 0;

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void CullDraw(SpriteBatch spriteBatch)
        {
            if (Timer < 30)
                DrawLines(Main.spriteBatch);

            Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)Projectile.oldPos.Length;
                float fade = Pow(completionRatio, 2f);
                float scale = Projectile.scale * Lerp(1.3f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) *
                    Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.Lerp(StreakBaseColor, new Color(229, 255, 255), fade) * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        public void DrawLines(SpriteBatch spriteBatch)
        {
            // The total number of lines to draw.
            int totalDrawPoints = 80;

            Texture2D lineTexture = InfernumTextureRegistry.Pixel.Value;

            // Initialize the previous point + velocity with the projectiles initial ones.
            Vector2 previousDrawPoint = InitialCenter;
            Vector2 previousDrawVelocity = InitialVelocity;

            float lineOpacityScalar = Sin(Timer / 30 * Pi);

            // Loop through the total number of draw points.
            for (int i = 0; i < totalDrawPoints; i++)
            {
                // Get the rotation amount. This is the same as used by the projectiles movement.
                float rotationAmount = Lerp(0.034f, 0.001f, i / 300f);
                // Get a velocity, from rotating the last one by the rotation amount. This is how the projectile moves.
                Vector2 drawVelocity = previousDrawVelocity.RotatedBy(Direction * rotationAmount);
                // And also scale it.
                drawVelocity *= 1.01f;
                // Create a "center" to draw at by adding the current velocity to the previous position.
                Vector2 drawPoint = previousDrawPoint + drawVelocity;
                // Get the direction between the two points.
                Vector2 direction = previousDrawPoint - drawPoint;
                // Get the length of this. This doesn't fully connect normally so adding 0.5 to the length is a shitty
                // hack to make them work. However, this means you cannot use additive drawing due to the overlap being visible.
                float length = direction.Length() + 0.5f;
                // Use this to create a rectangle.
                Rectangle rectangle = new(0, 0, (int)length, 4);
                // Set the color of the line.
                Color lineColor = Color.Lerp(Color.HotPink, StreakBaseColor, lineOpacityScalar) * 1.3f;
                // Make it fade out for the last bit.
                if (totalDrawPoints - i <= 50)
                {
                    float interpolant = ((float)i - (totalDrawPoints - 50)) / (totalDrawPoints - (totalDrawPoints - 50));
                    lineColor = Color.Lerp(lineColor, Color.Transparent, interpolant);
                }
                lineColor *= lineOpacityScalar;
                // Draw the line.
                spriteBatch.Draw(lineTexture, previousDrawPoint - Main.screenPosition, rectangle, lineColor, direction.ToRotation(), rectangle.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                // Update the previous points.
                previousDrawPoint = drawPoint;
                previousDrawVelocity = drawVelocity;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < 3; i++)
            {
                if (targetHitbox.Intersects(Utils.CenteredRectangle(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Size)))
                    return true;
            }
            return false;
        }
    }
}
