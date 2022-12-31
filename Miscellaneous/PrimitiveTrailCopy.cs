using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode
{
    public class PrimitiveTrailCopy
    {
        public struct VertexPosition2DColor : IVertexType
        {
            public Vector2 Position;
            public Color Color;
            public Vector2 TextureCoordinates;
            public VertexDeclaration VertexDeclaration => _vertexDeclaration;

            private static readonly VertexDeclaration _vertexDeclaration = new(new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            });
            public VertexPosition2DColor(Vector2 position, Color color, Vector2 textureCoordinates)
            {
                Position = position;
                Color = color;
                TextureCoordinates = textureCoordinates;
            }
        }

        internal Matrix? PerspectiveMatrixOverride = null;

        public delegate float VertexWidthFunction(float completionRatio);
        public delegate Vector2 VertexOffsetFunction(float completionRatio);
        public delegate Color VertexColorFunction(float completionRatio);

        public VertexWidthFunction WidthFunction;
        public VertexColorFunction ColorFunction;
        public VertexOffsetFunction OffsetFunction;

        // NOTE: Beziers can be laggy when a lot of control points are used, since our implementation
        // uses a recursive Lerp that gets more computationally expensive the more original indices.
        // n(n - 1)/2 linear interpolations to be precise, where n is the amount of original indices.
        public bool UsesSmoothening;
        public BasicEffect BaseEffect;
        public MiscShaderData SpecialShader;

        public PrimitiveTrailCopy(VertexWidthFunction widthFunction, VertexColorFunction colorFunction, VertexOffsetFunction offsetFunction = null, bool useSmoothening = true, MiscShaderData specialShader = null)
        {
            if (widthFunction is null || colorFunction is null)
                throw new NullReferenceException($"In order to create a primitive trail, a non-null {(widthFunction is null ? "width" : "color")} function must be specified.");
            WidthFunction = widthFunction;
            ColorFunction = colorFunction;
            OffsetFunction = offsetFunction;

            UsesSmoothening = useSmoothening;

            if (specialShader != null)
                SpecialShader = specialShader;

            BaseEffect = new BasicEffect(Main.instance.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = false
            };
            UpdateBaseEffect(out _, out _);
        }

        public void UpdateBaseEffect(out Matrix effectProjection, out Matrix effectView)
        {
            // Screen bounds.
            int height = Main.instance.GraphicsDevice.Viewport.Height;

            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position.
            effectView *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            effectView *= Matrix.CreateRotationZ(MathHelper.Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f)
                effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            effectView *= zoomScaleMatrix;

            effectProjection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f) * zoomScaleMatrix;
            BaseEffect.View = effectView;
            BaseEffect.Projection = effectProjection;
        }

        public List<Vector2> GetTrailPoints(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints)
        {
            // Don't smoothen the points unless explicitly told do so.
            if (!UsesSmoothening)
            {
                List<Vector2> basePoints = originalPositions.Where(originalPosition => originalPosition != Vector2.Zero).ToList();
                List<Vector2> endPoints = new();

                if (basePoints.Count < 3)
                    return endPoints;

                // Remap the original positions across a certain length.
                for (int i = 0; i < basePoints.Count; i++)
                {
                    Vector2 offset = generalOffset;
                    if (OffsetFunction != null)
                        offset += OffsetFunction(i / (float)(basePoints.Count - 1f));

                    endPoints.Add(basePoints[i] + offset);
                }
                return endPoints;
            }

            List<Vector2> controlPoints = new();
            for (int i = 0; i < originalPositions.Count(); i++)
            {
                // Don't incorporate points that are zeroed out.
                // They are almost certainly a result of incomplete oldPos arrays.
                if (originalPositions.ElementAt(i) == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)originalPositions.Count();
                Vector2 offset = generalOffset;
                if (OffsetFunction != null)
                    offset += OffsetFunction(completionRatio);
                controlPoints.Add(originalPositions.ElementAt(i) + offset);
            }

            if (controlPoints.Count <= 1)
                return controlPoints;
            
            List<Vector2> points = new();

            // Round up the trail point count to the nearest multiple of the position count, to ensure that interpolants work.
            int splineIterations = (int)Math.Ceiling(totalTrailPoints / (double)controlPoints.Count);
            totalTrailPoints = splineIterations * totalTrailPoints;

            // The GetPoints method uses imprecise floating-point looping, which can result in inaccuracies with point generation.
            // Instead, an integer-based loop is used to mitigate such problems.
            for (int i = 1; i < controlPoints.Count - 2; i++)
            {
                for (int j = 0; j < splineIterations; j++)
                {
                    float splineInterpolant = j / (float)splineIterations;
                    if (splineIterations <= 1f)
                        splineInterpolant = 0.5f;

                    points.Add(Vector2.CatmullRom(controlPoints[i - 1], controlPoints[i], controlPoints[i + 1], controlPoints[i + 2], splineInterpolant));
                }
            }

            // Manually insert the front and end points.
            points.Insert(0, controlPoints.First());
            points.Add(controlPoints.Last());

            return points;
        }

        public VertexPosition2DColor[] GetVerticesFromTrailPoints(List<Vector2> trailPoints, float? directionOverride = null)
        {
            List<VertexPosition2DColor> vertices = new();

            for (int i = 0; i < trailPoints.Count - 1; i++)
            {
                float completionRatio = i / (float)trailPoints.Count;
                float widthAtVertex = WidthFunction(completionRatio);
                Color vertexColor = ColorFunction(completionRatio);

                Vector2 currentPosition = trailPoints[i];
                Vector2 positionAhead = trailPoints[i + 1];
                Vector2 directionToAhead = (positionAhead - trailPoints[i]).SafeNormalize(Vector2.Zero);
                if (directionOverride.HasValue)
                    directionToAhead = directionOverride.Value.ToRotationVector2();

                Vector2 leftCurrentTextureCoord = new(completionRatio, 0f);
                Vector2 rightCurrentTextureCoord = new(completionRatio, 1f);

                // Point 90 degrees away from the direction towards the next point, and use it to mark the edges of the rectangle.
                // This doesn't use RotatedBy for the sake of performance (there can potentially be a lot of trail points).
                Vector2 sideDirection = new(-directionToAhead.Y, directionToAhead.X);

                // What this is doing, at its core, is defining a rectangle based on two triangles.
                // These triangles are defined based on the width of the strip at that point.
                // The resulting rectangles combined are what make the trail itself.
                vertices.Add(new VertexPosition2DColor(currentPosition - sideDirection * widthAtVertex, vertexColor, leftCurrentTextureCoord));
                vertices.Add(new VertexPosition2DColor(currentPosition + sideDirection * widthAtVertex, vertexColor, rightCurrentTextureCoord));
            }

            return vertices.ToArray();
        }

        public static short[] GetIndicesFromTrailPoints(int pointCount)
        {
            // What this is doing is basically representing each point on the vertices list as
            // indices. These indices should come together to create a tiny rectangle that acts
            // as a segment on the trail. This is achieved here by splitting the indices (or rather, points)
            // into 2 triangles, which requires 6 points.
            // The logic here basically determines which indices are connected together.
            int totalIndices = (pointCount - 1) * 6;
            short[] indices = new short[totalIndices];
            
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

        public void SpecifyPerspectiveMatrix(Matrix m) => PerspectiveMatrixOverride = m;

        public void Draw(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, float? directionOverride = null)
        {
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            List<Vector2> trailPoints = GetTrailPoints(originalPositions, generalOffset, totalTrailPoints);

            // A trail with only one point or less has nothing to connect to, and therefore, can't make a trail.
            if (originalPositions.Count() <= 2 || trailPoints.Count <= 2)
                return;

            // If the trail point has any NaN positions, don't draw anything.
            if (trailPoints.Any(point => point.HasNaNs()))
                return;

            // If the trail points are all equal, don't draw anything.
            if (trailPoints.All(point => point == trailPoints[0]))
                return;

            UpdateBaseEffect(out Matrix projection, out Matrix view);
            VertexPosition2DColor[] vertices = GetVerticesFromTrailPoints(trailPoints, directionOverride);
            short[] triangleIndices = GetIndicesFromTrailPoints(trailPoints.Count);

            if (triangleIndices.Length % 6 != 0 || vertices.Length <= 3)
                return;

            if (SpecialShader != null)
            {
                SpecialShader.Shader.Parameters["uWorldViewProjection"].SetValue(PerspectiveMatrixOverride ?? (view * projection));
                SpecialShader.Apply();
                PerspectiveMatrixOverride = null;
            }
            else
                BaseEffect.CurrentTechnique.Passes[0].Apply();

            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, triangleIndices, 0, triangleIndices.Length / 3);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}