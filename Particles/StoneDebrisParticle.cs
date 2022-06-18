using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Particles
{
    public class StoneDebrisParticle2 : Particle
    {
        public override string Texture => "CalamityMod/Particles/StoneDebris";
        public override bool SetLifetime => true;
        public override int FrameVariants => 5;

        private float Spin;
        private float opacity;
        Color originalColor;

        public StoneDebrisParticle2(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, float rotationSpeed = 0.2f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            originalColor = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Variant = Main.rand.Next(3);
        }

        public override void Update()
        {
            Color = originalColor;
            Velocity = Velocity * new Vector2(0.95f, 1f) + Vector2.UnitY * 0.28f;
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);

            if (Collision.SolidCollision(Position, 1, 1) && Time < Lifetime - 1 && Time > 8)
            {
                Main.PlaySound(SoundID.Item51, Position);
                Time = Lifetime - 1;
            }
        }
    }
}
