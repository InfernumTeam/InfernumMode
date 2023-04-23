using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Credits
{
    public class CreditDeveloper
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public readonly SpriteEffects Direction;

        public CreditDeveloper(Texture2D texture, Vector2 position, Vector2 velocity, float rotation, SpriteEffects direction)
        {
            Texture = texture;
            Position = position;
            Velocity = velocity;
            Rotation = rotation;
            Direction = direction;
        }

        public void Update()
        {
            Position += Velocity;
        }

        public void Draw(float opacity, float scale)
        {
            Main.spriteBatch.Draw(Texture, Position, null, Color.White * opacity, Rotation, Texture.Size() * 0.5f, scale * 0.7f, Direction, 0f);
        }
    }
}
