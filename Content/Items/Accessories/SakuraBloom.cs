using CalamityMod.Items;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Content.Rarities.Sparkles;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Items.Accessories
{
    public class SakuraBloom : ModItem
    {
        private List<RaritySparkle> loveSparkles = new();
        private List<RaritySparkle> memorySparkles = new();

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sakura Bloom");
            // Tooltip.SetDefault("A symbol of how beautiful love is when in bloom, and how easily it can wither away whyyyy\nTemporary");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 26;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumSakuraRarity>();
            Item.accessory = true;
            Item.vanity = true;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            loveSparkles.RemoveAll(s => s.Time >= s.Lifetime);
            memorySparkles.RemoveAll(s => s.Time >= s.Lifetime);
            if (line.Text.StartsWith("A symbol of how beautiful love is when in bloom, and how easily it can wither away"))
            {
                Vector2 drawOffset = Vector2.UnitY * yOffset;

                drawOffset.X += DrawLine(line, drawOffset, ref loveSparkles, "A symbol of how beautiful ");

                drawOffset.X += DrawLine(line, drawOffset, ref loveSparkles, "love", true);

                DrawLine(line, drawOffset, ref loveSparkles, " is when it’s in bloom, and yet how easily it can wither away");
                return false;
            }
            else if (line.Text == "Temporary")
            {
                Vector2 drawOffset = Vector2.UnitY * yOffset;
                drawOffset.X += DrawLine(line, drawOffset, ref loveSparkles, "Maybe with this, we can hold onto the ");
                drawOffset.X += DrawLine(line, drawOffset, ref memorySparkles, "memories", true, 12);
                drawOffset.X += DrawLine(line, drawOffset, ref loveSparkles, "?");
                return false;
            }

            return true;
        }

        public static float DrawLine(DrawableTooltipLine line, Vector2 drawOffset, ref List<RaritySparkle> sparkles, string overridingText = null, bool specialText = false, int spawnRate = 16, Color? overrideColor = null)
        {
            Color textOuterColor = new(235, 195, 240);
            if (specialText)
                textOuterColor = new(244, 127, 255);

            if (overrideColor != null)
                textOuterColor = overrideColor.Value;

            Color textInnerColor = Color.Lerp(Color.Black, textOuterColor, 0.15f);

            // Get the text of the tooltip line.
            string text = overridingText ?? line.Text;
            Vector2 textPosition = new Vector2(line.X, line.Y) + drawOffset;

            // Get an offset to the afterimageOffset based on a sine wave.
            float sine = (float)((1f + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) * 0.5f);
            float sineOffset = Lerp(0.4f, 0.775f, sine);

            // Draw text backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * (2f * sineOffset);

                // Draw the text. Rotate the position based on i.
                ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, (textPosition + afterimageOffset).RotatedBy(TwoPi * (i / 12)), textOuterColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
            }

            // Draw the main inner text.
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, textPosition, textInnerColor, line.Rotation, line.Origin, line.BaseScale);
            Vector2 lineSize = line.Font.MeasureString(text) * line.BaseScale;

            if (specialText && sparkles != null)
            {
                // Spawn sparkles
                if (Main.rand.NextBool(spawnRate))
                {
                    Rectangle rectangle = new((int)(-lineSize.X * 0.5f), (int)(-lineSize.Y * 0.3f), (int)(lineSize.X), (int)(lineSize.Y * 0.5f));
                    Vector2 position = Main.rand.NextVector2FromRectangle(rectangle);
                    PinkSparkle pinkSparkle = new(Main.rand.Next(90, 120), Main.rand.NextFloat(0.2f, 0.4f), Main.rand.NextFloat(TwoPi),
                        Main.rand.NextFloat(0, 0.02f) * Main.rand.NextFromList(-1, 1), position, -Vector2.UnitY * Main.rand.NextFloat(0.025f, 0.075f));

                    sparkles.Add(pinkSparkle);
                }
                // Update and draw them.
                foreach (var sparkle in sparkles)
                {
                    sparkle.Update();
                    sparkle.Draw(Main.spriteBatch, lineSize * 0.5f + textPosition + sparkle.Position);
                }
            }

            // Return the x offset.
            return line.Font.MeasureString(text).X * line.BaseScale.X;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<CherryBlossomPlayer>().CreatingCherryBlossoms = true;

        public override void UpdateVanity(Player player) => player.GetModPlayer<CherryBlossomPlayer>().CreatingCherryBlossoms = true;
    }
}
