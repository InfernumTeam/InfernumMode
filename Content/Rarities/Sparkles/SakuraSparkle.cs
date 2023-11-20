using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class SakuraSparkle : RaritySparkle
    {
        public int Varient;

        public const int MaxVarients = 3;
        public const int FrameWidth = 8;
        public const int FrameHeight = 12;

        public SakuraSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            UseSingleFrame = true;
            DrawColor = Color.White;
            Texture = RarityTextureRegistry.SakuraTexture;
            Varient = Main.rand.Next(0, MaxVarients);
            BaseFrame = new(0, FrameHeight * Varient, FrameWidth, FrameHeight);
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            // Draw backglow.
            float afterimageAmount = 5f;
            for (int i = 0; i < afterimageAmount; i++)
            {
                Color afterColor = Color.White * 0.3f;

                Vector2 afterDrawPos = drawPosition + (TwoPi * i / afterimageAmount).ToRotationVector2() * 1f;
                spriteBatch.Draw(Texture, afterDrawPos, BaseFrame, afterColor with { A = 0 }, Rotation, BaseFrame.Value.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(Texture, drawPosition, BaseFrame, DrawColor * 0.5f, Rotation, BaseFrame.Value.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
