using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.Credits
{
    public class CreditAnimationObject
    {
        public Vector2 Center;

        public Vector2 Velocity;

        public Texture2D[] Textures;

        public CreditAnimationObject(Vector2 center, Vector2 velocity, Texture2D[] textures)
        {
            Center = center;
            Velocity = velocity;
            Textures = textures;
        }

        public void DisposeTextures()
        {
            foreach (var texture in Textures)
                texture.Dispose();
        }

        public void Update()
        {
            Center += Velocity;
        }

        public void Draw(int textureIndex, float opacity)
        {
            textureIndex = (int)MathHelper.Clamp(textureIndex, 0f, Textures.Length - 1);
            Texture2D texture = Textures[textureIndex];
            Main.spriteBatch.Draw(texture, Center, null, Color.White * opacity, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        }
    }
}
