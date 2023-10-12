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
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.ScreenEffects
{
    public class CosmicBackgroundSystem : ModSystem
    {
        public static ManagedRenderTarget KalisetFractal
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

        public static float MonolithIntensity
        {
            get;
            set;
        }

        public static bool EffectIsActive
        {
            get
            {
                if (Main.gameMenu || InfernumConfig.Instance.ReducedGraphicsConfig)
                    return false;

                if (Main.LocalPlayer.Infernum_Biome().CosmicBackgroundEffect)
                    return true;

                bool dogCondition = CalamityGlobalNPC.DoGHead != -1 && InfernumMode.CanUseCustomAIs;
                return dogCondition;
            }
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(PrepareTarget);
        }

        public override void OnModUnload()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                if (KalisetFractal is not null && !KalisetFractal.IsDisposed)
                    KalisetFractal.Dispose();

                KalisetFractal = null;
            });
        }

        internal static void PrepareTarget()
        {
            int width = 2048;
            int height = 2048;

            int iterations = 14;

            // This is stored as a render target and not a PNG in the mod's source because the fractal needs to contain information that exceeds the traditional range of 0-1 color values.
            // It could theoretically be loaded into a binary file in some way but at that point you're going to need to translate it into some GPU-friendly object, like a render target.
            // It's easiest to just create it dynamically here.
            // There are a LOT of calculations needed to generate the entire texture though, hence the usage of background threads.
            KalisetFractal = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
            }, false);

            // This number is highly important to the resulting structure of the fractal, and is very sensitive (as is typically the case with objects from Chaos Theory).
            // Many numbers will give no fractal at all, pure white, or pure black. But tweaking it to be just right gives some amazing patterns.
            // Feel free to tweak this if you want to see what it does to the texture.
            float julia = 0.584f;

            new Thread(_ =>
            {
                float[] kalisetData = new float[width * height];

                // Evolve the system based on the Kaliset.
                // Over time it will achieve very, very chaotic behavior similar to a fractal and as such is incredibly reliable for
                // getting pseudo-random change over time.
                for (int i = 0; i < width * height; i++)
                {
                    int x = i % width;
                    int y = i / width;
                    float previousDistance = 0f;
                    float totalChange = 0f;
                    Vector2 p = new(x / (float)width - 0.5f, y / (float)height - 0.5f);

                    // Repeat the iterative function of 'abs(z) / dot(z) - c' multiple times to generate the fractal patterns.
                    // The higher the amount of iterations, the greater amount of detail. Do note that too much detail can lead to grainy artifacts
                    // due to individual pixels being unusually bright next to their neighbors, as the fractal inevitably becomes so detailed that the
                    // texture cannot encompass all of its features.
                    for (int j = 0; j < iterations; j++)
                    {
                        p = new Vector2(Math.Abs(p.X), Math.Abs(p.Y)) / Vector2.Dot(p, p);
                        p.X -= julia;
                        p.Y -= julia;

                        float distance = p.Length();
                        totalChange += Math.Abs(distance - previousDistance);
                        previousDistance = distance;
                    }

                    // Sometimes the results of the above iterative process will send the distance so far off that the numbers explode into the NaN or Infinity range.
                    // The GPU won't know what to do with this and will just act like it's a black pixel, which we don't want.
                    // As such, this check exists to put a hard limit on the values sent into the fractal texture. Something beyond 1000 shouldn't be making a difference anyway.
                    // At that point the pixel it spits out from the shader should be a pure white.
                    if (float.IsNaN(totalChange) || float.IsInfinity(totalChange) || totalChange >= 1000f)
                        totalChange = 1000f;

                    kalisetData[i] = totalChange;
                }

                Main.QueueMainThreadAction(() => KalisetFractal.Target.SetData(kalisetData));
            }).Start();
        }

        public static void Draw()
        {
            // Make the intensity dissipate.
            if (IdealExtraIntensity != 0f)
            {
                ExtraIntensity = Lerp(ExtraIntensity, IdealExtraIntensity, 0.09f);
                if (Distance(ExtraIntensity, IdealExtraIntensity) <= 0.01f)
                    IdealExtraIntensity = 0f;
            }
            else
                ExtraIntensity *= 0.96f;

            if (!EffectIsActive && MonolithIntensity <= 0f)
                return;

            float intensity = ExtraIntensity + (DoGPhase2HeadBehaviorOverride.InPhase2 ? 0.55f : 0.4f);

            if (CalamityGlobalNPC.DoGHead != -1)
            {
                NPC dog = Main.npc[CalamityGlobalNPC.DoGHead];
                intensity += dog.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.DeathAnimationTimerIndex] * 0.01f;
            }
            else
            {
                if (EffectIsActive)
                    MonolithIntensity = Clamp(MonolithIntensity + 0.01f, 0f, 1f);
                intensity *= MonolithIntensity;
            }

            Vector2 scale = new Vector2(Main.screenWidth, Main.screenWidth) / TextureAssets.MagicPixel.Value.Size() * 2f;

            Main.instance.GraphicsDevice.Textures[1] = KalisetFractal.Target;
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["zoom"].SetValue(0.5f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["brightness"].SetValue(intensity);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["scrollSpeedFactor"].SetValue(0.0024f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["frontStarColor"].SetValue(Color.Lerp(Color.ForestGreen, Color.Cyan, 0.4f).ToVector3() * 0.6f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Shader.Parameters["backStarColor"].SetValue(Color.Fuchsia.ToVector3() * 0.5f);
            InfernumEffectsRegistry.CosmicBackgroundShader.Apply();

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, 0, 0f);
        }
    }
}
