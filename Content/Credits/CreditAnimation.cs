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

        public bool SwapSides;

        private readonly bool BaseCredits;

        public Vector2 TextCenter => new(Main.screenWidth * (SwapSides ? 0.35f : 0.65f), Center.Y);

        public CreditAnimationObject(Vector2 velocity, Texture2D[] textures, string header, string names, Color headerColor, bool swapSides, bool baseCredits)
        {
            Center = new(Main.screenWidth * (swapSides ? 0.7f : 0.3f), Main.screenHeight * 0.5f);
            Velocity = velocity;
            Textures = textures;
            Header = header;
            Names = names;
            HeaderColor = headerColor;
            SwapSides = swapSides;
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
            Center.X = Clamp(Center.X, 0f, Main.screenWidth);
            Center.Y = Clamp(Center.Y, 0f, Main.screenHeight);
        }


        public void DrawGIF(int textureIndex, float opacity)
        {
            // Get out if there are no textures loaded.
            if (Textures == null)
                return;

            // Get the noise intensity.
            float noiseIntensity = 0.1f;

            // If the base credits are in use, a single image is used and the noise intensity is lowered as a result.
            if (BaseCredits)
            {
                textureIndex = 0;
                noiseIntensity = 0.05f;
            }

            // Ensure the texture index remains in bounds.
            else if (textureIndex >= Textures.Length)
                textureIndex = Textures.Length - 1;

            // Setup the credits shader parameters. This gives them an ol' timey look.
            Effect creditEffect = InfernumEffectsRegistry.CreditShader.GetShader().Shader;
            creditEffect.Parameters["lerpColor"].SetValue(Color.SandyBrown.ToVector3());
            creditEffect.Parameters["lerpColorAmount"].SetValue(0.3f);
            creditEffect.Parameters["noiseScale"].SetValue(3f);
            creditEffect.Parameters["noiseIntensity"].SetValue(noiseIntensity);
            creditEffect.Parameters["overallOpacity"].SetValue(opacity);
            creditEffect.Parameters["justCrop"].SetValue(false);
            creditEffect.CurrentTechnique.Passes["CreditPass"].Apply();

            // Get the current frame from the textures.
            Texture2D texture = Textures[textureIndex];

            // Ensure that is is valid and non disposed before trying to draw to avoid errors. This is because they are disposed after use due to the sheer amount of them.
            if (texture != null && !texture.IsDisposed)
            {
                // Get the scale.
                Vector2 scale = Vector2.One * ScreenCapturer.DownscaleFactor;
                if (texture.Height >= 120f)
                    scale *= 120f / texture.Height;

                // Draw the frame.
                Main.spriteBatch.Draw(texture, Center, null, Color.White, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        public void DrawNames(float opacity)
        {
            // Split up the names into new lines per name.
            IEnumerable<string> cutUpString = Names.Split("\n").Prepend(Header);

            // Get the height. This is done for the first one only, so the gap inbetween each name is the same.
            float stringHeight = FontAssets.DeathText.Value.MeasureString(cutUpString.ElementAt(0)).Y;

            for (int i = 0; i < cutUpString.Count(); i++)
            {
                // Get the draw position using the text center, and offseting the Y position based on the number of names to center it. The latter half calculates the distance travelled by velocity.
                Vector2 drawPos = new(TextCenter.X, Main.screenHeight * (0.5f - 0.02f * cutUpString.Count()) - (Main.screenHeight * 0.5f - TextCenter.Y));
                // Lower the text based on its position in the name list.
                drawPos.Y += stringHeight * 0.9f * i;

                Color textColor = Color.White;

                // The backglow alternates throughout every color.
                float hue = Main.GlobalTimeWrappedHourly * 0.02f % 1f;
                if (hue < 0f)
                    hue += 1f;

                Color backglowColor = Main.hslToRgb(hue, 1f, 0.5f);
                Vector2 scale = Vector2.One * 0.8f;

                // Properly center the text.
                Vector2 origin = FontAssets.DeathText.Value.MeasureString(cutUpString.ElementAt(i)) * 0.5f;

                // The header has some attributes changed to make it stand out from the names.
                if (i is 0)
                {
                    textColor = Color.Lerp(HeaderColor, Color.White, 0.7f);
                    backglowColor = HeaderColor;
                    scale *= 1.1f;
                }

                // Draw the backglow, then the name.
                ChatManager.DrawColorCodedStringShadow(Main.spriteBatch, FontAssets.DeathText.Value, cutUpString.ElementAt(i), drawPos, backglowColor * 0.25f * opacity, 0f, origin, scale);
                ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.DeathText.Value, cutUpString.ElementAt(i), drawPos, textColor * opacity, 0f, origin, scale);
            }
        }
    }
}
