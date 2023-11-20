using InfernumMode.Content.Rarities.Sparkles;
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
        public static void DrawBaseTooltipTextAndGlow(DrawableTooltipLine tooltipLine, Color glowColor, Color textOuterColor, Color? textInnerColor = null, Texture2D glowTexture = null, Vector2? glowScaleOffset = null)
        {
            textInnerColor ??= Color.Black;
            glowTexture ??= RarityTextureRegistry.BaseRarityGlow;
            glowScaleOffset ??= Vector2.One;
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
            Vector2 glowScale = new Vector2(textSize.X * 0.115f, 0.6f) * glowScaleOffset.Value;
            glowColor.A = 0;
            // Draw the glow texture.
            Main.spriteBatch.Draw(glowTexture, glowPosition, null, glowColor * 0.85f, 0f, glowTexture.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);

            // Get an offset to the afterimageOffset based on a sine wave.
            float sine = (float)((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) / 2);
            float sineOffset = Lerp(0.5f, 1f, sine);

            // Draw text backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (TwoPi * i / 12f).ToRotationVector2() * (2f * sineOffset);
                // Draw the text. Rotate the position based on i.
                ChatManager.DrawColorCodedString(Main.spriteBatch, tooltipLine.Font, text, (textPosition + afterimageOffset).RotatedBy(TwoPi * (i / 12)), textOuterColor * 0.9f, tooltipLine.Rotation, tooltipLine.Origin, tooltipLine.BaseScale);
            }

            // Draw the main inner text.
            Color mainTextColor = Color.Lerp(glowColor, textInnerColor.Value, 0.9f);
            ChatManager.DrawColorCodedString(Main.spriteBatch, tooltipLine.Font, text, textPosition, mainTextColor, tooltipLine.Rotation, tooltipLine.Origin, tooltipLine.BaseScale);
        }

        public static void SpawnAndUpdateTooltipParticles(DrawableTooltipLine tooltipLine, ref List<RaritySparkle> sparklesList, int spawnChance, SparkleType sparkleType)
        {
            Vector2 textSize = tooltipLine.Font.MeasureString(tooltipLine.Text);

            // Randomly spawn sparkles.
            if (Main.rand.NextBool(spawnChance))
            {
                int lifetime;
                float scale;
                float initialRotation;
                float rotationSpeed;
                Vector2 position;
                Vector2 velocity;

                switch (sparkleType)
                {
                    case SparkleType.HourglassSparkle:
                        lifetime = (int)Main.rand.NextFloat(120f - 25f, 120f);
                        scale = Main.rand.NextFloat(0.25f * 0.5f, 0.25f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = Vector2.UnitY * Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(0.05f, 0.15f);
                        sparklesList.Add(new HourglassSparkle(lifetime, scale, 0f, 0f, position, Vector2.Zero));
                        break;

                    case SparkleType.ProfanedSparkle:
                        lifetime = (int)Main.rand.NextFloat(70f - 25f, 70f);
                        scale = Main.rand.NextFloat(0.9f * 0.5f, 0.9f);
                        initialRotation = Main.rand.NextFloat(0f, TwoPi);
                        rotationSpeed = Main.rand.NextFloat(-0.03f, 0.03f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        sparklesList.Add(new ProfanedSparkle(lifetime, scale, initialRotation, rotationSpeed, position, Vector2.Zero));
                        break;

                    case SparkleType.RelicSparkle:
                        lifetime = (int)Main.rand.NextFloat(70f - 25f, 70f);
                        scale = Main.rand.NextFloat(0.6f * 0.5f, 0.6f);
                        initialRotation = Main.rand.NextFloat(0f, TwoPi);
                        rotationSpeed = Main.rand.NextFloat(-0.03f, 0.03f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = (-Vector2.UnitY * Main.rand.NextFloat(0.05f, 0.15f)).RotatedBy(Main.rand.NextFloat(-0.05f, 0.05f));
                        sparklesList.Add(new RelicSparkle(lifetime, scale, initialRotation, rotationSpeed, position, velocity));
                        break;

                    case SparkleType.EggSparkle:
                    case SparkleType.VassalSparkle:
                        lifetime = (int)Main.rand.NextFloat(70f - 25f, 70f);
                        scale = Main.rand.NextFloat(0.3f * 0.5f, 0.3f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.4f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = Vector2.UnitY * Main.rand.NextFloat(0.1f, 0.25f);
                        if (sparkleType == SparkleType.VassalSparkle)
                            sparklesList.Add(new VassalSparkle(lifetime, scale, 0f, 0f, position, velocity));
                        else
                            sparklesList.Add(new EggSparkle(lifetime, scale, 0f, 0f, position, velocity));
                        break;

                    case SparkleType.CodeSymbols:
                        lifetime = Main.rand.Next(30, 42);
                        scale = Main.rand.NextFloat(0.8f, 1f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f))) + Vector2.UnitY * 5f;
                        velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.05f, 0.05f)) * Main.rand.NextFloat(0.15f, 0.5f);
                        sparklesList.Add(new CodeSymbol(lifetime, scale, position, velocity));
                        break;

                    case SparkleType.RedLightningSparkle:
                        lifetime = Main.rand.Next(35, 48);
                        scale = Main.rand.NextFloat(0.8f, 1f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = position.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.15f) * Main.rand.NextFloat(0.2f, 0.54f);
                        velocity.Y -= 0.14f;
                        sparklesList.Add(new RedLightningSparkle(lifetime, scale, velocity.ToRotation() + PiOver2, position, velocity));
                        break;

                    case SparkleType.PuritySparkle:
                        lifetime = (int)Main.rand.NextFloat(70f - 25f, 70f);
                        scale = Main.rand.NextFloat(0.3f * 0.5f, 0.3f);
                        initialRotation = Main.rand.NextFloat(0f, TwoPi);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = (-Vector2.UnitY * Main.rand.NextFloat(0.05f, 0.15f)).RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                        sparklesList.Add(new PuritySparkle(lifetime, scale, velocity.ToRotation() + PiOver2, 0f, position, velocity));
                        break;

                    case SparkleType.MusicNotes:
                        lifetime = Main.rand.Next(50, 105);
                        scale = Main.rand.NextFloat(0.3f, 0.45f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = position.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.15f) * Main.rand.NextFloat(0.125f, 0.432f);
                        velocity.Y -= 0.25f;
                        velocity *= 0.4f;
                        sparklesList.Add(new MusicNoteSymbol(lifetime, scale, position, velocity));
                        break;

                    case SparkleType.BubbleSparkle:
                        lifetime = (int)Main.rand.NextFloat(90f, 120f);
                        scale = Main.rand.NextFloat(0.4f * 0.5f, 0.4f);
                        initialRotation = Main.rand.NextFloat(0f, TwoPi);
                        rotationSpeed = Main.rand.NextFloat(0.01f, 0.03f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.5f)));
                        velocity = (-Vector2.UnitY * Main.rand.NextFloat(0.05f, 0.15f)).RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                        sparklesList.Add(new BubbleSparkle(lifetime, scale, initialRotation, rotationSpeed, position, velocity));
                        break;

                    case SparkleType.CyanLightningSparkle:
                        for (int i = 0; i < 2; i++)
                        {
                            lifetime = Main.rand.Next(32, 45);
                            scale = Main.rand.NextFloat(0.8f, 1.1f);
                            position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                            velocity = position.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.15f) * Main.rand.NextFloat(0.1f, 0.3f);
                            velocity.Y -= 0.1f;
                            sparklesList.Add(new CyanLightningSparkle(lifetime, scale, velocity.ToRotation() + PiOver2, position, velocity));
                        }
                        break;

                    case SparkleType.SakuraSparkle:
                        lifetime = (int)Main.rand.NextFloat(270f, 300f);
                        scale = Main.rand.NextFloat(0.8f * 0.75f, 0.8f);
                        initialRotation = Main.rand.NextFloat(0f, TwoPi);
                        rotationSpeed = Main.rand.NextFloat(0.005f, 0.01f);
                        position = Main.rand.NextVector2FromRectangle(new((int)(textSize.X * 0.35f), -(int)(textSize.Y * 0.55f), (int)(textSize.X * 0.3f), (int)(textSize.Y * 0.3f)));
                        velocity = (Vector2.UnitY * Main.rand.NextFloat(0.25f, 0.41f)).RotatedBy(PiOver4 + Main.rand.NextFloat(0.65f, 0.75f));
                        sparklesList.Add(new SakuraSparkle(lifetime, scale, initialRotation, rotationSpeed, position, velocity));
                        break;

                    case SparkleType.BookSparkle:
                        lifetime = (int)Main.rand.NextFloat(60f, 90f);
                        scale = Main.rand.NextFloat(0.125f, 0.25f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = Vector2.UnitY * Main.rand.NextBool().ToDirectionInt() * Main.rand.NextFloat(0.05f, 0.15f);
                        sparklesList.Add(new BookSparkle(lifetime, scale, 0f, 0f, position, Vector2.Zero));
                        break;

                    case SparkleType.TransSparkle:
                        lifetime = (int)Main.rand.NextFloat(90f, 120f);
                        scale = Main.rand.NextFloat(0.525f, 0.85f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        velocity = (-Vector2.UnitY * Main.rand.NextFloat(0.05f, 0.15f)).RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                        sparklesList.Add(new TransSparkle(lifetime, scale, 0f, 0f, position, velocity));
                        break;

                    case SparkleType.CreditSparkle:
                        lifetime = (int)Main.rand.NextFloat(20f, 25f);
                        scale = Main.rand.NextFloat(0.325f, 0.65f);
                        position = Main.rand.NextVector2FromRectangle(new(-(int)(textSize.X * 0.5f), -(int)(textSize.Y * 0.3f), (int)textSize.X, (int)(textSize.Y * 0.35f)));
                        sparklesList.Add(new CreditSparkle(lifetime, scale, Main.rand.NextFloat(Tau), 0f, position, Vector2.Zero));
                        break;
                }
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
