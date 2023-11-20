using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class RelicSparkle : RaritySparkle
    {
        public RelicSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.OrangeRed, Color.Red, Main.rand.NextFloat(0, 1f));
            Texture = RarityTextureRegistry.BaseRaritySparkleTexture;
            BaseFrame = null;
        }
    }
}
