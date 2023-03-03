using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class CloudParticle : Particle
    {
        public bool IsImportant
        {
            get;
            set;
        }

        public float StartingScale
        {
            get;
            set;
        }

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

        public override bool Important => IsImportant;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/CloudParticle";

        public CloudParticle(Vector2 relativePosition, Vector2 velocity, Color startingColor, Color endingColor, int lifetime, float scale, bool isImportant = false)
        {
            Position = relativePosition;
            Velocity = velocity;
            StartingScale = scale;
            StartingColor = startingColor;
            EndingColor = endingColor;
            Scale = 0.01f;
            Lifetime = lifetime;
            IsImportant = isImportant;
        }

        public override void Update()
        {
            Velocity *= 0.987f;
            Scale = MathHelper.Lerp(Scale, StartingScale, 0.03f);
            Color = Color.Lerp(StartingColor, EndingColor, LifetimeCompletion);
            Color = Color.Lerp(Color, Color.Transparent, (float)Math.Pow((double)LifetimeCompletion, 3.0));
            Rotation += Velocity.X * 0.008f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float brightness = (float)Math.Pow((double)Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15) * 0.9f;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * brightness, Rotation, texture.Size() * 0.5f, Scale, 0, 0f);
        }
    }
}
