using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class PinkSparkle : RaritySparkle
    {
        public Texture2D Bloom;
        public int Variant;

        public int Variant2;

        public int CurrentFrame;

        public const int MaxFrames = 6;

        public PinkSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.Pink, Color.LightPink, Main.rand.NextFloat());
            Texture = InfernumRarityHelper.SparkleTexure;
            Variant = Main.rand.Next(0, 7);
            Variant2 = Main.rand.Next(0, 7);
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor with { A = 0 }, Rotation, Texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            return true;
        }
    }
}
