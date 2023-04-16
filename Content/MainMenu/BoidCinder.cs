using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.MainMenu
{
    public class BoidCinder
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
        public Vector2 Acceleration;
        public Color DrawColor;
        public Texture2D Texture;

        public float SeperateCoeffecient = 0.51f;
        public float AlignCoeffecient = 0.45f;
        public float ClumpCoeffecient = 0.55f;
        public float Radius = 50f;
        public float SeperateDistanceScalar = 42f;
        public float SchoolSize = 10f;

        public float MaxSpeed => 4f;

        public float TimeLeft => Lifetime - Time;

        public BoidCinder(int lifetime, float scale, float initialRotation, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Rotation = initialRotation;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.IndianRed, Color.Orange, Main.rand.NextFloat(0.9f));
            Texture = InfernumTextureRegistry.Gleam.Value;
            Depth = 1f;
        }

        public void Update()
        {
            // Grow rapidly
            if (Time <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, Time / 20f);

            // Shrink rapidly.
            if (TimeLeft <= 20)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)TimeLeft / 20f);

            // Increase the rotation and time.
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;//+= RotationSpeed;
            WrapAround();
            DoBehavior();

            Velocity += Acceleration;
            Velocity = Velocity.ClampMagnitude(0f, MaxSpeed);
            Position += Velocity;
            Time++;
            Acceleration = Vector2.Zero;
        }

        private void WrapAround()
        {
            Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
            if (Position.X < screen.Left)
                Position.X = screen.Right;
            else if (Position.X > screen.Right)
                Position.X = screen.Left;

            if (Position.Y < screen.Top)
                Position.Y = screen.Bottom;
            else if (Position.Y > screen.Bottom)
                Position.Y = screen.Top;
        }

        private void DoBehavior()
        {
            Vector2 separation = Vector2.Zero;
            Vector2 coheshion = Vector2.Zero;
            Vector2 alignment = Vector2.Zero;
            int total = 0;

            // Loop through every boid. This could be optimised potentially?
            foreach (var otherBoid in InfernumMainMenu.Boids)
            {
                // Get the distance.
                float distance = Vector2.Distance(Position, otherBoid.Position);
                if (otherBoid != this && distance < Radius)
                {
                    // Get the direction to avoid and add it to the overall direction.
                    separation -= (otherBoid.Position - Position) * (SeperateDistanceScalar / (Position - otherBoid.Position).Length());
                    coheshion += otherBoid.Position;
                    alignment += otherBoid.Velocity;
                    total++;
                }
            }

            if (total == 0)
                return;
            //else if (total >= SchoolSize)
            //{
            //    coheshion /= total;
            //    Velocity -= (coheshion - Position) * ClumpCoeffecient * 1.2f;
            //}
            else
            {

                coheshion /= total;
                Velocity += (coheshion - Position) * ClumpCoeffecient;

                separation /= total;
                Velocity += separation * SeperateCoeffecient;

                alignment /= total;
                Velocity += (alignment - Velocity) * AlignCoeffecient;
            }
        }       

        public void Draw()
        {
            Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle screen = new(-1000, -1000, 4000, 4000);
            Vector2 scale = new(1f / Depth, 1.5f / Depth);
            Vector2 position = (Position - screenCenter) * scale + screenCenter;
            if (screen.Contains((int)position.X, (int)position.Y))
            {
                Vector2 origin = Texture.Size() * 0.5f;
                Main.spriteBatch.Draw(Texture, position, null, DrawColor with { A = 0 }, Rotation, origin, scale * Scale, 0, 0f);
                Main.spriteBatch.Draw(Texture, position, null, DrawColor with { A = 0 }, Rotation + MathHelper.PiOver2, origin, scale * Scale, 0, 0f);
                Main.spriteBatch.Draw(Texture, position, null, DrawColor with { A = 0 }, Rotation + MathHelper.Pi, origin, scale * Scale, 0, 0f);
                Main.spriteBatch.Draw(Texture, position, null, DrawColor with { A = 0 }, Rotation - MathHelper.PiOver2, origin, scale * Scale, 0, 0f);

            }
        }
    }
}
