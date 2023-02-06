using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class PuritySparkle : RaritySparkle
    {
        public PuritySparkle(SparkleType type, int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Type = type;
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.LightBlue, Color.LightCyan, Main.rand.NextFloat(1f));
            Texture = InfernumTextureRegistry.EmpressStar.Value;
        }
    }
}
