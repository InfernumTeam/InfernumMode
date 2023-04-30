using CalamityMod.NPCs;
using CalamityMod.NPCs.Yharon;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.Content.Subworlds;
using InfernumMode.Core.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class YharonSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            int yharonIndex = NPC.FindFirstNPC(ModContent.NPCType<Yharon>());
            bool transitioningToPhase2 = yharonIndex >= 0 && Main.npc[yharonIndex].ai[0] == (int)YharonBehaviorOverride.YharonAttackType.EnterSecondPhase;
            return (CalamityGlobalNPC.yharon != -1 || CalamityGlobalNPC.yharonP2 != -1 || transitioningToPhase2) && InfernumMode.CanUseCustomAIs;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Yharon", isActive);
        }
    }

    public class YharonSky : CustomSky
    {
        public class BackgroundSmoke
        {
            public int Time;

            public int Lifetime;

            public float Rotation;

            public Vector2 DrawPosition;

            public Vector2 Velocity;

            public Color SmokeColor;

            public void Update()
            {
                Time++;
                DrawPosition += Velocity;
                Velocity *= 0.983f;
                SmokeColor *= 0.997f;
                Rotation += Velocity.X * 0.01f;
            }
        }

        private float intensity;

        private float phase2VariantInterpolant;

        private bool isActive;

        public static List<BackgroundSmoke> SmokeParticles = new();

        public override void Activate(Vector2 position, params object[] args) => isActive = true;

        public override void Deactivate(params object[] args) => isActive = false;

        public override void Reset() => isActive = false;

        public override bool IsActive() => isActive || intensity > 0f;

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f)
                intensity = MathHelper.Clamp(intensity + 0.005f, 0f, 1f);
            else if (!isActive && intensity > 0f)
                intensity = MathHelper.Clamp(intensity - 0.005f, 0f, 1f);

            phase2VariantInterpolant = MathHelper.Clamp(phase2VariantInterpolant + YharonBehaviorOverride.InSecondPhase.ToDirectionInt() * 0.007f, 0f, intensity + 0.0001f);

            // Kill every cloud.
            for (int i = 0; i < Main.maxClouds; i++)
                Main.cloud[i].kill = true;

            // Randomly emit smoke.
            int smokeReleaseChance = 3;
            if (Main.rand.NextBool(smokeReleaseChance))
            {
                for (int i = 0; i < 8; i++)
                {
                    SmokeParticles.Add(new()
                    {
                        DrawPosition = new Vector2(Main.rand.NextFloat(-400f, Main.screenWidth + 400f), Main.screenHeight + 250f),
                        Velocity = -Vector2.UnitY * Main.rand.NextFloat(5f, 23f) + Main.rand.NextVector2Circular(3f, 3f),
                        SmokeColor = Color.Lerp(Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(0.5f, 0.85f)) * 0.67f,
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                        Lifetime = Main.rand.Next(120, 480)
                    });
                }
            }

            // Update smoke particles.
            SmokeParticles.RemoveAll(s => s.Time >= s.Lifetime);
            foreach (BackgroundSmoke smoke in SmokeParticles)
                smoke.Update();
        }

        public static void CreateSmokeBurst()
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSmallSound with { Volume = 0.67f });

            for (int i = 0; i < 85; i++)
            {
                SmokeParticles.Add(new()
                {
                    DrawPosition = new Vector2(Main.rand.NextFloat(-400f, Main.screenWidth + 400f), Main.screenHeight + 250f),
                    Velocity = -Vector2.UnitY * Main.rand.NextFloat(9f, 25f) + Main.rand.NextVector2Circular(4f, 4f),
                    SmokeColor = Color.LightGray,
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                    Lifetime = Main.rand.Next(90, 240)
                });
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Force it to be sunset.
            Main.time = MathHelper.Lerp((float)Main.time, (float)Main.dayLength * 0.5f, 0.01f);
            Main.dayTime = true;

            // Draw the sky, sun, and smoke.
            if (maxDepth >= 0f && minDepth < 0f)
            {
                Texture2D skyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/YharonSky").Value;
                Texture2D skyTextureP2 = ModContent.Request<Texture2D>("InfernumMode/Content/Skies/YharonSkyP2").Value;
                spriteBatch.Draw(skyTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Lerp(Color.White, Color.Gray, 0.33f) * intensity * (1f - phase2VariantInterpolant) * 0.63f);
                spriteBatch.Draw(skyTextureP2, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * intensity * phase2VariantInterpolant * 0.83f);

                DrawVibrantSun();
                DrawSmoke();
            }
        }

        public void DrawVibrantSun()
        {
            // Use additive drawing.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);

            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            Vector2 origin = backglowTexture.Size() * 0.5f;
            float opacity = intensity * MathHelper.Lerp(0.73f, 0.76f, MathF.Sin(Main.GlobalTimeWrappedHourly * 29f) * 0.5f + 0.5f);
            Vector2 sunDrawPosition = DrawLostColosseumBackgroundHook.SunPosition;
            Main.spriteBatch.Draw(backglowTexture, sunDrawPosition, null, Color.Wheat * opacity * 0.93f, 0f, origin, 1f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, sunDrawPosition, null, Color.Yellow * opacity * 0.72f, 0f, origin, 3f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, sunDrawPosition, null, Color.Orange * opacity * 0.66f, 0f, origin, 6f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, sunDrawPosition, null, Color.Red * opacity * 0.7f, 0f, origin, 12f, 0, 0f);

            // Draw the regular sun.
            var sceneArea = DrawLostColosseumBackgroundHook.SunSceneArea;
            typeof(Main).GetMethod("DrawSunAndMoon", Utilities.UniversalBindingFlags).Invoke(Main.instance, new object[]
            {
                sceneArea,
                Color.White * intensity,
                Color.White * intensity,
                0f
            });
        }

        public void DrawSmoke()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);

            Texture2D smokeTexture = InfernumTextureRegistry.Smoke.Value;
            foreach (BackgroundSmoke smoke in SmokeParticles)
                Main.spriteBatch.Draw(smokeTexture, smoke.DrawPosition, null, smoke.SmokeColor * 0.56f, smoke.Rotation, smokeTexture.Size() * 0.5f, 1f, 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.BackgroundViewMatrix.EffectMatrix);
        }
    }
}
