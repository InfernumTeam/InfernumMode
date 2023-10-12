using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Common.BaseEntities
{
    public abstract class BasePulseRingProjectile : ModProjectile
    {
        #region Structs
        public struct PulseParameterData
        {
            public float NoiseZoom;

            public float NoiseSpeed;

            public float NoiseFactor;

            public float BrightnessFactor;

            public float Thickness;

            public float InnerNoiseFactor;

            public static PulseParameterData Default => new(10f, 9f, 1.2f, 2.5f, 0.03f, 3.5f);

            public PulseParameterData(float noiseZoom, float noiseSpeed, float noiseFactor, float brightnessFactor, float thickness, float innerNoiseFactor)
            {
                NoiseZoom = noiseZoom;
                NoiseSpeed = noiseSpeed;
                NoiseFactor = noiseFactor;
                BrightnessFactor = brightnessFactor;
                Thickness = thickness;
                InnerNoiseFactor = innerNoiseFactor;
            }
        }
        #endregion

        #region Statics
        internal static void DrawPulseRings()
        {
            List<BasePulseRingProjectile> rings = new();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];

                if (projectile.active && projectile.ModProjectile is BasePulseRingProjectile pulseRing)
                    rings.Add(pulseRing);
            }

            // Leave if no rings are found.
            if (!rings.Any())
                return;

            Texture2D texture = InfernumTextureRegistry.Invisible.Value;
            Vector2 drawPosition = Vector2.Zero;
            Color drawColor = Color.White;
            Vector2 scale = new(Main.screenWidth, Main.screenHeight);
            DrawData drawData = new(texture, drawPosition, null, drawColor, 0f, Vector2.Zero, scale, SpriteEffects.None);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer);

            foreach (var ring in rings)
                ring.DrawPulseRing(drawData);

            Main.spriteBatch.End();
        }
        #endregion

        #region Abstracts/Virtuals
        public virtual Texture2D BorderNoiseTexture { get; } = InfernumTextureRegistry.BlurryPerlinNoise.Value;

        public virtual Texture2D InnerNoiseTexure { get; } = InfernumTextureRegistry.BlurryPerlinNoise.Value;

        public virtual PulseParameterData SetParameters(float lifetimeCompletionRatio) => PulseParameterData.Default;

        public abstract int Lifetime { get; }

        public abstract Color DeterminePulseColor(float lifetimeCompletionRatio);

        public abstract float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer);
        #endregion

        #region AI
        public float StartingScale
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = StartingScale;
        }

        public override void AI()
        {
            // Do screen shake effects.
            float distanceFromPlayer = Projectile.Distance(Main.LocalPlayer.Center);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = DetermineScreenShakePower(1f - Projectile.timeLeft / (float)Lifetime, distanceFromPlayer);

            // Cause the pulse to expand outward, along with its hitbox.
            Projectile.scale += 60f;// Lerp(0f, MaxScale, ScaleEasing(Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true)));

            // Fade out at the end of the lifetime.
            Projectile.Opacity = Lerp(1f, 0f, Utils.GetLerpValue(Lifetime, Lifetime - 10, Projectile.timeLeft, true));
        }
        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPulseRing(DrawData data)
        {
            Effect pulseRing = InfernumEffectsRegistry.PulseRingShader.GetShader().Shader;

            float lifetimeRatio = 1f - Projectile.timeLeft / (float)Lifetime;
            PulseParameterData parameterData = SetParameters(lifetimeRatio);
            pulseRing.Parameters["noiseZoom"]?.SetValue(parameterData.NoiseZoom);
            pulseRing.Parameters["noiseSpeed"]?.SetValue(parameterData.NoiseSpeed);
            pulseRing.Parameters["noiseFactor"]?.SetValue(parameterData.NoiseFactor);
            pulseRing.Parameters["brightnessFactor"]?.SetValue(parameterData.BrightnessFactor);
            pulseRing.Parameters["thickness"]?.SetValue(parameterData.Thickness);
            pulseRing.Parameters["innerNoiseFactor"]?.SetValue(parameterData.InnerNoiseFactor);

            pulseRing.Parameters["time"]?.SetValue(Main.GlobalTimeWrappedHourly);
            // Use a base scale of 1200.
            float size = Projectile.scale / 1200f;
            pulseRing.Parameters["size"]?.SetValue(size);
            pulseRing.Parameters["opacity"]?.SetValue(Projectile.Opacity);
            pulseRing.Parameters["mainColor"]?.SetValue(DeterminePulseColor(lifetimeRatio).ToVector3());

            pulseRing.Parameters["screenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            pulseRing.Parameters["resolution"]?.SetValue(Utilities.CreatePixelationResolution(data.texture.Size() * data.scale));

            // Convert the position to screen UV coords.
            Vector2 screenBasedPosition = Projectile.Center - Main.screenPosition;
            Vector2 screenUVPosition = screenBasedPosition / new Vector2(Main.screenWidth, Main.screenHeight);
            pulseRing.Parameters["explosionCenterUV"]?.SetValue(screenUVPosition);
            pulseRing.Parameters["innerNoiseTexture"]?.SetValue(InnerNoiseTexure);

            Utilities.SetTexture1(BorderNoiseTexture);
            //Utilities.SetTexture2(InnerNoiseTexure);

            pulseRing.CurrentTechnique.Passes[0].Apply();
            data.Draw(Main.spriteBatch);
        }
        #endregion
    }
}
