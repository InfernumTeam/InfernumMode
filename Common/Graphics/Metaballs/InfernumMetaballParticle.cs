using Microsoft.Xna.Framework;

namespace InfernumMode.Common.Graphics.Metaballs
{
    /// <summary>
    /// Represents a base metaball particle for use by infernums metaballs.
    /// </summary>
    public sealed class InfernumMetaballParticle
    {
        public Vector2 Center;

        public Vector2 Velocity;

        public Vector2 Size;

        public float DecayRate;

        public InfernumMetaballParticle(Vector2 center, Vector2 velocity, Vector2 size, float decayRate = 0.997f)
        {
            Center = center;
            Velocity = velocity;
            Size = size;
            DecayRate = decayRate;
        }

        public void Update()
        {
            Size = Vector2.Clamp(Size - Vector2.One * 0.1f, Vector2.Zero, Vector2.One * 200f) * DecayRate;
            if (Size.Length() < 20f)
                Size = Size * DecayRate * 0.8f - Vector2.One;

            Center += Velocity;
        }
    }
}
