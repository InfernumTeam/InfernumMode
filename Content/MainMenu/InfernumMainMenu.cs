using CalamityMod.MainMenu;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    public class InfernumMainMenu : ModMenu
    {
        public static Texture2D BackgroundTexture => ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/MenuBackground", AssetRequestMode.ImmediateLoad).Value;

        internal List<Raindroplet> RainDroplets;

        private int TimeTilNextFlash;

        public override string DisplayName => "Infernum Style";

        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<NullSurfaceBackground>();

        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/Logo", AssetRequestMode.ImmediateLoad);

        public override Asset<Texture2D> MoonTexture => InfernumTextureRegistry.Invisible;

        public override Asset<Texture2D> SunTexture => InfernumTextureRegistry.Invisible;

        public override int Music => SetMusic();

        private static int SetMusic()
        {
            if (InfernumMode.MusicModIsActive)
                return MusicLoader.GetMusicSlot(InfernumMode.InfernumMusicMod, "Sounds/Music/TitleScreen");
            return MusicID.MenuMusic;
        }

        public override void Load()
        {
            RainDroplets = new();
        }

        public override void Unload()
        {
            RainDroplets = null;
        }      

        private void HandleRaindrops()
        {
            // Remove all things that should die.
            RainDroplets.RemoveAll(r => r.Time >= r.Lifetime);

            float maxDroplets = 300f;
            Rectangle spawnRectangle = new(0, -200, Main.screenWidth, (int)(Main.screenHeight * 0.3f));

            // Randomly spawn symbols.
            if (RainDroplets.Count < maxDroplets)
                RainDroplets.Add(new Raindroplet(Main.rand.Next(300, 300), Main.rand.NextFloat(0.35f, 0.85f), 0f, Main.rand.NextVector2FromRectangle(spawnRectangle),
                    Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.05f, 0.35f)) * Main.rand.NextFloat(9f, 15f)));

            foreach (var rain in RainDroplets)
            {
                rain.Update();
                rain.Draw();
            }
        }

        private void HandleLightning(Vector2 drawOffset, float scale)
        {
            if (TimeTilNextFlash == 0)
            {
                TimeTilNextFlash = Main.rand.Next(240, 480);
                LightningFlash.TimeLeft = 35;
                float distanceModifier = Main.rand.NextFloat();
                LightningFlash.SoundTime = (int)(LightningFlash.TimeLeft * distanceModifier);
                LightningFlash.DistanceModifier = distanceModifier;
            }
            TimeTilNextFlash = (int)MathHelper.Clamp(TimeTilNextFlash - 1, 0f, int.MaxValue);
            LightningFlash.Draw(drawOffset, scale);
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            Vector2 drawOffset = Vector2.Zero;
            float xScale = (float)Main.screenWidth / BackgroundTexture.Width;
            float yScale = (float)Main.screenHeight / BackgroundTexture.Height;
            float scale = xScale;

            if (xScale != yScale)
            {
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (BackgroundTexture.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                    drawOffset.Y -= (BackgroundTexture.Height * scale - Main.screenHeight) * 0.5f;
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            
            // Apply a raindrop effect to the texture.
            Effect raindrop = InfernumEffectsRegistry.RaindropShader.GetShader().Shader;
            raindrop.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            raindrop.Parameters["cellResolution"].SetValue(15f);
            raindrop.Parameters["intensity"].SetValue(2f);
            raindrop.CurrentTechnique.Passes["RainPass"].Apply();
            spriteBatch.Draw(BackgroundTexture, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            HandleRaindrops();

            if (InfernumConfig.Instance.FlashbangOverlays)
                HandleLightning(drawOffset, scale);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            spriteBatch.Draw(Logo.Value, logoDrawCenter, null, drawColor, logoRotation, Logo.Value.Size() * 0.5f, logoScale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            return false;
        }
    }
}