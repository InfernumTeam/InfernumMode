using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace InfernumMode.Common.Graphics
{
    public class Primitive3DStrip
    {
        internal VertexHeightFunction HeightFunction;

        internal VertexColorFunction ColorFunction;

        internal Asset<Texture2D> BandTexture;

        internal BasicEffect BaseEffect;

        public delegate float VertexHeightFunction(float completionRatio);

        public delegate Color VertexColorFunction(float completionRatio);

        public Primitive3DStrip(VertexHeightFunction heightFunction, VertexColorFunction colorFunction)
        {
            if (heightFunction is null || colorFunction is null)
                throw new NullReferenceException($"In order to create a primitive 3D strip, a non-null {(heightFunction is null ? "height" : "color")} function must be specified.");

            HeightFunction = heightFunction;
            ColorFunction = colorFunction;
            BaseEffect = new BasicEffect(Main.instance.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = true
            };
            UpdateBaseEffect(out _, out _);
        }

        public void UpdateBaseEffect(out Matrix effectProjection, out Matrix effectView)
        {
            CalamityUtils.CalculatePerspectiveMatricies(out effectView, out effectProjection);
            BaseEffect.View = effectView;
            BaseEffect.Projection = effectProjection;
        }

        public void UseBandTexture(Asset<Texture2D> bandTexture) => BandTexture = bandTexture;

        internal VertexPositionColorTexture[] CalculateVertices(Vector2 left, Vector2 right, float textureScrollSpeed, float verticalWobble, float wobblePhaseShift)
        {
            int vertexCount = 256;
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[vertexCount * 2];

            for (int i = 0; i < vertexCount; i++)
            {
                // Calculates the coordinates of a rectangle wrapped as a circle in 3D space.
                float completionRatio = i / (float)vertexCount;
                float angleCompletionRatio = MathHelper.TwoPi * completionRatio;
                float x = MathHelper.Lerp(left.X, right.X, (float)Math.Cos(angleCompletionRatio) * 0.5f + 0.5f);
                float z = (float)Math.Sin(angleCompletionRatio + wobblePhaseShift);
                float y = MathHelper.Lerp(left.Y, right.Y, (float)Math.Pow(Math.Sin(angleCompletionRatio * 0.5f), 2D)) + z * verticalWobble;
                float height = HeightFunction(z * 0.5f + 0.5f);
                Color color = ColorFunction(z * 0.5f + 0.5f);

                Vector3 center = new(x, y, 1f);
                VertexPositionColorTexture top = new(center - Vector3.UnitY * height, color, new((completionRatio + Main.GlobalTimeWrappedHourly * textureScrollSpeed) % 1f, 0f));
                VertexPositionColorTexture bottom = new(center + Vector3.UnitY * height, color, new((completionRatio + Main.GlobalTimeWrappedHourly * textureScrollSpeed) % 1f, 1f));

                vertices[i * 2] = top;
                vertices[i * 2 + 1] = bottom;
            }

            return vertices;
        }

        internal static short[] CalculateIndices(VertexPositionColorTexture[] vertices)
        {
            int pointCount = vertices.Length / 2;
            int totalIndices = (pointCount - 1) * 6;
            short[] indices = new short[totalIndices];

            // Refer to the primitive drawer/drawcode doc I wrote for more details on this.
            for (int i = 0; i < pointCount - 2; i++)
            {
                int startingTriangleIndex = i * 6;
                int connectToIndex = i * 2;
                indices[startingTriangleIndex] = (short)connectToIndex;
                indices[startingTriangleIndex + 1] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 2] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 3] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 4] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 5] = (short)(connectToIndex + 3);
            }

            return indices;
        }

        public void Draw(Vector2 left, Vector2 right, float textureScrollSpeed, float verticalWobble, float wobblePhaseShift)
        {
            UpdateBaseEffect(out _, out _);

            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            if (BandTexture is not null)
                BaseEffect.Texture = BandTexture.Value;
            BaseEffect.CurrentTechnique.Passes[0].Apply();

            var vertices = CalculateVertices(left, right, textureScrollSpeed, verticalWobble, wobblePhaseShift);
            var triangleIndices = CalculateIndices(vertices);
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, triangleIndices, 0, triangleIndices.Length / 3);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}