using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Backgrounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class HellLavaBackgroundSystem : ModSystem
    {
        internal List<BaseHellLavaBackground> LoadedBackgrounds = new();

        internal Dictionary<BaseHellLavaBackground, float> BackgroundIntensities = new();

        internal List<BaseHellLavaBackground> ActiveBackgrounds => LoadedBackgrounds.Where(b => BackgroundIntensities[b] >= 0f).ToList();

        public override void OnModLoad()
        {
            // Load all background effects.
            LoadedBackgrounds = new();
            foreach (Type backgroundType in Utilities.GetEveryTypeDerivedFrom(typeof(BaseHellLavaBackground), InfernumMode.Instance.Code))
            {
                var background = (BaseHellLavaBackground)Activator.CreateInstance(backgroundType);
                LoadedBackgrounds.Add(background);
                BackgroundIntensities[background] = 0f;
            }

            On.Terraria.Main.DrawUnderworldBackgroudLayer += DrawSpecialLava;
        }

        // I wish I could better explain what this shit is about. Background code is evil.
        private void DrawSpecialLava(On.Terraria.Main.orig_DrawUnderworldBackgroudLayer orig, bool flat, Vector2 screenOffset, float pushUp, int layerTextureIndex)
        {
            int underworldBackgroundIndex = Main.underworldBG[layerTextureIndex];
            Asset<Texture2D> asset = TextureAssets.Underworld[underworldBackgroundIndex];
            if (!asset.IsLoaded)
                Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);

            Texture2D texture = asset.Value;
            Vector2 origin = texture.Size() * 0.5f;
            float depth = flat ? 1f : (layerTextureIndex * 2 + 3f);
            Vector2 depthThingIdk = new(1f / depth);
            Rectangle rectangle = new(0, 0, texture.Width, texture.Height);
            float backgroundScale = 1.3f;
            Vector2 drawOffset = Vector2.Zero;
            switch (underworldBackgroundIndex)
            {
                case 1:
                    {
                        int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % 4;
                        rectangle = new Rectangle((frame >> 1) * (texture.Width >> 1), frame % 2 * (texture.Height >> 1), texture.Width >> 1, texture.Height >> 1);
                        origin *= 0.5f;
                        drawOffset.Y += 175f;
                        break;
                    }
                case 2:
                    drawOffset.Y += 100f;
                    break;
                case 3:
                    drawOffset.Y += 75f;
                    break;
                case 4:
                    backgroundScale = 0.5f;
                    drawOffset.Y -= 0f;
                    break;
                case 6:
                    {
                        int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % 4;
                        rectangle = new Rectangle(frame % 2 * (texture.Width >> 1), (frame >> 1) * (texture.Height >> 1), texture.Width >> 1, texture.Height >> 1);
                        origin *= 0.5f;
                        drawOffset.Y += -60f;
                        break;
                    }
                case 7:
                    {
                        int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % 4;
                        rectangle = new Rectangle(frame % 2 * (texture.Width >> 1), (frame >> 1) * (texture.Height >> 1), texture.Width >> 1, texture.Height >> 1);
                        origin *= 0.5f;
                        drawOffset.X -= 400f;
                        drawOffset.Y += 90f;
                        break;
                    }
                case 8:
                    {
                        int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % 4;
                        rectangle = new Rectangle(frame % 2 * (texture.Width >> 1), (frame >> 1) * (texture.Height >> 1), texture.Width >> 1, texture.Height >> 1);
                        origin *= 0.5f;
                        drawOffset.Y += 90f;
                        break;
                    }
                case 9:
                    drawOffset.Y -= 30f;
                    break;
                case 10:
                    drawOffset.Y += 250f * depth;
                    break;
                case 11:
                    drawOffset.Y += 100f * depth;
                    break;
                case 12:
                    drawOffset.Y += 20f * depth;
                    break;
                case 13:
                    {
                        drawOffset.Y += 20f * depth;
                        int frame = (int)(Main.GlobalTimeWrappedHourly * 8f) % 4;
                        rectangle = new Rectangle(frame % 2 * (texture.Width >> 1), (frame >> 1) * (texture.Height >> 1), texture.Width >> 1, texture.Height >> 1);
                        origin *= 0.5f;
                        break;
                    }
            }
            if (flat)
            {
                backgroundScale *= 1.5f;
            }
            origin *= backgroundScale;

            SkyManager.Instance.DrawToDepth(Main.spriteBatch, 1f / depthThingIdk.X);
            if (flat)
                drawOffset.Y += (TextureAssets.Underworld[0].Height() >> 1) * 1.3f - origin.Y;

            drawOffset.Y -= pushUp;
            float horizontalStep = backgroundScale * rectangle.Width;
            int layerCount = (int)(((int)(screenOffset.X * depthThingIdk.X - origin.X + drawOffset.X - (Main.screenWidth >> 1))) / horizontalStep);

            origin = origin.Floor();

            int extraLayers = (int)Math.Ceiling(Main.screenWidth / horizontalStep);
            int depthThing2 = (int)(backgroundScale * ((rectangle.Width - 1) / depthThingIdk.X));
            Vector2 drawPosition = (new Vector2((layerCount - 2) * depthThing2, Main.UnderworldLayer * 16f) + origin - screenOffset) * depthThingIdk + screenOffset - Main.screenPosition - origin + drawOffset;
            drawPosition = drawPosition.Floor();
            while (drawPosition.X + horizontalStep < 0f)
            {
                layerCount++;
                drawPosition.X += horizontalStep;
            }
            for (int i = layerCount - 2; i <= layerCount + 4 + extraLayers; i++)
            {
                Main.spriteBatch.Draw(texture, drawPosition, rectangle, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                if (layerTextureIndex == 0)
                {
                    int verticalCutoff = (int)(drawPosition.Y + rectangle.Height * backgroundScale);
                    Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle((int)drawPosition.X, verticalCutoff, (int)(rectangle.Width * backgroundScale), Math.Max(0, Main.screenHeight - verticalCutoff)), new Color(11, 3, 7));
                }

                // Only part that actually matters; drawing the custom lava effects.
                foreach (var background in ActiveBackgrounds)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Color lavaColor = background.LavaColor * BackgroundIntensities[background];
                        if (underworldBackgroundIndex == 1)
                        {
                            Texture2D lavaTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Backgrounds/UnderworldLava3").Value;
                            Main.spriteBatch.Draw(lavaTexture, drawPosition, rectangle, lavaColor, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                        }
                        if (underworldBackgroundIndex == 6)
                        {
                            Texture2D lavaTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Backgrounds/UnderworldLava1").Value;
                            Main.spriteBatch.Draw(lavaTexture, drawPosition, rectangle, lavaColor, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                        }
                        if (underworldBackgroundIndex == 7)
                        {
                            Texture2D lavaTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Backgrounds/UnderworldLava2").Value;
                            Main.spriteBatch.Draw(lavaTexture, drawPosition, rectangle, lavaColor, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                        }
                    }
                }

                drawPosition.X += horizontalStep;
            }

            SignusBackgroundSystem.Draw();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Update the state of all backgrounds depending on whether they should be drawing or not.
            foreach (BaseHellLavaBackground background in LoadedBackgrounds)
                BackgroundIntensities[background] = Clamp(BackgroundIntensities[background] + background.IsActive.ToDirectionInt() * 0.04f, 0f, 1f);
        }
    }
}
