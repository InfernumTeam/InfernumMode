using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles
{
    public class SnowflakeCinder : BaseCinderParticle
    {
        private Color? DrawColor;

        private readonly Color[] PossibleColors =
        [
            new(221, 254, 255),
            new(184, 237, 255),
            new(124, 147, 211)
        ];

        private float RotationPerFrame;

        public override int NumberOfFrames => 4;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/SnowflakeCinder";

        public override void Initialize()
        {
            DrawColor = PossibleColors[Main.rand.Next(PossibleColors.Length)];
            RotationPerFrame = Main.rand.NextFloat(0, 0.25f);
        }

        public override void Update()
        {
            Rotation += RotationPerFrame;
            base.Update();
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            if (DrawColor.HasValue)
            {
                Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
                Rectangle sourceRectangle = texture.Frame(1, NumberOfFrames, 0, Variant);
                Vector2 origin = texture.Size() * 0.5f;
                Main.spriteBatch.Draw(texture, Position - Main.screenPosition, sourceRectangle, DrawColor.Value, Rotation, origin, Scale * 0.5f, 0, 0);
            }
        }
    }
}
