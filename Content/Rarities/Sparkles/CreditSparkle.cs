using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class CreditSparkle : RaritySparkle
    {
        public readonly Color RandomFireColor;

        public CreditSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Black;
            Texture = InfernumTextureRegistry.BloomCircle.Value;
            BaseFrame = null;
        }

        public override bool CustomUpdate()
        {
            if (Time <= 3)
                DrawColor = Color.Lerp(Color.Black, Color.White, Time / 3);
            else if (Time <= 5)
                DrawColor = Color.Lerp(Color.White, RandomFireColor, (Time - 3) / 2);
            else
                DrawColor = Color.Lerp(RandomFireColor, Color.DarkGray, (Time - 5) / (Lifetime - 5));

            return true;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            float scale = Scale * Lerp(0.3f, 1f, Utils.GetLerpValue(0, 5, Time, true));
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor with { A = 0 }, Rotation, Texture.Size() * 0.5f, scale * 0.03f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
