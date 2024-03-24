using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static CalamityMod.UI.DraedonSummoning.CodebreakerUI;

namespace InfernumMode.Content.UI
{
    public static class InfernumDraedonDialog
    {
        public static float SelectionAreaPulseInterpolant
        {
            get;
            set;
        }

        public static bool SelectingTextBox
        {
            get;
            set;
        }

        public static string InputtedText
        {
            get;
            set;
        }

        public static float AskButtonScaleInterpolant
        {
            get;
            set;
        }

        public const int InputtedTextLimit = 384;

        public static void DisplayCommunicationPanel()
        {
            // Draw the background panel. This pops up.
            float panelWidthScale = Utils.Remap(CommunicationPanelScale, 0f, 0.5f, 0.085f, 1f);
            float panelHeightScale = Utils.Remap(CommunicationPanelScale, 0.5f, 1f, 0.085f, 1f);
            Vector2 panelScale = GeneralScale * new Vector2(panelWidthScale, panelHeightScale) * 1.4f;
            Texture2D panelTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/DraedonContactPanel").Value;
            float basePanelHeight = GeneralScale * panelTexture.Height * 1.4f;
            Vector2 panelCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f + panelTexture.Height * panelScale.Y * 0.5f - basePanelHeight * 0.5f);
            Rectangle panelArea = Utils.CenteredRectangle(panelCenter, panelTexture.Size() * panelScale);

            Main.spriteBatch.Draw(panelTexture, panelCenter, null, Color.White, 0f, panelTexture.Size() * 0.5f, panelScale, 0, 0f);

            if (DraedonScreenStaticInterpolant > 0.2f)
                InputtedText = string.Empty;

            // Draw static if the static interpolant is sufficiently high.
            if (DraedonScreenStaticInterpolant > 0f)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

                // Apply a glitch shader.
                GameShaders.Misc["CalamityMod:BlueStatic"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/SharpNoise"));
                GameShaders.Misc["CalamityMod:BlueStatic"].Shader.Parameters["useStaticLine"].SetValue(false);
                GameShaders.Misc["CalamityMod:BlueStatic"].Shader.Parameters["coordinateZoomFactor"].SetValue(0.5f);
                GameShaders.Misc["CalamityMod:BlueStatic"].Shader.Parameters["useTrueNoise"].SetValue(true);
                GameShaders.Misc["CalamityMod:BlueStatic"].Apply();

                float readjustedInterpolant = Utils.GetLerpValue(0.42f, 1f, DraedonScreenStaticInterpolant, true);
                Color staticColor = Color.White * Pow(CalamityUtils.AperiodicSin(readjustedInterpolant * 2.94f) * 0.5f + 0.5f, 0.54f) * Pow(readjustedInterpolant, 0.51f);
                Main.spriteBatch.Draw(panelTexture, panelCenter, null, staticColor, 0f, panelTexture.Size() * 0.5f, panelScale, 0, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            }

            // Disable clicks if hovering over the panel.
            if (panelArea.Intersects(MouseScreenArea))
                Main.blockMouse = Main.LocalPlayer.mouseInterface = true;

            DisplayDraedonFacePanel(panelCenter, panelScale);
            DisplayTextSelectionOptions(panelArea, panelScale);
            DisplayDialogHistory(panelArea, panelScale);
        }

        public static void DisplayDraedonFacePanel(Vector2 panelCenter, Vector2 panelScale)
        {
            // Draw a panel that has Draedon's face.
            Texture2D iconTexture = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/DraedonIconBorder").Value;
            Texture2D iconTextureInner = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/DraedonIconBorderInner").Value;
            float draedonIconDrawInterpolant = Utils.GetLerpValue(0.51f, 0.36f, DraedonScreenStaticInterpolant, true);
            Vector2 draedonIconDrawTopRight = panelCenter + new Vector2(-218f, -130f) * panelScale;
            draedonIconDrawTopRight += new Vector2(24f, 4f) * panelScale;

            Vector2 draedonIconScale = panelScale * 0.5f;
            Vector2 draedonIconCenter = draedonIconDrawTopRight + iconTexture.Size() * new Vector2(0.5f, 0.5f) * draedonIconScale;
            Rectangle draedonIconArea = Utils.CenteredRectangle(draedonIconCenter, iconTexture.Size() * draedonIconScale * 0.9f);
            Main.spriteBatch.Draw(iconTexture, draedonIconDrawTopRight, null, Color.White * draedonIconDrawInterpolant, 0f, Vector2.Zero, draedonIconScale, 0, 0f);

            // Draw Draedon's face inside the panel.
            // This involves restarting the sprite batch with a rasterizer state that can cut out Draedon's face if it exceeds the icon area.
            Main.spriteBatch.EnforceCutoffRegion(draedonIconArea, Matrix.Identity, SpriteSortMode.Immediate);

            // Apply a glitch shader.
            GameShaders.Misc["CalamityMod:TeleportDisplacement"].UseOpacity(0.04f);
            GameShaders.Misc["CalamityMod:TeleportDisplacement"].UseSecondaryColor(Color.White * 0.75f);
            GameShaders.Misc["CalamityMod:TeleportDisplacement"].UseSaturation(0.75f);
            GameShaders.Misc["CalamityMod:TeleportDisplacement"].Shader.Parameters["frameCount"].SetValue(Vector2.One);
            GameShaders.Misc["CalamityMod:TeleportDisplacement"].Apply();

            Vector2 draedonScale = new Vector2(draedonIconDrawInterpolant, 1f) * 1.6f;
            SpriteEffects draedonDirection = SpriteEffects.FlipHorizontally;
            Texture2D draedonFaceTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/HologramDraedon").Value;

            Main.spriteBatch.Draw(draedonFaceTexture, draedonIconCenter, null, Color.White * draedonIconDrawInterpolant, 0f, draedonFaceTexture.Size() * 0.5f, draedonScale, draedonDirection, 0f);
            Main.spriteBatch.ReleaseCutoffRegion(Matrix.Identity, SpriteSortMode.Immediate);

            // Draw a glitch effect over the panel and Draedon's icon.
            GameShaders.Misc["CalamityMod:BlueStatic"].UseColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:BlueStatic"].UseImage1("Images/Misc/noise");
            GameShaders.Misc["CalamityMod:BlueStatic"].Shader.Parameters["useStaticLine"].SetValue(true);
            GameShaders.Misc["CalamityMod:BlueStatic"].Shader.Parameters["coordinateZoomFactor"].SetValue(1f);
            GameShaders.Misc["CalamityMod:BlueStatic"].Apply();
            Main.spriteBatch.Draw(iconTextureInner, draedonIconDrawTopRight, null, Color.White * draedonIconDrawInterpolant, 0f, Vector2.Zero, draedonIconScale, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Matrix.Identity);
        }

