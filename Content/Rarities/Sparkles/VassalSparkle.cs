using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class VassalSparkle : RaritySparkle
    {
        public VassalSparkle(SparkleType type, int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Type = type;
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.CadetBlue, Color.LightBlue, Main.rand.NextFloat(0, 1f));
            Texture = InfernumVassalRarity.DropletTexture;

            BaseFrame = null;
        }
    }
}
