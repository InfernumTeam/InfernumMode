using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Credits
{
    public class CreditDeveloper(Texture2D texture, Vector2 position, Vector2 velocity, float rotation, SpriteEffects direction)
    {
        public Texture2D Texture = texture;

        public Vector2 Position = position;

        public Vector2 StartingPosition = position;

        public Vector2 Velocity = velocity;

        public float Rotation = rotation;

        public readonly SpriteEffects Direction = direction;

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
