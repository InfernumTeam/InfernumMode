using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class BookSparkle : RaritySparkle
    {
        private readonly float DirectionMultiplier;

        public BookSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(new Color(172, 68, 118), new Color(114, 92, 168), Main.rand.NextFloat());
            DrawColor.A = 0;
            Texture = RarityTextureRegistry.BookTexture;
            BaseFrame = null;
            DirectionMultiplier = Main.rand.NextBool().ToDirectionInt();
        }

        public override bool CustomUpdate()
        {
            float sine = Sin(Time * 0.03f) * DirectionMultiplier;
            Velocity.Y = sine * 0.15f;
            return true;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor, Rotation, Texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
