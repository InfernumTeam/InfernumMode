using CalamityMod;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class HeartSummoningDagger : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float SwingDirection => ref Projectile.ai[1];

        public ref float BladeHorizontalFactor => ref Projectile.localAI[0];

        public const int AnimationTime = 48;

        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Sacrifice";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Sacrificial Dagger");

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Initialize the swing direction of the dagger.
            if (SwingDirection == 0f)
            {
                SwingDirection = -SupremeCalamitasBehaviorOverride.SCal.spriteDirection;
                Projectile.netUpdate = true;
            }

            // Fade in quickly.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            float swingCompletion = Utils.GetLerpValue(0f, AnimationTime, Time, true);

            // Calculate the direction of the blade.
            float swingSpeedInterpolant = MathHelper.Lerp(0.27f, 1f, Utils.GetLerpValue(0f, 0.2f, swingCompletion, true));
            float horizontalAngle = MathHelper.Lerp(2.6f, -2.38f, (float)Math.Pow(MathHelper.SmoothStep(0f, 1f, swingCompletion), 3D));
            Vector2 aimDirection = horizontalAngle.ToRotationVector2();

            // Determine the horizontal stretch offset of the blade. This is used in matrix math below to create 2.5D visuals.
            BladeHorizontalFactor = MathHelper.Lerp(1f, 1.25f, aimDirection.X * 0.5f + 0.5f);

            float idealRotation = horizontalAngle + MathHelper.PiOver4;
            if (SwingDirection == -1f)
                idealRotation += MathHelper.Pi;

            Projectile.rotation = Projectile.rotation.AngleTowards(idealRotation, swingSpeedInterpolant * 0.45f).AngleLerp(idealRotation, swingSpeedInterpolant * 0.2f);
            Projectile.Center = SupremeCalamitasBehaviorOverride.CalculateHandPosition();
            Projectile.position.Y += 24f;

            // Create a bunch of flames when swinging.
            if (swingCompletion < 1f && swingSpeedInterpolant > 0.97f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fireSpawnOffset = aimDirection * Main.rand.NextFloat(1f, 1.5f) * SwingDirection * -Projectile.height;
                    Vector2 fireSpawnPosition = Projectile.position - fireSpawnOffset;
                    Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 267);
                    fire.velocity = fireSpawnOffset.RotatedBy(MathHelper.PiOver2 * -SwingDirection).SafeNormalize(Vector2.UnitY) * 4f;
                    fire.velocity.X += SwingDirection * 4f;
                    fire.scale = 1.5f;
                    fire.fadeIn = 0.6f;
                    fire.color = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), Color.Fuchsia, Color.Red, Color.Orange);
                    fire.noGravity = true;
                }
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();
            CalamityUtils.CalculatePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix);

            InfernumEffectsRegistry.LinearTransformationVertexShader.UseColor(Main.hslToRgb(0.95f, 0.85f, 0.5f));
            InfernumEffectsRegistry.LinearTransformationVertexShader.UseOpacity(0f);
            InfernumEffectsRegistry.LinearTransformationVertexShader.Shader.Parameters["uWorldViewProjection"].SetValue(viewMatrix * projectionMatrix);
            InfernumEffectsRegistry.LinearTransformationVertexShader.Shader.Parameters["localMatrix"].SetValue(new Matrix()
            {
                M11 = BladeHorizontalFactor,
                M12 = 0f,
                M21 = 0f,
                M22 = 1f,
            });
            InfernumEffectsRegistry.LinearTransformationVertexShader.Apply();

            CalamityUtils.DrawAfterimagesCentered(Projectile, 2, lightColor);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = Vector2.UnitY * texture.Height;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            drawPosition -= Projectile.Size * new Vector2(0f, 0.5f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
