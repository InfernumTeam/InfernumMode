using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Particles
{
    public class BloodSplashParticle : Particle
    {
        public float Gravity;

        public Color InitialColor;

        public override bool SetLifetime => true;
        public override bool UseCustomDraw => true;
        public override bool UseAdditiveBlend => true;

        public override string Texture => "CalamityMod/Particles/Blood2";

        public BloodSplashParticle(Vector2 relativePosition, Vector2 velocity, int lifetime, float scale, Color color, float gravity = 0f)
        {
            Position = relativePosition;
            Velocity = velocity;
            Scale = scale;
            Lifetime = lifetime;
            Color = InitialColor = color;
            Gravity = gravity;
            Rotation = Velocity.ToRotation();
        }

        public override void Update()
        {
            Velocity.X *= 0.98f;
            Velocity.Y += Gravity;
            Color = Color.Lerp(InitialColor, Color.Transparent, Pow(LifetimeCompletion, 4f));
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            float brightness = Pow(Lighting.Brightness((int)(Position.X / 16f), (int)(Position.Y / 16f)), 0.15f);
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, 3, 0, (int)(LifetimeCompletion * 3f));
            Vector2 origin = frame.Size() * 0.5f;

            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * brightness, Rotation, origin, Scale, 0, 0f);
        }
    }
}