        public static void DisplayTextSelectionOptions(Rectangle panelArea, Vector2 panelScale)
        {
            // Draw the outline for the text selection options.
            float selectionOptionsDrawInterpolant = Utils.GetLerpValue(0.3f, 0f, DraedonScreenStaticInterpolant, true);
            Texture2D selectionOutline = ModContent.Request<Texture2D>("CalamityMod/UI/DraedonSummoning/DraedonSelectionOutline").Value;
            Vector2 selectionCenter = panelArea.BottomLeft() - new Vector2(selectionOutline.Width * -0.5f - 24f, selectionOutline.Height * 0.5f + 24f) * panelScale;
            Color baseSelectionColor = Color.Lerp(Color.White, Color.Yellow with { A = 0 }, (Sin(Main.GlobalTimeWrappedHourly * 11f) * 0.5f + 0.5f) * SelectionAreaPulseInterpolant);
            Vector2 selectionScale = panelScale * Lerp(1f, 1.1f, SelectionAreaPulseInterpolant);
            Rectangle selectionArea = Utils.CenteredRectangle(selectionCenter, selectionOutline.Size() * selectionScale);
            Main.spriteBatch.Draw(selectionOutline, selectionCenter, null, baseSelectionColor * selectionOptionsDrawInterpolant, 0f, selectionOutline.Size() * 0.5f, selectionScale, 0, 0f);

            // Make the pulse interpolant increase if the box is hovered over or selected.
            if (MouseScreenArea.Intersects(selectionArea) || SelectingTextBox)
            {
                SelectionAreaPulseInterpolant = Clamp(SelectionAreaPulseInterpolant + (SelectingTextBox ? 0.26f : 0.1f), 0f, 1f);

                // Toggle the tex box selection if it's clicked.
                if ((Main.mouseLeft && Main.mouseLeftRelease) || Main.inputTextEscape)
                {
                    SelectingTextBox = !SelectingTextBox;
                    Main.blockInput = SelectingTextBox;

                    if (!SelectingTextBox)
                    {
                        SelectionAreaPulseInterpolant *= 0.3f;

                        if (!string.IsNullOrEmpty(InputtedText))
                        {
                            if (DialogHistory.Count <= 0)
                            {
                                DialogHistory.Add(new(InputtedText, false));
                                DialogHistory.Add(new(string.Empty, true));
                            }
                            else
                            {
                                DialogHistory[^1] = new(InputtedText, false);
                                DialogHistory.Add(new(string.Empty, true));
                            }

                            WrittenDraedonText = "Unfinished";
                            InputtedText = string.Empty;
                        }
                    }
                }
            }
            else
                SelectionAreaPulseInterpolant = Clamp(SelectionAreaPulseInterpolant - 0.08f, 0f, 1f);

            // Take input from the player if if the text box is being selected.
            InputtedText ??= string.Empty;
            if (SelectingTextBox)
            {
                Main.hasFocus = true;
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();
                InputtedText = string.Concat(Main.GetInputText(InputtedText, true).Take(InputtedTextLimit));
            }

            // Draw the inputted text.
            int maxTextWidth = (int)(panelScale.X * selectionOutline.Width * 1.05f);
            Vector2 textTopLeft = selectionArea.TopLeft() + Vector2.UnitX * panelScale * 10f;
            foreach (string line in Utils.WordwrapString(InputtedText, FontAssets.MouseText.Value, maxTextWidth, 10, out _))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                textTopLeft.Y += panelScale.Y * 16f;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, line, textTopLeft, Color.Cyan, 0f, Vector2.Zero, Vector2.One * GeneralScale * 0.7f);
            }
        }
    }
}
