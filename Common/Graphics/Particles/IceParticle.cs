using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.Common.Graphics.Particles
{
    public class SnowyIceParticle : Particle
    {
        private float Spin;

        private float opacity;

        private Vector2 Gravity;

        public Rectangle Frame;

        public override bool UseHalfTransparency => false;

        public override bool UseCustomDraw => true;

        public override bool SetLifetime => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/SnowyIce";

        public SnowyIceParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifetime, float rotationspeed = 1f, Vector2? gravity = null)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationspeed;
            Gravity = (gravity ?? new Vector2?(Vector2.Zero)).Value;
            Variant = Main.rand.Next(12);
            Frame = new Rectangle(Variant % 6 * 12, 12 + Variant / 6 * 12, 10, 10);
        }

        public override void Update()
        {
            Velocity += Gravity;
            opacity = MathF.Cos(MathHelper.PiOver2 * LifetimeCompletion);
            Velocity *= 0.95f;
            Rotation += Spin * (Velocity.X > 0f ? 1f : -1f);
            Scale *= 0.98f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = GeneralParticleHandler.GetTexture(Type);
            spriteBatch.Draw(texture, Position - Main.screenPosition, Frame, Color * opacity, Rotation, Frame.Size() / 2f, Scale, 0, 0f);
        }
    }
}
