using System.Collections.Generic;
using System.Linq;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Credits
{
    public class CreditAnimationObject(Vector2 velocity, string header, string names, Color headerColor, bool swapSides)
    {
        public Vector2 Center = new(Main.screenWidth * (swapSides ? 0.7f : 0.3f), Main.screenHeight * 0.5f);

        public Vector2 Velocity = velocity;

        public string Header = header;

        public string Names = names;

        public Color HeaderColor = headerColor;

        public bool SwapSides = swapSides;

        public Vector2 TextCenter => new(Main.screenWidth * (SwapSides ? 0.35f : 0.65f), Center.Y);

        public void Update()
        {
            Center += Velocity;
            Center.X = Clamp(Center.X, 0f, Main.screenWidth);
            Center.Y = Clamp(Center.Y, 0f, Main.screenHeight);
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
