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

        private readonly bool BaseCredits;

        public CreditAnimationObject(Vector2 center, Vector2 velocity, Texture2D[] textures, bool baseCredits)
        {
            Center = center;
            Velocity = velocity;
            Textures = textures;
            BaseCredits = baseCredits;
        }

        public void DisposeTextures()
        {
            if (Textures is null || BaseCredits)
                return;

            foreach (var texture in Textures)
            {
                if (!texture?.IsDisposed ?? false)
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
            else if (textureIndex >= Textures.Length)
                textureIndex = Textures.Length - 1;
            Texture2D texture = Textures[textureIndex];
            if (texture != null && !texture.IsDisposed)
            {
                Vector2 scale = Vector2.One;
                if (texture.Width >= 640)
                    scale *= 640f / texture.Width;

                Main.spriteBatch.Draw(texture, Center, null, Color.White * opacity, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
