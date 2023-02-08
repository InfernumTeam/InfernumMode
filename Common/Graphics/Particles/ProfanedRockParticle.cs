using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Common.Graphics.Particles
{
    public class ProfanedRockParticle : Particle
    {
        public Color OriginalColor;

        public float RotationSpeed;

        public bool Gravity;

        public override int FrameVariants => 6;

        public override bool SetLifetime => true;

        public override string Texture => "InfernumMode/Common/Graphics/Particles/ProfanedRockParticle";

        public ProfanedRockParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, float rotationSpeed = 0.2f, bool gravity = true)
        {
            Position = position;
            Velocity = velocity;
            Color = OriginalColor = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi); ;
            RotationSpeed = rotationSpeed;
            Variant = Main.rand.Next(6);
            Gravity = gravity;
        }

        public override void Update()
        {
            Velocity *= 0.99f;
            if (Gravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.1f;
            }

            Rotation += RotationSpeed * (Velocity.X > 0 ? 1f : -1f);

            if (LifetimeCompletion >= 0.8f)
            {
                Color = OriginalColor * (1 - ((LifetimeCompletion - 0.8f) / 0.2f));
            }
        }
    }
}
