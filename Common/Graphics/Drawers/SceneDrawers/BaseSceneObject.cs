using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers
{
    public abstract class BaseSceneObject
    {
        public Vector2 Position;

        public Vector2 Velocity;

        public Vector2 Scale;

        public int Variant;

        public int Timer;

        public int Lifetime;

        public float Rotation;

        public float RotationSpeed;

        public float RandomSeed;

        public float Depth;

        public float LifetimeRatio => (float)Timer / Lifetime;

        public virtual bool ShouldKill => LifetimeRatio >= 1f;

        public BaseSceneObject(Vector2 position, Vector2 velocity, Vector2 scale, int lifetime, float depth, float rotation, float rotationSpeed)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Lifetime = lifetime;
            Rotation = rotation;
            RotationSpeed = rotationSpeed;
            RandomSeed = Main.rand.NextFloat();
            Depth = depth;
        }

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
