using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Rarities
{
    public static class InfernumRarityHelper
    {
        public static Texture2D GlowTexture => ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/BaseRarityGlow").Value;

        public static Texture2D SparkleTexure => ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/BaseRaritySparkleTexture").Value;

        public static void DrawBaseTooltipTextAndGlow(DrawableTooltipLine tooltipLine, Color glowColor, Color textOuterColor, Color? textInnerColor = null, Texture2D glowTexture = null)
        {
            textInnerColor ??= Color.Black;
            glowTexture ??= GlowTexture;

            // Get the text of the tooltip line.
            string text = tooltipLine.Text;
            // Get the size of the text in its font.
            Vector2 textSize = tooltipLine.Font.MeasureString(text);
            // Get the center of the text.
            Vector2 textCenter = textSize * 0.5f;
            // The position to draw the text.
            Vector2 textPosition = new(tooltipLine.X, tooltipLine.Y);
            // Get the position to draw the glow behind the text.
            Vector2 glowPosition = new(tooltipLine.X + textCenter.X, tooltipLine.Y + textCenter.Y / 1.5f);
            // Get the scale of the glow texture based off of the text size.
            Vector2 glowScale = new(textSize.X * 0.085f, 0.3f);

            // Draw the glow texture.
            Main.spriteBatch.Draw(glowTexture, glowPosition, null, glowColor * 0.4f, 0f, GlowTexture.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);

            // Get an offset to the afterimageOffset based on a sine wave.
            float sine = (float)((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) / 2);
            float sineOffset = MathHelper.Lerp(0.7f, 1.3f, sine);

            // Draw text backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * (2f * sineOffset);
                // Draw the text. Rotate the position based on i.
                ChatManager.DrawColorCodedString(Main.spriteBatch, tooltipLine.Font, text, (textPosition + afterimageOffset).RotatedBy(MathHelper.TwoPi * (i / 12)), textOuterColor * 0.9f, tooltipLine.Rotation, tooltipLine.Origin, tooltipLine.BaseScale);
            }

            // Draw the main inner text.
            Color mainTextColor = Color.Lerp(glowColor, textInnerColor.Value, 0.9f);
            ChatManager.DrawColorCodedString(Main.spriteBatch, tooltipLine.Font, text, textPosition, mainTextColor, tooltipLine.Rotation, tooltipLine.Origin, tooltipLine.BaseScale);
        }

        public static void SpawnAndUpdateTooltipParticles(DrawableTooltipLine tooltipLine, ref List<RaritySparkle> sparklesList, int spawnChance, SparkleType sparkleType)
        {
            Vector2 textSize = tooltipLine.Font.MeasureString(tooltipLine.Text);
            Color sparkleColor;
            float sparkleMaxScale;
            float sparkleAverageLifetime;
            float sparkleRotationSpeed;
            Texture2D sparkleTexture;
            Rectangle? frame = null;
            bool useVelocity = true;
            bool useAdditive = true;
            float velocityDirection = -1;
            float rectYOffset = 0.25f;
            float rectHeightOffset = 0.5f;

            switch (sparkleType)
            {
                case SparkleType.ProfanedSparkle:
                    sparkleColor = Main.rand.NextBool() ? new Color(255, 255, 150) : new Color(255, 191, 73);
                    sparkleAverageLifetime = 70f;
                    sparkleRotationSpeed = 0.06f;
                    if (Main.rand.NextBool())
                    {
                        sparkleTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/CritSpark").Value;
                        frame = new(0, 0, 14, 15);
                        sparkleMaxScale = 0.65f;
                    }
                    else
                    {
                        sparkleTexture = SparkleTexure;
                        sparkleMaxScale = 0.8f;
                    }
                    useVelocity = false;
                    rectYOffset = 0.3f;
                    rectHeightOffset = 0.35f;
                    break;

                case SparkleType.RelicSparkle:
                    sparkleColor = Color.Lerp(Color.OrangeRed, Color.Red, Main.rand.NextFloat(0, 1f));
                    sparkleMaxScale = 0.6f;
                    sparkleAverageLifetime = 70f;
                    sparkleRotationSpeed = 0.03f;
                    sparkleTexture = SparkleTexure;
                    break;

                case SparkleType.VassalSparkle:
                    sparkleColor = Color.Lerp(Color.CadetBlue, Color.LightBlue, Main.rand.NextFloat(0, 1f));
                    sparkleMaxScale = 0.3f;
                    sparkleAverageLifetime = 70f;
                    sparkleRotationSpeed = 0f;
                    sparkleTexture = InfernumVassalRarity.DropletTexture;
                    velocityDirection = 1;
                    rectYOffset = 0.4f;
                    rectHeightOffset = 0.35f;
                    break;

                default:
                    sparkleColor = Color.White;
                    sparkleMaxScale = 0.5f;
                    sparkleAverageLifetime = 70f;
                    sparkleRotationSpeed = 0.03f;
                    sparkleTexture = SparkleTexure;
                    break;
            }

            // Randomly spawn sparkles.
            if (Main.rand.NextBool(spawnChance))
            {
                int lifetime = (int)Main.rand.NextFloat(sparkleAverageLifetime - 25f, sparkleAverageLifetime);
                float scale = Main.rand.NextFloat(sparkleMaxScale * 0.5f, sparkleMaxScale);
                float initialRotation = sparkleRotationSpeed == 0f ? 0 : Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float rotationSpeed = Main.rand.NextFloat(-sparkleRotationSpeed, sparkleRotationSpeed);
                Vector2 position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * rectYOffset), (int)textSize.X, (int)(textSize.Y * rectHeightOffset)));
                Vector2 velocity = useVelocity ? velocityDirection * Vector2.UnitY * Main.rand.NextFloat(0.1f, 0.2f) : Vector2.Zero;
                Color color = sparkleColor;
                if (useAdditive)
                    color.A = 0;
                sparklesList.Add(new RaritySparkle(sparkleType, lifetime, scale, initialRotation, rotationSpeed, position, velocity, color, sparkleTexture, frame));
            }

            // Update any active sparkles.
            for (int i = 0; i < sparklesList.Count; i++)
                sparklesList[i].Update();

            // Remove any sparkles that have existed long enough.
            sparklesList.RemoveAll((RaritySparkle s) => s.Time >= s.Lifetime);

            // Draw the sparkles.
            foreach (RaritySparkle sparkle in sparklesList)
                sparkle.Draw(Main.spriteBatch, new Vector2(tooltipLine.X, tooltipLine.Y) + textSize * 0.5f + sparkle.Position);
        }
    }
}
