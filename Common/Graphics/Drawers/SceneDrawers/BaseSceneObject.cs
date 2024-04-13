using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers
{
    public abstract class BaseSceneObject(Vector2 position, Vector2 velocity, Vector2 scale, int lifetime, float depth, float rotation, float rotationSpeed)
    {
        public Vector2 Position = position;

        public Vector2 Velocity = velocity;

        public Vector2 Scale = scale;

        public int Variant;

        public int Timer;

        public int Lifetime = lifetime;

        public float Rotation = rotation;

        public float RotationSpeed = rotationSpeed;

        public float RandomSeed = Main.rand.NextFloat();

        public float Depth = depth;

        public float LifetimeRatio => (float)Timer / Lifetime;

        public virtual bool ShouldKill => LifetimeRatio >= 1f;

        public void Update()
        {
            Position += Velocity;
            Rotation += RotationSpeed;

            ExtraUpdate();
            Timer++;
        }

        public virtual void ExtraUpdate()
        {

        }

        public abstract void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, Vector2 screenCenter, Vector2 scale);
    }
}
