using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    public class GlowingEmber
    {
        public int Time;
        public int Lifetime;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 DistortScale;
        public Color DrawColor;

        public static Texture2D BloomTexture => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

        public float TimeLeft => Lifetime - Time;

        public GlowingEmber(Vector2 position, Vector2 velocity, Color drawColor, float rotation, float rotationSpeed, float maxScale, int lifetime)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = drawColor;
            Rotation = rotation;
            RotationSpeed = rotationSpeed;
            MaxScale = maxScale;
            Lifetime = lifetime;
            DistortScale = new(Main.rand.NextFloat(0.8f, 1.6f), Main.rand.NextFloat(0.8f, 1.6f));
        }

        public void Update()
        {
            Position += Velocity;

            // Grow rapidly
            if (Time <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, Time / 20f);

            // Shrink rapidly.
            if (TimeLeft <= 30)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)TimeLeft / 30f);

            // Increase the rotation and time.
            Rotation += RotationSpeed;
            Time++;
        }

        public void Draw()
        {
            Rectangle screen = new(-1000, -1000, 4000, 4000);
            if (screen.Contains((int)Position.X, (int)Position.Y))
            {
                Main.spriteBatch.Draw(BloomTexture, Position, null, Color.DarkMagenta with { A = 0 } * 0.9f, Rotation, BloomTexture.Size() * 0.5f, Scale * 0.15f * DistortScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(BloomTexture, Position, null, Color.Lerp(DrawColor, Color.DarkMagenta, 0.5f) with { A = 0 } * 0.9f, Rotation, BloomTexture.Size() * 0.5f, Scale * 0.075f * DistortScale, SpriteEffects.None, 0f);
            }
        }
    }
}
