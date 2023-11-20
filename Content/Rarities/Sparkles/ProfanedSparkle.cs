using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class ProfanedSparkle : RaritySparkle
    {
        public ProfanedSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Main.rand.NextBool() ? new Color(255, 255, 150) : new Color(255, 191, 73);
            if (Main.rand.NextBool())
            {
                Texture = ModContent.Request<Texture2D>("CalamityMod/Particles/CritSpark").Value;
                BaseFrame = new(0, 0, 16, 16);
                MaxScale *= 0.65f;
            }
            else
            {
                Texture = RarityTextureRegistry.BaseRaritySparkleTexture;
                BaseFrame = null;
            }
        }
    }
}
