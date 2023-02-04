using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class ElectricCloudParticle : Particle
    {
        public float StartingScale
        {
            get;
            set;
        }

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/ElectricCloudParticle";

        public ElectricCloudParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale)
        {
            Position = relativePosition;
            Velocity = velocity;
            StartingScale = scale;
            Scale = 0.01f;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Velocity *= 0.987f;
            Scale = MathHelper.Lerp(Scale, StartingScale, 0.03f);
            Color = Color.Lerp(Color.Cyan, Color.BlueViolet, LifetimeCompletion);
            Color = Color.Lerp(Color, Color.Transparent, (float)Math.Pow((double)LifetimeCompletion, 3.0));
            Rotation += Velocity.X * 0.003f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float brightness = (float)Math.Pow((double)Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15) * 0.4f;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * brightness, Rotation, texture.Size() * 0.8f, Scale, 0, 0f);
        }
    }
}
