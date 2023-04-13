using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.AttemptRecording;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Credits
{
    public class CreditAnimationObject
    {
        public Vector2 Center;

        public Vector2 Velocity;

        public Texture2D[] Textures;

        public string Header;

        public string Names;

        public Color HeaderColor;

        private readonly bool BaseCredits;

        public Vector2 TextCenter => Center + new Vector2(0f, 230f);

        public CreditAnimationObject(Vector2 center, Vector2 velocity, Texture2D[] textures, string header, string names, Color headerColor, bool baseCredits)
        {
            Center = center;
            Velocity = velocity;
            Textures = textures;
            Header = header;
            Names = names;
            HeaderColor = headerColor;
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


        public void DrawGIF(int textureIndex, float opacity)
        {
            float noiseIntensity = 0.4f;
            if (BaseCredits)
            {
                textureIndex = 0;
                noiseIntensity = 0.2f;
            }
            else if (textureIndex >= Textures.Length)
                textureIndex = Textures.Length - 1;

            Effect creditEffect = InfernumEffectsRegistry.CreditShader.GetShader().Shader;
            creditEffect.Parameters["lerpColor"].SetValue(Color.SandyBrown.ToVector3());
            creditEffect.Parameters["lerpColorAmount"].SetValue(0.5f);
            creditEffect.Parameters["noiseScale"].SetValue(3f);
            creditEffect.Parameters["noiseIntensity"].SetValue(noiseIntensity);
            creditEffect.Parameters["overallOpacity"].SetValue(opacity);
            creditEffect.CurrentTechnique.Passes["CreditPass"].Apply();

            Texture2D texture = Textures[textureIndex];

            if (texture != null && !texture.IsDisposed)
            {
                Vector2 scale = Vector2.One * ScreenCapturer.DownscaleFactor;
                if (texture.Height >= 120f)
                    scale *= 120f / texture.Height;

                Main.spriteBatch.Draw(texture, Center, null, Color.White, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        public void DrawNames(float opacity)
        {
            IEnumerable<string> cutUpString = Names.Split("\n").Prepend(Header);
            float stringHeight = FontAssets.MouseText.Value.MeasureString(cutUpString.ElementAt(0)).Y;
            for (int i = 0; i < cutUpString.Count(); i++)
            {
                float stringWidth = FontAssets.MouseText.Value.MeasureString(cutUpString.ElementAt(i)).X * 0.5f;
                Vector2 drawPos = TextCenter + new Vector2(-stringWidth * 0.5f, stringHeight * i);
                Color textColor = Color.White;
                Vector2 scale = Vector2.One;

                if (i is 0)
                {
                    textColor = HeaderColor;
                    scale *= 1.2f;
                }

                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, cutUpString.ElementAt(i), drawPos, textColor * opacity, 0f, 
                    new Vector2(stringWidth * 0.5f, stringHeight * 0.5f), scale);
            }
        }
    }
}
