using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class BubbleSparkle : RaritySparkle
    {
        public BubbleSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            if (Main.rand.NextBool())
            {
                DrawColor = Color.White;
                Texture = InfernumOceanFlowerRarity.BubbleTexture;
            }
            else
            {
                DrawColor = Color.Lerp(Color.Teal, Color.SkyBlue, Main.rand.NextFloat(1f));
                Texture = RarityTextureRegistry.BaseRaritySparkleTexture;
                MaxScale = scale * 1.5f;
            }
            BaseFrame = null;
        }
    }
}
