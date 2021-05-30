using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        private static readonly FieldInfo shaderTextureField = typeof(MiscShaderData).GetField("_uImage", BindingFlags.NonPublic | BindingFlags.Instance);

        // Use reflection to set the image. Its underlying data is private and the only way to change it publicly
        // is via a method that only accepts paths to vanilla textures.
        public static void SetShaderTexture(this MiscShaderData shader, Texture2D texture) => shaderTextureField.SetValue(shader, new Ref<Texture2D>(texture));

        /// <summary>
        /// Prepares a <see cref="SpriteBatch"/> for shader-based drawing.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        public static void EnterShaderRegion(this SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Ends changes to a <see cref="SpriteBatch"/> based on shader-based drawing in favor of typical draw begin states.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        public static void ExitShaderRegion(this SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Sets a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> arbitrarily.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void SetBlendState(this SpriteBatch spriteBatch, BlendState blendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, blendState, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Reset's a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> based to a typical <see cref="BlendState.AlphaBlend"/>.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void ResetBlendState(this SpriteBatch spriteBatch) => spriteBatch.SetBlendState(BlendState.AlphaBlend);

        /// <summary>
        /// Draws a line significantly more efficiently than <see cref="Utils.DrawLine(SpriteBatch, Vector2, Vector2, Color, Color, float)"/> using just one scaled line texture. Positions are automatically converted to screen coordinates.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch by which the line should be drawn.</param>
        /// <param name="start">The starting point of the line in world coordinates.</param>
        /// <param name="end">The ending point of the line in world coordinates.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="width">The width of the line.</param>
        public static void DrawLineBetter(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            // Draw nothing if the start and end are equal, to prevent division by 0 problems.
            if (start == end)
                return;

            start -= Main.screenPosition;
            end -= Main.screenPosition;

            Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/Line");
            float rotation = (end - start).ToRotation();
            Vector2 scale = new Vector2(Vector2.Distance(start, end) / line.Width, width);

            spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Creates a generic dust explosion at a given position.
        /// </summary>
        /// <param name="spawnPosition">The place to spawn dust at.</param>
        /// <param name="dustType">The dust ID to use.</param>
        /// <param name="dustPerBurst">The amount of dust to spawn per burst.</param>
        /// <param name="burstSpeed">The speed of the dust when exploding.</param>
        /// <param name="baseScale">The scale of the dust</param>
        public static void CreateGenericDustExplosion(Vector2 spawnPosition, int dustType, int dustPerBurst, float burstSpeed, float baseScale)
        {
            // Generate a dust explosion
            float burstDirectionVariance = 3;
            for (int j = 0; j < 10; j++)
            {
                burstDirectionVariance += j * 2;
                for (int k = 0; k < dustPerBurst; k++)
                {
                    Dust burstDust = Dust.NewDustPerfect(spawnPosition, dustType);
                    burstDust.scale = baseScale * Main.rand.NextFloat(0.8f, 1.2f);
                    burstDust.position = spawnPosition + Main.rand.NextVector2Circular(10f, 10f);
                    burstDust.velocity = Main.rand.NextVector2Square(-burstDirectionVariance, burstDirectionVariance).SafeNormalize(Vector2.UnitY) * burstSpeed;
                    burstDust.noGravity = true;
                }
                burstSpeed += 3f;
            }
        }
    }
}
