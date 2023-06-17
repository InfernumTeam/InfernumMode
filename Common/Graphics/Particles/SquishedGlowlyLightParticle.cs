using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class SquishedGlowlyLightParticle : Particle
    {
        private float Opacity;

        private readonly bool IsNegative;

        private readonly float OriginalOpacity;

        private readonly float SquishFactor;

        private readonly Color BloomColor;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override string Texture
        {
            get
            {
                if (IsNegative)
                    return "InfernumMode/Common/Graphics/Particles/GlowyLightParticleNegative";

                return "InfernumMode/Common/Graphics/Particles/GlowyLightParticle2";
            }
        }

        public SquishedGlowlyLightParticle(Vector2 position, Vector2 velocity, Color color, Color bloomColor, int lifetime, float scale, float squishFactor, float opacity, bool isNegative)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            BloomColor = bloomColor;
            Scale = scale;
            Lifetime = lifetime;
            OriginalOpacity = Opacity = opacity;
            SquishFactor = squishFactor;
            IsNegative = isNegative;
        }

        public override void Update()
        {
            Opacity = Utils.GetLerpValue(0f, 18f, Time, true) * OriginalOpacity;
            if (Time <= 18f)
                Scale *= 0.85f;
            Rotation = Velocity.ToRotation() + PiOver2;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Particles/GlowyLightParticle2").Value;
            Vector2 drawPosition = Position - Main.screenPosition;
            Vector2 scale = new(MathF.Max(Scale - Scale * SquishFactor * 0.3f, 0.03f), Scale * SquishFactor);
            Vector2 scaleBloom = scale * new Vector2(2f, 1f);

            spriteBatch.Draw(bloomTexture, drawPosition, null, BloomColor * Opacity, Rotation, texture.Size() * 0.5f, scaleBloom, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, drawPosition, null, Color * Opacity, Rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
