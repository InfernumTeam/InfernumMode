using CalamityMod.MainMenu;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    internal class InfernumMainMenu : ModMenu
    {
        internal static List<BoidCinder> Boids;

        internal static List<Raindroplet> RainDroplets;

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
            Boids = new();
            RainDroplets = new();
        }

        public override void Unload()
        {
            Boids = null;
            RainDroplets = null;
        }

        private void HandleBoids()
        {
            Boids.RemoveAll(b => b.Time >= b.Lifetime);

            Rectangle spawnRectangle = new(50, 50, Main.screenWidth - 50, Main.screenHeight - 50);
            int maxBoids = 200;
            // Setup the boids if none are present, when the mod loads.

            if (Main.rand.NextBool(2) && Boids.Count < maxBoids)
                Boids.Add(new(Main.rand.Next(300, 600), Main.rand.NextFloat(0.15f, 0.2f), 0f, Main.rand.NextVector2FromRectangle(spawnRectangle), Main.rand.NextFloat(MathF.Tau).ToRotationVector2() * Main.rand.NextFloat(2f, 4f)));
            

            foreach (var boid in Boids)
            {
                boid.Update();
                boid.Draw();
            }
        }

        private void HandleRaindrops()
        {
            // Remove all things that should die.
            RainDroplets.RemoveAll(r => r.Time >= r.Lifetime);

            float maxDroplets = 200f;
            Rectangle spawnRectangle = new(0, 0, 1920, 1080);

            // Randomly spawn symbols.
            if (Main.rand.NextBool(3) && RainDroplets.Count < maxDroplets)
                RainDroplets.Add(new Raindroplet(Main.rand.Next(150, 250), Main.rand.NextFloat(0.75f, 1.35f), 0f, Main.rand.NextVector2FromRectangle(spawnRectangle), Vector2.UnitY * Main.rand.NextFloat(1f, 3f)));

            foreach (var rain in RainDroplets)
            {
                rain.Update();
                //rain.Draw();
            }
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            Texture2D backgroundTexture = ModContent.Request<Texture2D>("CalamityMod/MainMenu/MenuBackground").Value;
            Vector2 drawOffset = Vector2.Zero;
            float xScale = (float)Main.screenWidth / backgroundTexture.Width;
            float yScale = (float)Main.screenHeight / backgroundTexture.Height;
            float scale = xScale;
            if (xScale != yScale)
            {
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (backgroundTexture.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                {
                    drawOffset.Y -= (backgroundTexture.Height * scale - Main.screenHeight) * 0.5f;
                }
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            
            // Apply a raindrop effect to the texture.
            Effect raindrop = InfernumEffectsRegistry.RaindropShader.GetShader().Shader;
            raindrop.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            raindrop.Parameters["cellResolution"].SetValue(20f);
            raindrop.Parameters["intensity"].SetValue(2f);
            raindrop.CurrentTechnique.Passes["RainPass"].Apply();
            spriteBatch.Draw(backgroundTexture, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            HandleBoids();
            HandleRaindrops();

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            spriteBatch.Draw(Logo.Value, logoDrawCenter, null, drawColor, logoRotation, Logo.Value.Size() * 0.5f, logoScale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            return false;
        }
    }
}