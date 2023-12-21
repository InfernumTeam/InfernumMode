using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class FlowerOceanSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override float GetWeight(Player player) => 0.8f;

        public override bool IsSceneEffectActive(Player player)
        {
            return player.Infernum().GetValue<bool>("FlowerOceanVisualsActive");
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:FlowerOfTheOcean", isActive);
        }
    }

    public class FlowerOceanSky : CustomSky
    {
        public class Godray
        {
            public int Timer;
            public int Lifetime;
            public float ColorLerpAmount;
            public float Rotation;
            public float RotationSpeed;
            public float Opacity;
            public float OpacityScalar;
            public float Depth;
            public float LengthScalar;
            public bool GuardsVersion;
            public Vector2 DrawPosition;

            public float LifetimeCompletion => Timer / (float)Lifetime;

            public void Update()
            {
                if (!GuardsVersion)
                    DrawPosition = GetMoonPosition();
                Opacity = Utils.GetLerpValue(0f, 0.2f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true);
                Rotation += RotationSpeed;
                Timer++;
            }

            public void Draw(SpriteBatch spriteBatch, float opacity)
            {
                Texture2D rayTexture = InfernumTextureRegistry.LaserCircle.Value;

                Color lightColor = (GuardsVersion ? Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[2], ColorLerpAmount) :
                    Color.Lerp(Color.SkyBlue, Color.Teal, ColorLerpAmount))
                    * Opacity * (0.015f * OpacityScalar) * opacity;

                Vector2 scale = new(0.4f, 15.3f * LengthScalar);
                spriteBatch.Draw(rayTexture, DrawPosition, null, lightColor with { A = 0 }, Rotation, rayTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        public class Cinder
        {
            public int Timer;
            public int Lifetime;
            public float ColorLerpAmount;
            public float Rotation;
            public float RotationSpeed;
            public float Opacity;
            public float Depth;
            public Vector2 DrawPosition;
            public Vector2 Velocity;
            public bool Bubble;

            public float LifetimeCompletion => Timer / (float)Lifetime;

            public void Update()
            {
                DrawPosition += Velocity;
                Opacity = Utils.GetLerpValue(0f, 0.02f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.98f, LifetimeCompletion, true);
                Rotation += RotationSpeed;
                Timer++;
            }
        }

        private readonly List<Godray> Rays = new();
        private readonly List<Cinder> Cinders = new();
        public static readonly List<FishBoid> Fishes = new();
        private float intensity;
        private bool isActive;

        public override void Activate(Vector2 position, params object[] args) => isActive = true;

        public override void Deactivate(params object[] args) => isActive = false;

        public override void Reset() => isActive = false;

        public override bool IsActive() => isActive || intensity > 0f;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f)
                intensity = Clamp(intensity + 0.005f, 0f, 1f);
            else if (!isActive && intensity > 0f)
                intensity = Clamp(intensity - 0.005f, 0f, 1f);

            // Kill every cloud
            for (int i = 0; i < Main.maxClouds; i++)
                Main.cloud[i].kill = true;

            // Deactivate any blood moons and wind.
            if (Main.bloodMoon)
                Main.bloodMoon = false;

            CalamityMod.CalamityMod.StopRain();

            Main.windSpeedTarget = 0f;

            if (!Main.LocalPlayer.Infernum().GetValue<bool>("FlowerOceanVisualsActive") || Main.dayTime)
            {
                isActive = false;
                if (intensity <= 0)
                    Deactivate();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Only draw at night time.
            if (Main.dayTime)
                return;

            // If either of these are drawing, don't.
            if (AstralDimensionSystem.MonolithIntensity >= 1f || CosmicBackgroundSystem.MonolithIntensity >= 1f)
                return;

            // Draw the sky and moon.
            if (maxDepth >= 0 && minDepth < 0)
            {
                Texture2D skyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/FlowerOceanSky").Value;
                spriteBatch.Draw(skyTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White with { A = 0 } * intensity * 0.2f);

                // Draw underwater rays over the screen.
                DrawWaterRays(spriteBatch);

                // Draw the light rays.
                DrawRays(spriteBatch);
                // Draw the moon.
                DrawMoon(spriteBatch);
            }
            DrawFish();
            DrawCinders(spriteBatch, minDepth, maxDepth);
        }

        private void DrawWaterRays(SpriteBatch spriteBatch)
        {
            Texture2D noise = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleGradients/BlurryPerlinNoise", AssetRequestMode.ImmediateLoad).Value;
            Effect waterRays = InfernumEffectsRegistry.UnderwaterRayShader.Shader;
            waterRays.Parameters["noiseMap"].SetValue(noise);
            waterRays.Parameters["mainColor"].SetValue(Color.SkyBlue.ToVector3() * 1.5f);

            waterRays.Parameters["noiseTiling"].SetValue(new Vector2(8f, 0.2f));
            waterRays.Parameters["scrollSpeed"].SetValue(new Vector2(-0.1f, -0.1f));

            waterRays.Parameters["noiseScale"].SetValue(1f);
            waterRays.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            waterRays.Parameters["dissolvePower"].SetValue(1.5f);
            waterRays.Parameters["sceneOpacity"].SetValue(intensity);

            Vector2 scale = new(8f, 4f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, waterRays, Utilities.GetCustomSkyBackgroundMatrix());
            spriteBatch.Draw(noise, new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f), null, Color.White, 0f, noise.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());
        }

        private void DrawRays(SpriteBatch spriteBatch)
        {
            int maxRays = InfernumConfig.Instance.ReducedGraphicsConfig ? 45 : 60;
            // Randomly spawn light rays under the moon.
            if (Main.rand.NextBool(10) && Rays.Count < maxRays)
            {
                Rays.Add(new Godray()
                {
                    Lifetime = Main.rand.Next(320, 540),
                    Rotation = Main.rand.NextFloat(TwoPi),
                    RotationSpeed = Main.rand.NextFloat(0.001f, 0.002f) * Main.rand.NextFromList(-1, 1),
                    ColorLerpAmount = Main.rand.NextFloat(),
                    OpacityScalar = Main.rand.NextFloat(1f, 1.5f),
                    Depth = Main.rand.NextFloat(1.3f, 3f),
                    LengthScalar = Main.rand.NextFloat(0.3f, 0.8f)
                });
            }

            while (Rays.Count > maxRays)
                Rays.RemoveAt(0);

            // Draw the rays.
            foreach (Godray ray in Rays)
            {
                ray.Update();
                ray.Draw(spriteBatch, intensity);
            }
            Rays.RemoveAll(r => r.Timer >= r.Lifetime);
        }

        private void DrawMoon(SpriteBatch spriteBatch)
        {
            // Draw the moon.
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;//InfernumTextureRegistry.DistortedBloomRing.Value;
            Color bloomColor = Color.LightSkyBlue with { A = 0 } * intensity;
            float rotation = Main.GlobalTimeWrappedHourly * 0.67f;
            float bloomScale = 0.7f;
            for (int i = 0; i < 2; i++)
            {
                spriteBatch.Draw(bloom, GetMoonPosition(), null, bloomColor, rotation, bloom.Size() * 0.5f, bloomScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(bloom, GetMoonPosition(), null, bloomColor * 0.05f, rotation, bloom.Size() * 0.5f, bloomScale * 5f, SpriteEffects.None, 0f);
            }
        }

        public static Vector2 GetMoonPosition()
        {
            Texture2D moonTexture = TextureAssets.Moon[0].Value;

            int screenWidth = Main.screenWidth;
            int screenHeight = Main.screenHeight;
            Vector2 zero = Vector2.Zero;
            if (screenWidth < 800)
            {
                int smallWidth = 800 - screenWidth;
                zero.X -= smallWidth * 0.5f;
                screenWidth = 800;
            }
            if (screenHeight < 600)
            {
                int smallHeight = 600 - screenHeight;
                zero.Y -= smallHeight * 0.5f;
                screenHeight = 600;
            }
            Main.SceneArea sceneArea = new()
            {
                bgTopY = 0,
                totalWidth = screenWidth,
                totalHeight = screenHeight,
                SceneLocalScreenPositionOffset = zero
            };

            int xPos = (int)(Main.time / 32400.0 * (double)(sceneArea.totalWidth + (moonTexture.Width * 2))) - moonTexture.Width;
            float yTimeOffset = Pow((float)(Main.time / 32400f - 0.5f) * 2.0f, 2.0f);
            int yPos = (int)(sceneArea.bgTopY + yTimeOffset * 230.0 + 100.0);
            return new Vector2(xPos, yPos + Main.moonModY) + sceneArea.SceneLocalScreenPositionOffset;
        }

        private static void DrawFish()
        {
            int maxBoids = InfernumConfig.Instance.ReducedGraphicsConfig ? 90 : 180;

            // Spawn fishes.
            Rectangle spawnRectangle = new(50, 50, Main.screenWidth - 50, Main.screenHeight - 50);
            if (Main.rand.NextBool(2) && Fishes.Count < maxBoids)
                Fishes.Add(new(Main.rand.Next(600, 1200), Main.rand.NextFloat(0.15f, 0.2f), Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloat(-Main.screenWidth * 0.8f, Main.screenWidth * 0.8f),
                        Main.rand.NextFloat(-Main.screenHeight * 0.8f, Main.screenHeight * 0.8f)),
                        Main.rand.NextFloat(Tau).ToRotationVector2() * Main.rand.NextFloat(2f, 4f)));

            Fishes.RemoveAll(f => f.Time >= f.Lifetime);

            while (Fishes.Count > maxBoids)
                Fishes.RemoveAt(0);

            foreach (var fish in Fishes)
            {
                fish.Update();
                fish.Draw();
            }
        }

        private void DrawCinders(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            int maxCinders = InfernumConfig.Instance.ReducedGraphicsConfig ?  120 : 240;

            // Randomly spawn cinders.
            if (Main.rand.NextBool(5) && Cinders.Count < maxCinders)
            {
                bool downwards = Main.rand.NextBool(4);
                Cinders.Add(new Cinder()
                {
                    DrawPosition = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloat(-Main.screenWidth * 1.5f, Main.screenWidth * 1.5f),
                        Main.rand.NextFloat(Main.screenHeight * 0.5f, Main.screenHeight) * (downwards ? -1.2f : 1f)),
                    Velocity = Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.05f, 0.5f)) * Main.rand.NextFloat(1f, 1.5f) * (downwards ? 1f : -1f),
                    Lifetime = Main.rand.Next(2500, 3100),
                    Rotation = Main.rand.NextFloat(TwoPi),
                    RotationSpeed = Main.rand.NextFloat(0.003f, 0.006f) * Main.rand.NextFromList(-1, 1),
                    ColorLerpAmount = Main.rand.NextFloat(),
                    Depth = Main.rand.NextFloat(1.3f, 3f) * (downwards ? 0.8f : 1f),
                    Bubble = Main.rand.NextBool(downwards ? 10 : 3)
                });
            }

            while (Cinders.Count > maxCinders)
                Cinders.RemoveAt(0);

            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle rectangle = new(-1000, -1000, 4000, 4000);

            // Draw all cinders
            for (int i = 0; i < Cinders.Count; i++)
            {
                Cinder cinder = Cinders[i];
                cinder.Update();
                Texture2D cinderTexture = cinder.Bubble ? ModContent.Request<Texture2D>("CalamityMod/Particles/Bubble").Value : ModContent.Request<Texture2D>("CalamityMod/Particles/ThinSparkle").Value;
                if (cinder.Depth > minDepth && cinder.Depth < maxDepth * 2f)
                {
                    Vector2 scale = new Vector2(1f / cinder.Depth, 1f / cinder.Depth) * (cinder.Bubble ? 0.5f : 1f);
                    Vector2 position = (cinder.DrawPosition - screenCenter) * scale + screenCenter - Main.screenPosition;
                    if (rectangle.Contains((int)position.X, (int)position.Y))
                    {
                        Color lightColor = (cinder.Bubble ? Color.White : Color.Lerp(Color.SkyBlue, Color.Teal, cinder.ColorLerpAmount)) * cinder.Opacity * intensity;
                        Vector2 origin = cinderTexture.Size() * 0.5f;
                        spriteBatch.Draw(cinderTexture, position, null, lightColor with { A = 0 }, cinder.Rotation, origin, scale * cinder.Opacity, 0, 0f);
                    }
                }
            }

            Cinders.RemoveAll(s => s.Timer >= s.Lifetime);
        }
    }
}
