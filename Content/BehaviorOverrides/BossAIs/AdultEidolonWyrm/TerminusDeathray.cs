using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class TerminusDeathray : ModProjectile, IAboveWaterProjectileDrawer
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public Projectile Owner => CalamityUtils.FindProjectileByIdentity(OwnerIndex, Projectile.owner);

        public int OwnerIndex
        {
            get;
            set;
        }

        public float LaserLength
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public ref float AngularVelocity => ref Projectile.localAI[0];

        public const float MaxLaserLength = 4000f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Primordial Light");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLength);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLength = reader.ReadSingle();
        }

        public override void AI()
        {
            // Die if the owner is not present.
            if (Owner is null || Owner.type != ModContent.ProjectileType<HorizontalRayTerminus>())
            {
                Projectile.Kill();
                return;
            }

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            // Determine the scale of the laser.
            CalculateScale();

            Projectile.Center = Owner.Bottom + Vector2.UnitY * 40f;
            Projectile.velocity = Vector2.UnitY;
            if (Time >= Lifetime)
                Projectile.Kill();

            LaserLength = (float)Math.Pow(Utils.GetLerpValue(4f, 30f, Time, true), 2.4) * MaxLaserLength;

            // Create very strong screen shakes.
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.Remap(Time, 10f, 90f, 20f, 3f);

            Time++;
        }

        public void CalculateScale()
        {
            Projectile.scale = CalamityUtils.Convert01To010(Time / Lifetime) * 1.55f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (MaxLaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.92f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.04f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Yellow, Color.IndianRed, 0.85f);
            return color * Projectile.Opacity * 1.5f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawFrontGlow()
        {
            float pulse = MathF.Cos(Main.GlobalTimeWrappedHourly * 36f);
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            Vector2 origin = backglowTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 50f;
            Vector2 baseScale = new Vector2(1f + pulse * 0.1f, 1f) * Projectile.scale * 1.1f;
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.White * Projectile.scale, 0f, origin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.IndianRed * Projectile.scale * 0.4f, 0f, origin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, drawPosition, null, Color.Red * Projectile.scale * 0.3f, 0f, origin, baseScale * 1.7f, 0, 0f);
        }

        public void DrawBloomFlare()
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BloomFlare").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 20f;
            Color bloomFlareColor = Color.Lerp(Color.Wheat, Color.Yellow, 0.7f);
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.64f;
            float bloomFlareScale = Projectile.scale * 0.33f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, -bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);

            bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + 0.5f) % 1f, 1f, 0.55f), 0.7f);
            bloomFlareColor = Color.Lerp(bloomFlareColor, Color.Red, 0.85f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);
        }

        public override bool ShouldUpdatePosition() => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);

            spriteBatch.SetBlendState(BlendState.Additive);
            DrawFrontGlow();
            DrawBloomFlare();

            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseSaturation(1.4f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(0.1f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.HarshNoise);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.Shader.Parameters["uStretchReverseFactor"].SetValue((LaserLength + 1f) / MaxLaserLength);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 16; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 16f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
            {
                float backwardsOffset = MathHelper.Min(LaserLength * 0.1f, 100f);
                BeamDrawer.Draw(points, -Main.screenPosition - Projectile.velocity * backwardsOffset, 47);
            }
        }
    }
}
