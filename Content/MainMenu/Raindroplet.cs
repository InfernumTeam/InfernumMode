using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    public class Raindroplet
    {
        public int Time;
        public int Lifetime;
        public int Variant;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
        public float Depth;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color DrawColor;
        public Texture2D Texture;
        public Rectangle BaseFrame;

        public float TimeLeft => Lifetime - Time;

        public const int FrameWidth = 4;
        public const int FrameHeight = 42;

        public const int MaxFrames = 3;

        public Raindroplet(int lifetime, float scale, float initialRotation, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Rotation = initialRotation;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.White;
            Texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/VanillaReplacements/RainAstral").Value;
            Depth = Main.rand.NextFloat(1.3f, 3f);
            Variant = Main.rand.Next(0, MaxFrames);
            BaseFrame = new(FrameWidth * Variant, 0, 2, FrameHeight);
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
                Vector2 origin = BaseFrame.Size() * 0.5f;
                float opacity = 0.5f;
                Main.spriteBatch.Draw(Texture, Position, BaseFrame, DrawColor with { A = 50 } * opacity, Velocity.ToRotation() + MathHelper.PiOver2, origin, Scale * new Vector2(0.5f, 1.4f), SpriteEffects.None, 0f);
            }
        }
    }
}
