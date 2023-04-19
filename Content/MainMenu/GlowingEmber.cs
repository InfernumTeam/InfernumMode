using InfernumMode.Assets.ExtraTextures;
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
        public Texture2D Texture;

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
            DistortScale = new(Main.rand.NextFloat(0.8f, 2.2f), Main.rand.NextFloat(0.8f, 2.2f));
            Texture = InfernumTextureRegistry.BigGreyscaleCircle.Value;
        }

        public void Update()
        {
            Position += Velocity;

            // Grow rapidly
            if (Time <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, Time / 20f);

            // Shrink rapidly.
            if (TimeLeft <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)TimeLeft / 20f);

            // Increase the rotation and time.
            Rotation += RotationSpeed;
            Time++;
        }

        public void Draw()
        {
            Rectangle screen = new(-1000, -1000, 4000, 4000);
            if (screen.Contains((int)Position.X, (int)Position.Y))
            {
                Main.spriteBatch.Draw(BloomTexture, Position, null, Color.DarkMagenta * 0.7f, Rotation, BloomTexture.Size() * 0.5f, Scale * 0.2f * DistortScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(Texture, Position, null, DrawColor, Rotation, Texture.Size() * 0.5f, Scale * 0.016f * DistortScale, SpriteEffects.None, 0f);
            }
        }
    }
}
