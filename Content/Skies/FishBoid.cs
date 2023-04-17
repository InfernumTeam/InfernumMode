using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class FishBoid
    {
        public int Time;
        public int Lifetime;
        public float MaxScale;
        public float Scale;
        public float Rotation;
        public float Depth;
        public int CurrentFrame;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Acceleration;
        public Color DrawColor;

        public float SeperateCoeffecient = 0.05f;
        public float AlignCoeffecient = 0.05f;
        public float ClumpCoeffecient = 0.0005f;
        public float Radius = 100f;
        public float SeperateDistanceScalar = 42f;
        public float SchoolSize = 10f;

        public float MaxSpeed = 1f;

        public float TimeLeft => Lifetime - Time;

        public const int FrameCount = 6;

        public const int FrameWidth = 38;

        public const int FrameHeight = 28;

        public FishBoid(int lifetime, float scale, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.Black, Color.DarkSlateBlue, Main.rand.NextFloat());
            Depth = Main.rand.Next(1, 4);
        }

        public void Update()
        {
            // Grow rapidly
            if (Time <= 120)
                Scale = MathHelper.Lerp(0f, MaxScale, Time / 60f);

            // Shrink rapidly.
            if (TimeLeft <= 120)
                Scale = MathHelper.Lerp(0f, MaxScale, (float)TimeLeft / 60f);

            // Choose a direction.

            // Increase the rotation and time.
            Rotation = Velocity.ToRotation() ;
            //WrapAround();
            DoBehavior();

            if (Time % 8 == 0)
                CurrentFrame = (CurrentFrame + 1) % FrameCount;

            Velocity += Acceleration;
            Velocity = Velocity.ClampMagnitude(0f, MaxSpeed);
            Position += Velocity;
            Time++;
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
            int posInList = 1;

            // Loop through every boid. This could be optimised potentially?
            for (int i = 0; i < FlowerOceanSky.Fishes.Count; i++)
            {
                FishBoid otherBoid = FlowerOceanSky.Fishes[i];
                if (otherBoid == this)
                    posInList = i;
                // Get the distance.
                float distance = Vector2.Distance(Position, otherBoid.Position);
                if (otherBoid != this && distance < Radius && Depth == otherBoid.Depth)
                {
                    // Get the direction to avoid and add it to the overall direction.
                    if (distance < Radius * 0.3f)
                    {
                        float avoidFactor = Utils.GetLerpValue(Radius * 0.3f, 0f, Vector2.Distance(Position, otherBoid.Position), true);
                        separation += (Position - otherBoid.Position) * avoidFactor;
                    }
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
                float clockCenterMoveInterpolant = Utils.GetLerpValue(0f, 40f, Vector2.Distance(Position, coheshion), true);
                Velocity += (coheshion - Position ) * clockCenterMoveInterpolant * ClumpCoeffecient;

                separation /= total;
                Velocity += separation * SeperateCoeffecient;

                alignment /= total;
                Velocity += (alignment - Velocity) * AlignCoeffecient;
            }
            // Swim around idly.
            Velocity = Velocity.RotatedBy(MathHelper.Pi * (posInList % 2f == 0f).ToDirectionInt() * 0.002f);
        }

        public void Draw()
        {
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle screen = new(-1000, -1000, 4000, 4000);
            SpriteEffects effects = (Velocity.X > 0f).ToDirectionInt() == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            //float depthScalar = MathHelper.Lerp(1f, 3f, Depth / 3f);
            Vector2 scale = Vector2.One / Depth;
            Vector2 position = (Position - screenCenter) * scale + screenCenter - Main.screenPosition;
            float rotation = (Velocity.X > 0f).ToDirectionInt() == -1 ? Rotation += MathHelper.Pi : Rotation;
            if (screen.Contains((int)position.X, (int)position.Y))
            {
                Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/DepthFeeder").Value;
                Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/DepthFeederGlow").Value;
                Rectangle frame = new(0, FrameHeight * CurrentFrame, FrameWidth, FrameHeight);
                Vector2 origin = frame.Size() * 0.5f;
                float opacityScalar = MathHelper.Lerp(0.4f, 0.7f, (Depth - 1) / 2f);
                Main.spriteBatch.Draw(texture, position, frame, DrawColor * opacityScalar, rotation, origin, scale* Scale, effects, 0f);
                Main.spriteBatch.Draw(glowmask, position, frame, Color.White * 1.3f * opacityScalar, rotation, origin, scale * Scale, effects, 0f);

            }
        }
    }
}
