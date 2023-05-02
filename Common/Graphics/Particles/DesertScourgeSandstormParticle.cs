using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.Common.Graphics.Particles
{
    public class DesertScourgeSandstormParticle : Particle
    {
        private float Spin;

        private float opacity;

        public Rectangle Frame;

        public override string Texture => "CalamityMod/Particles/SandyDust";

        public override bool UseHalfTransparency => false;

        public override bool UseCustomDraw => true;

        public override bool SetLifetime => true;

        public DesertScourgeSandstormParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, float rotationSpeed = 1f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Variant = Main.rand.Next(12);
            Frame = new Rectangle(Variant % 6 * 12, 12 + Variant / 6 * 12, 10, 10);
        }

        public override void Update()
        {
            opacity = MathF.Cos(MathHelper.PiOver2 * LifetimeCompletion);
            Velocity *= 0.99f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Scale *= 0.98f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D dustTexture = GeneralParticleHandler.GetTexture(Type);
            spriteBatch.Draw(dustTexture, Position - Main.screenPosition, (Rectangle?)Frame, Color * opacity, Rotation, Frame.Size() / 2f, Scale, 0, 0f);
        }
    }
}
