using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class CreditSparkle : RaritySparkle
    {
        public readonly Color RandomFireColor;

        public float Opacity;

        public CreditSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = scale;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Black;
            RandomFireColor = Color.Lerp(Color.Gold, Color.OrangeRed, Main.rand.NextFloat());
            Texture = InfernumTextureRegistry.BloomCircle.Value;
            BaseFrame = null;
            Opacity = 0f;
        }

        public override bool CustomUpdate()
        {
            if (Time <= 6)
            {
                if (Opacity < 1f)
                    Opacity += 0.2f;
                DrawColor = Color.Lerp(Color.White, RandomFireColor, Time / 6);
            }
            else
                DrawColor = Color.Lerp(RandomFireColor, Color.Black, (float)(Time - 6) / (Lifetime - 6));

            return false;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            spriteBatch.Draw(InfernumTextureRegistry.BloomFlare.Value, drawPosition, null, DrawColor with { A = 0 } * 1.2f * Opacity, Rotation, InfernumTextureRegistry.BloomFlare.Value.Size() * 0.5f, Scale * 0.06f, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor with { A = 0 } * Opacity, Rotation, Texture.Size() * 0.5f, Scale * 0.2f, SpriteEffects.None, 0f);

            if (Time <= 3)
            {
                Texture2D texture = InfernumTextureRegistry.BigGreyscaleCircle.Value;
                spriteBatch.Draw(texture, drawPosition, null, Color.Black, Rotation, texture.Size() * 0.5f, Scale * 0.03f, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
