using System.Collections.Generic;
using CalamityMod.MainMenu;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    public class InfernumMainMenu : ModMenu
    {
        public static Texture2D BackgroundTexture => ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/MenuBackground", AssetRequestMode.ImmediateLoad).Value;

        internal List<Raindroplet> RainDroplets;

        internal List<GlowingEmber> Embers;

        public static SlotId RainSlot { get; private set; }

        private int TimeTilNextFlash;

        public const int FlashTime = 35;

        public override string DisplayName => "Infernum Style";

        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<NullSurfaceBackground>();

        public override Asset<Texture2D> Logo
        {
            get
            {
                if (Utilities.IsAprilFirst())
                    return ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/AprilsFoolsLogo", AssetRequestMode.ImmediateLoad);
                return ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/InfernumLogo", AssetRequestMode.ImmediateLoad);
            }
        }

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
            RainDroplets = [];
            Embers = [];
        }

        public override void Unload()
        {
            RainDroplets = null;
            Embers = null;
        }

        public override void Update(bool isOnTitleScreen)
        {
            if (!SoundEngine.TryGetActiveSound(RainSlot, out var _) && Main.instance.IsActive)
                RainSlot = SoundEngine.PlaySound(InfernumSoundRegistry.RainLoop with { Volume = 0.1f, PlayOnlyIfFocused = true, IsLooped = true });

            // Ensure it is midday.
            Main.time = 27000.0;
            Main.dayTime = true;
        }

        public override void OnDeselected()
        {
            if (SoundEngine.TryGetActiveSound(RainSlot, out var rain))
                rain.Stop();
        }

        private void HandleRaindrops()
        {
            // Remove all things that should die.
            RainDroplets.RemoveAll(r => r.Time >= r.Lifetime);

            float maxDroplets = 300f;
            Rectangle spawnRectangle = new(0, -200, (int)(Main.screenWidth * 1.1f), (int)(Main.screenHeight * 0.3f));

            // Randomly spawn symbols.
            if (RainDroplets.Count < maxDroplets)
            {
                float scaleScalar = Main.rand.NextFloat(1f, 4f);
                Vector2 velocity = Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.05f, 0.35f)) * Main.rand.NextFloat(25f, 34f);
                RainDroplets.Add(new Raindroplet(Main.rand.Next(70, 80), Main.rand.NextFloat(0.35f, 0.85f) * scaleScalar, 0f, Main.rand.NextVector2FromRectangle(spawnRectangle), velocity));
            }

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
                LightningFlash.TimeLeft = FlashTime;
                float distanceModifier = Main.rand.NextFloat(0.2f, 1f);
                LightningFlash.SoundTime = (int)(LightningFlash.TimeLeft * distanceModifier);
                LightningFlash.DistanceModifier = distanceModifier;
            }

            TimeTilNextFlash = (int)Clamp(TimeTilNextFlash - 1, 0f, int.MaxValue);
            LightningFlash.Draw(drawOffset, scale);
        }

        private void HandleEmbers()
        {
            Embers.RemoveAll(e => e.Time >= e.Lifetime);

            float maxEmbers = 75f;
            Rectangle spawnRectangle = new(0, (int)(Main.screenHeight * 1.1f), Main.screenWidth, (int)(Main.screenHeight * 0.1f));
            if (Main.rand.NextBool(3) && Embers.Count < maxEmbers)
            {
                Vector2 position = Main.rand.NextVector2FromRectangle(spawnRectangle);
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f)) * Main.rand.NextFloat(1.2f, 2.4f);
                Color color = Color.Lerp(Color.Pink, Color.Magenta, Main.rand.NextFloat());
                Embers.Add(new GlowingEmber(position, velocity, color, Main.rand.NextFloat(Tau), Main.rand.NextFloat(0f, 0.025f), Main.rand.NextFloat(0.5f, 1f), Main.rand.Next(300, 420)));
            }

            // Draw a large bloom at the bottom of the screen.
            Vector2 drawPos = new(Main.screenWidth / 2f, Main.screenHeight);
            Main.spriteBatch.Draw(GlowingEmber.BloomTexture, drawPos, null, Color.Lerp(Color.Lerp(Color.Pink, Color.Magenta, 0.5f), Color.DarkMagenta, 0.5f) with { A = 0 } * 0.6f, 0f, GlowingEmber.BloomTexture.Size() * 0.5f,
                new Vector2(15f, 1f), SpriteEffects.None, 0f);

            foreach (var ember in Embers)
            {
                ember.Update();
                ember.Draw();
            }
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
            raindrop.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 2f);
            raindrop.Parameters["cellResolution"].SetValue(15f);
            raindrop.Parameters["intensity"].SetValue(1f);
            raindrop.Parameters["sceneBrightness"].SetValue(1f);
            raindrop.CurrentTechnique.Passes["RainPass"].Apply();

            spriteBatch.Draw(BackgroundTexture, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            if (InfernumConfig.Instance.FlashbangOverlays)
                HandleLightning(drawOffset, scale);

            HandleEmbers();

            HandleRaindrops();

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            float interpolant = (1f + Sin(Main.GlobalTimeWrappedHourly * 0.5f)) * 0.5f;
            logoScale = Lerp(0.85f, 1.05f, interpolant);
            spriteBatch.Draw(Logo.Value, logoDrawCenter, null, drawColor, logoRotation, Logo.Value.Size() * 0.5f, logoScale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            return false;
        }
    }
}
