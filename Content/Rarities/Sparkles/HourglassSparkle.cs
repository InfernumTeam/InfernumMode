using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.Sparkles
{
    public class HourglassSparkle : RaritySparkle
    {
        public readonly Texture2D SandTexture;

        public readonly Color SandColor;

        private readonly float DirectionMultiplier;

        public HourglassSparkle(int lifetime, float scale, float initialRotation, float rotationSpeed, Vector2 position, Vector2 velocity)
        {
            Lifetime = lifetime;
            Scale = 0;
            MaxScale = scale;
            Rotation = initialRotation;
            RotationSpeed = rotationSpeed;
            Position = position;
            Velocity = velocity;
            DrawColor = Color.Lerp(new Color(164, 51, 43), new Color(206, 101, 45), Main.rand.NextFloat(0, 1f));
            DrawColor.A = 0;
            SandColor = new(248, 216, 104) { A = 0 };
            Texture = ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/Hourglass").Value;
            SandTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/HourglassSand").Value;
            BaseFrame = null;
            DirectionMultiplier = Main.rand.NextBool().ToDirectionInt();
        }

        public override bool CustomUpdate()
        {
            float sine = (float)Math.Sin(Time * 0.07f) * DirectionMultiplier;
            Velocity.Y = sine * 0.15f;
            return true;
        }

        public override bool CustomDraw(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            spriteBatch.Draw(SandTexture, drawPosition, null, SandColor, Rotation, SandTexture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture, drawPosition, null, DrawColor, Rotation, Texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
