using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class CosmicBackgroundSystem : ModSystem
    {
        public static RenderTarget2D KalisetFractal
        {
            get;
            internal set;
        }

        public static float IdealExtraIntensity
        {
            get;
            set;
        }

        internal static float ExtraIntensity
        {
            get;
            set;
        }

        public static bool EffectIsActive => CalamityGlobalNPC.DoGHead != -1 && InfernumMode.CanUseCustomAIs && !Main.gameMenu && !InfernumConfig.Instance.ReducedGraphicsConfig;

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(PrepareTarget);
        }

        internal static void PrepareTarget()
        {
            int width = 4096;
            int height = 4096;
            int iterations = 14;
            KalisetFractal = new(Main.instance.GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);

            // Evolve the system based on the Kaliset.
            // Over time it will achieve very, very chaotic behavior similar to a fractal and as such is incredibly reliable for
            // getting pseudo-random change over time.
            float julia = 0.584f;

            new Thread(_ =>
            {
                float[] kalisetData = new float[width * height];

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        float previousDistance = 0f;
                        float totalChange = 0f;
                        Vector2 p = new(i / (float)width - 0.5f, j / (float)height - 0.5f);

                        for (int k = 0; k < iterations; k++)
                        {
                            p = new Vector2(Math.Abs(p.X), Math.Abs(p.Y)) / Vector2.Dot(p, p);
                            p.X -= julia;
                            p.Y -= julia;

                            float distance = p.Length();
                            totalChange += Math.Abs(distance - previousDistance);
                            previousDistance = distance;
                        }

                        if (float.IsNaN(totalChange) || float.IsInfinity(totalChange) || totalChange >= 1000f)
                            totalChange = 1000f;

                        kalisetData[i + j * width] = totalChange;
                    }
                }

                Main.QueueMainThreadAction(() => KalisetFractal.SetData(kalisetData));
            }).Start();
        }

        public static void Draw()
        {
            // Make the intensity dissipate.
            if (IdealExtraIntensity != 0f)
            {
                ExtraIntensity = MathHelper.Lerp(ExtraIntensity, IdealExtraIntensity, 0.09f);
                if (MathHelper.Distance(ExtraIntensity, IdealExtraIntensity) <= 0.01f)
                    IdealExtraIntensity = 0f;
            }
            else
                ExtraIntensity *= 0.96f;

            if (CalamityGlobalNPC.DoGHead == -1)
                return;

            NPC dog = Main.npc[CalamityGlobalNPC.DoGHead];
            float intensity = ExtraIntensity + (DoGPhase2HeadBehaviorOverride.InPhase2 ? 0.55f : 0.4f);
            intensity += dog.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.DeathAnimationTimerIndex] * 0.01f;

            Vector2 scale = new Vector2(Main.screenWidth, Main.screenWidth) / TextureAssets.MagicPixel.Value.Size() * Main.GameViewMatrix.Zoom * 2f;
            
            Main.instance.GraphicsDevice.Textures[1] = KalisetFractal;
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["zoom"].SetValue(0.5f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["brightness"].SetValue(intensity);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["scrollSpeedFactor"].SetValue(0.0024f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["frontStarColor"].SetValue(Color.DarkGreen.ToVector3() * 0.95f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["backStarColor"].SetValue(Color.Fuchsia.ToVector3() * 0.5f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Apply();

            // Screen shader? What screen shader?
            Filters.Scene["CalamityMod:DevourerofGodsHead"].GetShader().UseOpacity(0f);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, 0, 0f);
        }
    }
}