using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class HeartParticle : Particle
    {
        public Color StartingColor
        {
            get;
            set;
        }

        public Color EndingColor
        {
            get;
            set;
        }

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/HeartParticle";

        public HeartParticle(Vector2 relativePosition, Color startingColor, Color endingColor, Vector2 velocity, int lifetime, float rotation, float scale)
        {
            Position = relativePosition;
            Velocity = velocity;
            StartingColor = startingColor;
            EndingColor = endingColor;
            Scale = scale;
            Rotation = rotation;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Color = Color.Lerp(StartingColor, EndingColor, LifetimeCompletion);
            Color = Color.Lerp(Color, Color.Transparent, MathF.Pow(LifetimeCompletion, 2.6f));
            Velocity *= 0.94f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color, Rotation, texture.Size() * 0.5f, Scale, 0, 0f);
        }
    }
}
