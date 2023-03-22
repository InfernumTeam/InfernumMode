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

        private bool BaseCredits;

        public CreditAnimationObject(Vector2 center, Vector2 velocity, Texture2D[] textures, bool baseCredits)
        {
            Center = center;
            Velocity = velocity;
            Textures = textures;
            BaseCredits = baseCredits;
        }

        public void DisposeTextures()
        {
            foreach (var texture in Textures)
            {
                if (!texture.IsDisposed)
                    texture.Dispose();
            }
        }

        public void Update()
        {
            Center += Velocity;
            Center.X = MathHelper.Clamp(Center.X, 0f, Main.screenWidth);
            Center.Y = MathHelper.Clamp(Center.Y, 0f, Main.screenHeight);
        }

        public void Draw(int textureIndex, float opacity)
        {
            if (BaseCredits)
                textureIndex = 0;
            else
                textureIndex = (int)MathHelper.Clamp(textureIndex, 0f, Textures.Length - 1);
            Texture2D texture = Textures[textureIndex];
            if (texture != null || !texture.IsDisposed)
                Main.spriteBatch.Draw(texture, Center, null, Color.White * opacity, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
        }
    }
}
