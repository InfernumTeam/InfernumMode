using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class EggSparkle : RaritySparkle
    {
        public EggSparkle(SparkleType type, int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Type = type;
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.Yellow, Color.Gold, Main.rand.NextFloat(0, 1f));
            Texture = InfernumVassalRarity.DropletTexture;

            BaseFrame = null;
        }
    }
}
