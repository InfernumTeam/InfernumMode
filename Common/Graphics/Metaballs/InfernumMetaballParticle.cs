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

        public float Timer;

        public InfernumMetaballParticle(Vector2 center, Vector2 velocity, Vector2 size, float decayRate = 0.985f)
        {
            Center = center;
            Velocity = velocity;
            Size = size;
            DecayRate = decayRate;
        }

        public void Update()
        {
            Size *= DecayRate;

            Center += Velocity;
            Timer++;
        }
    }
}
