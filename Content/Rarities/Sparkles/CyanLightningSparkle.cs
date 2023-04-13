using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class CyanLightningSparkle : RaritySparkle
    {
        public CyanLightningSparkle(int lifetime, float scale, float initialRotation, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0f;
            MaxScale = scale;
            Rotation = initialRotation;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(Color.Cyan, Color.Yellow, Utilities.UltrasmoothStep(Main.rand.NextFloat())) * 0.5f;
            Texture = InfernumTextureRegistry.Gleam.Value;
            MaxScale *= 0.4f;
        }
    }
}
