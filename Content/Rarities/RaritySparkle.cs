using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.Content.Rarities
{
    public class RaritySparkle
    {
        public SparkleType Type;
        public int Time;
        public int Lifetime;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color DrawColor;
        public Texture2D Texture;
        public Rectangle? BaseFrame;

        public float TimeLeft => Lifetime - Time;

        public RaritySparkle(SparkleType type, int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity, Color drawColor, Texture2D texture, Rectangle? frame)
        {
            Type = type;
            Lifetime = lifetime;
            MaxScale = scale;
            Scale = 0;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = drawColor;
            Texture = texture;
            BaseFrame = frame;
        }

        public void Update()
        {
            Position += Velocity;

            // Grow rapidly
            if (Time <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)Time / 20f);

            // Shrink rapidly.
            if (TimeLeft <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)TimeLeft / 20f);

            // Increase the rotation and time.
            Rotation += RotationSpeed;
            Time++;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position)
        {
            Rectangle? frame = null;
            if (BaseFrame.HasValue)
            {
                int animationFrame = (int)Math.Floor(Time / ((float)Lifetime / 6));
                frame = new Rectangle(0, BaseFrame.Value.Y * animationFrame, BaseFrame.Value.Width, BaseFrame.Value.Height);
            }
            spriteBatch.Draw(Texture, position, frame, DrawColor, Rotation, !frame.HasValue ? Texture.Size() * 0.5f : frame.Value.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }
}
