using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace InfernumMode.Common.Graphics.Fluids
{
    public class FluidFieldInfernum
    {
        internal ManagedRenderTarget ColorTarget;

        internal ManagedRenderTarget VelocityTarget;

        internal ManagedRenderTarget TempTarget;

        public bool IsDisposing
        {
            get;
            private set;
        }

        public int MovementUpdateSteps
        {
            get;
            set;
        } = 1;

        public bool ShouldUpdate
        {
            get;
            set;
        }

        public FluidFieldProperties Properties
        {
            get;
            set;
        }

        public Point Center => new(Width / 2, Height / 2);

        public readonly int Width;

        public readonly int Height;

        public RenderTarget2D FluidCreateCondition(int width, int height) =>
            new(Main.instance.GraphicsDevice, Width, Height, false, SurfaceFormat.Vector4, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);

        public FluidFieldInfernum(int width, int height, FluidFieldProperties properties)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Width = width;
            Height = height;
            Properties = properties;

            // Initialize targets.
            var graphics = Main.instance.GraphicsDevice;
            ColorTarget = new(false, FluidCreateCondition);
            VelocityTarget = new(false, FluidCreateCondition);
            TempTarget = new(false, FluidCreateCondition);

            // Store the field in the cache.
            FluidSimulatorManagementSystem.CreatedFields.Add(this);
        }

        internal void PerformUpdateStep()
        {
            for (int i = 0; i < MovementUpdateSteps; i++)
                PerformPassToTarget("VelocityUpdatePass", VelocityTarget.Target);
            PerformPassToTarget("VelocityUpdateVorticityPass", VelocityTarget.Target);
            PerformPassToTarget("DiffusePass", VelocityTarget.Target, Properties.VelocityDiffusion);

            PerformPassToTarget("DiffusePass", ColorTarget.Target, Properties.ColorDiffusion);
            PerformPassToTarget("AdvectPass", ColorTarget.Target);
        }

        internal void PerformPassToTarget(string passName, RenderTarget2D target, float viscosity = 0f)
        {
            var graphics = Main.instance.GraphicsDevice;
            var fluidShader = GameShaders.Misc["Infernum:FluidAdvect"].Shader;

            graphics.SetRenderTarget(TempTarget.Target);
            graphics.Clear(Color.Transparent);

            // Draw the target to the temp target with the shader effect.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            graphics.Textures[0] = target;
            graphics.Textures[1] = VelocityTarget.Target;
            graphics.Textures[2] = ColorTarget.Target;
            fluidShader.Parameters["simulationArea"].SetValue(new Vector2(Width, Height));
            fluidShader.Parameters["viscosity"].SetValue(viscosity);
            fluidShader.Parameters["vorticityAmount"].SetValue(Properties.VorticityAmount);
            fluidShader.Parameters["densityClumpingFactor"].SetValue(Properties.DensityClumpingFactor);
            fluidShader.Parameters["densityDecayFactor"].SetValue(Properties.DensityDecayFactor);
            fluidShader.Parameters["velocityPersistence"].SetValue(Properties.VelocityPersistence);
            fluidShader.Parameters["decelerationFactor"].SetValue(Properties.DecelerationFactor);

            if (passName == "AdvectPass")
                GameShaders.Misc["Infernum:FluidAdvect"].Apply();
            else if (passName == "VelocityUpdatePass")
                GameShaders.Misc["Infernum:FluidUpdateVelocity"].Apply();
            else if (passName == "VelocityUpdateVorticityPass")
                GameShaders.Misc["Infernum:FluidUpdateVelocityVorticity"].Apply();
            else
                fluidShader.CurrentTechnique.Passes[passName].Apply();
            Main.spriteBatch.Draw(target, Vector2.Zero, Color.White);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            // Set the temp target's contents to the original target.
            graphics.SetRenderTarget(target);
            Main.spriteBatch.Draw(TempTarget.Target, Vector2.Zero, Color.White);

            // Return to the backbuffer.
            graphics.SetRenderTarget(null);
        }

        public void Draw(Vector2 drawCenter, float scale = 1f, float colorInterpolateSharpness = 0f, params Vector4[] fadeColors)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var graphics = Main.instance.GraphicsDevice;
            Texture2D pixel = InfernumTextureRegistry.Pixel.Value;

            graphics.Textures[1] = VelocityTarget.Target;
            graphics.Textures[2] = ColorTarget.Target;
            GameShaders.Misc["Infernum:DrawFluidResult"].Shader.Parameters["simulationArea"].SetValue(new Vector2(Width, Height));
            GameShaders.Misc["Infernum:DrawFluidResult"].Shader.Parameters["colorInterpolateSharpness"].SetValue(colorInterpolateSharpness);
            GameShaders.Misc["Infernum:DrawFluidResult"].Shader.Parameters["lifetimeFadeStops"].SetValue(fadeColors.Length);
            GameShaders.Misc["Infernum:DrawFluidResult"].Shader.Parameters["lifetimeFadeColors"].SetValue(fadeColors);
            GameShaders.Misc["Infernum:DrawFluidResult"].Apply();

            Main.spriteBatch.Draw(pixel, drawCenter, null, Color.White, 0f, pixel.Size() * 0.5f, new Vector2(Width, Height) * scale, 0, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }

        public void CreateSource(Point sourceCenter, Vector2 area, Vector2 velocity, Color color, float density)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            int totalElements = (int)(area.X * area.Y);
            Vector4[] velocities = new Vector4[totalElements];
            Vector4[] colors = new Vector4[totalElements];
            for (int i = 0; i < totalElements; i++)
            {
                velocities[i] = new(velocity.X, velocity.Y, density, 1f);
                colors[i] = color.ToVector4() with { W = 0f };
            }

            Rectangle areaRect = new(sourceCenter.X + (int)area.X / 2, sourceCenter.Y + (int)area.Y / 2, (int)area.X, (int)area.Y);

            Main.RunOnMainThread(() =>
            {
                VelocityTarget.Target.SetData(0, areaRect, velocities, 0, totalElements);
                ColorTarget.Target.SetData(0, areaRect, colors, 0, totalElements);
            });
        }
    }
}
