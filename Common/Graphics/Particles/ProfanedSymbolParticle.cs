using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    internal class ProfanedSymbolParticle : Particle
    {
        public float MaxScale;

        public float Opacity;

        public override bool SetLifetime => true;

        public override bool UseAdditiveBlend => false;

        public override bool UseCustomDraw => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/ProfanedSymbolParticle";

        public ProfanedSymbolParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            MaxScale = scale;
            Lifetime = lifeTime;
            Rotation = 0f;
        }

        public override void Update()
        {
            if (Time <= 10f)
                Opacity = MathHelper.Clamp(Opacity + 0.1f, 0f, 1f);
            else if (Time >= Lifetime - 10f)
                Opacity = MathHelper.Clamp(Opacity - 0.1f, 0f, 1f);

            // Rapidly affect the scale.
            float scaleSine = (1f + MathF.Sin(Time * 0.25f)) / 2f;

            Scale = MathHelper.Lerp(MaxScale * 0.85f, MaxScale, scaleSine);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color with { A = 0 } * Opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }
}
