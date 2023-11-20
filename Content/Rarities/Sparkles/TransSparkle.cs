using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class TransSparkle : RaritySparkle
    {

        public TransSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.DeepPink, Color.Cyan, Main.rand.NextFloat());
            DrawColor.A = 0;
            Texture = RarityTextureRegistry.BaseRaritySparkleTexture;
            BaseFrame = null;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor with { A = 0 }, Rotation, Texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
