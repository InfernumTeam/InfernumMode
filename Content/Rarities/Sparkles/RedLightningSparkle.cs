using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class RedLightningSparkle : RaritySparkle
    {
        public RedLightningSparkle(int lifetime, float scale, float initialRotation, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Rotation = initialRotation;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.IndianRed, Color.Orange, Main.rand.NextFloat(0.9f));
            Texture = InfernumTextureRegistry.Gleam.Value;
            MaxScale *= 0.4f;
        }
    }
}
