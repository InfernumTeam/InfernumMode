using CalamityMod;
using CalamityMod.Items.SummonItems;
using InfernumMode.Assets.Fonts;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.UI
{
    public static class GuardiansPlaqueUIManager
    {
        public static float Opacity
        {
            get;
            private set;
        }

        // If the player isn't within this range of the plaque, the UI will close.
        public static float DistanceThresholdToDrawUI => 100f;

        public static LocalizedText TextToDraw => Utilities.GetLocalization("UI.GuardiansPlaqueUI.PlaqueText");

        // The part of the TextToDraw should be drawn separately.
        public static LocalizedText SpecialText => Utilities.GetLocalization("UI.GuardiansPlaqueUI.SpecialText");

        public const string FieldName = "DrawPlaqueUI";

        public static float TextPadding => 30f;

        public static float TextScale => 0.8f;

        // The plaque spawn position is consistent across all generated profaned temples. This means that it can simply be found via a hardcoded offset relative to the temple's position.
        // This is a bit hack-y and does not generalize, but since players are not able to naturally place down/destroy the plaque, and the alternative to this is tile entities, it will do for now.
        public static Vector2 PlaqueWorldPosition => new(WorldSaveSystem.ProvidenceArena.X * 16f + 4360f, WorldSaveSystem.ProvidenceArena.Y * 16f + 1921f);

        // This not being immediate causes issues with getting it for initial sizings.
        public static Texture2D BackgroundTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/UI/ProfanedPlaqueBackground", AssetRequestMode.ImmediateLoad).Value;

        public static Player Player => Main.LocalPlayer;

        public static Vector2 PlaqueScale => Vector2.One;

        public static bool ShouldDraw => Player.Infernum().GetValue<bool>(FieldName);

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!ShouldDraw)
            {
                // Keep drawing while the opacity fades out.
                Opacity = Clamp(Opacity - 0.2f, 0f, 1f);
                if (Opacity == 0f)
                    return;
            }

            // Increase the opacity.
            else
                Opacity = Clamp(Opacity + 0.2f, 0f, 1f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.DepthRead, Main.Rasterizer, null, Main.UIScaleMatrix);

            // If far enough away, close.
            if (Player.Distance(PlaqueWorldPosition) > DistanceThresholdToDrawUI && ShouldDraw)
                CloseUI();

            Vector2 plaqueDrawCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - Vector2.UnitY * PlaqueScale * 196f;
            Vector2 plaqueDrawTopLeft = plaqueDrawCenter - BackgroundTexture.Size() * PlaqueScale * 0.5f;
            Vector2 textLeftDrawPosition = plaqueDrawTopLeft + Vector2.One * TextPadding;
            int maxTextLength = (int)Math.Round((BackgroundTexture.Width - (2f * TextPadding)) * PlaqueScale.X / TextScale);

            // Draw the background behind the text.
            spriteBatch.Draw(BackgroundTexture, plaqueDrawCenter, null, Color.White * Opacity, 0f, BackgroundTexture.Size() * 0.5f, 1f, 0, 0f);

            string specialTextValue = SpecialText.Value;

            foreach (string line in Utils.WordwrapString(TextToDraw.Value, InfernumFontRegistry.ProfanedTextFont, maxTextLength, 100, out _))
            {
                // If the line is undefined that means that the text has been exhausted, and we can safely leave this loop.
                if (string.IsNullOrEmpty(line))
                    break;

                // Draw the line.
                // If it contains the special line, draw it separately by splitting the surrounding lines.
                if (line.Contains(specialTextValue))
                {
                    List<string> splitLines = line.Split(specialTextValue).ToList();
                    splitLines.Insert(1, specialTextValue);

                    foreach (string line2 in splitLines)
                    {
                        DrawTextLine(line2, textLeftDrawPosition, spriteBatch);
                        textLeftDrawPosition.X += InfernumFontRegistry.ProfanedTextFont.MeasureString(line2).X * TextScale;
                    }
                }
                else
                    DrawTextLine(line, textLeftDrawPosition, spriteBatch);

                // Prepare for the next line by moving the draw position down and resetting the X position to its original value.
                textLeftDrawPosition.X = plaqueDrawTopLeft.X + TextPadding;
                textLeftDrawPosition.Y += 34f * TextScale;
            }
        }

        public static void DrawTextLine(string line, Vector2 textLeftDrawPosition, SpriteBatch spriteBatch)
        {
            Color textColor = WayfinderSymbol.Colors[0];
            Vector2 textArea = InfernumFontRegistry.ProfanedTextFont.MeasureString(line) * TextScale;
            Rectangle textRectangle = new((int)textLeftDrawPosition.X, (int)textLeftDrawPosition.Y + 5, (int)textArea.X, (int)(0.667f * textArea.Y));

            if (line == SpecialText.Value)
            {
                textColor = CalamityUtils.ColorSwap(Color.HotPink, Color.Coral, 1.5f);

                // Display the profaned shard if the mouse is hovering over the text.
                if (Utils.CenteredRectangle(Main.MouseScreen, Vector2.One).Intersects(textRectangle))
                {
                    Player.noThrow = 2;
                    Main.HoverItem = new(ModContent.ItemType<ProfanedShard>());
                    Main.hoverItemName = Main.HoverItem.Name;
                }
            }

            Utils.DrawBorderStringFourWay(spriteBatch, InfernumFontRegistry.ProfanedTextFont, line, textLeftDrawPosition.X, textLeftDrawPosition.Y, textColor * Opacity, textColor * Opacity * 0.16f, Vector2.Zero, TextScale);
        }

        public static void CloseUI()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Player.Infernum().SetValue<bool>(FieldName, false);
        }
    }
}
