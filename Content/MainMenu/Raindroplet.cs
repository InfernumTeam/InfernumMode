using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.MainMenu
{
    public class Raindroplet
    {
        public int Time;
        public int Lifetime;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
        public float Depth;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color DrawColor;
        public Texture2D Texture;
        public Rectangle? BaseFrame;

        public float TimeLeft => Lifetime - Time;

        public Raindroplet(int lifetime, float scale, float initialRotation, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Rotation = initialRotation;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.AliceBlue, Color.CornflowerBlue, Main.rand.NextFloat(0.9f));
            Texture = InfernumVassalRarity.DropletTexture;
            Depth = Main.rand.NextFloat(1.3f, 3f);
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
            Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle screen = new(-1000, -1000, 4000, 4000);
            Vector2 scale = new(0.66f / Depth, 2.5f / Depth);
            Vector2 position = (Position - screenCenter) * scale + screenCenter;
            if (screen.Contains((int)position.X, (int)position.Y))
            {
                Vector2 origin = Texture.Size() * 0.5f;
                Main.spriteBatch.Draw(Texture, position, null, DrawColor with { A = 0 }, 0f, origin, scale * Scale, 0, 0f);
            }
        }
    }
}
