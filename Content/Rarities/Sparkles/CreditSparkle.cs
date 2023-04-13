using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class CreditSparkle : RaritySparkle
    {
        public Texture2D Bloom;
        public int Variant;

        public int Variant2;

        public int CurrentFrame;

        public const int MaxFrames = 6;

        public CreditSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.Yellow, Color.Orange, Main.rand.NextFloat());
            Texture = InfernumTextureRegistry.Gleam.Value;
            Variant = Main.rand.Next(0, 7);
            Variant2 = Main.rand.Next(0, 7);
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            // Draw fire to indicate a gunshot.
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor, Rotation, Texture.Size() * 0.5f, Scale * new Vector2(1f, 1.7f), SpriteEffects.None, 0f);
            return true;
        }
    }
}
