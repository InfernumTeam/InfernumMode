using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Primitives
{
    public class PrimitiveBatchingSystem : ModSystem
    {
        internal class PrimitiveBatch
        {
            public PrimitiveTrailCopy PrimitiveDrawer;

            public List<short> Indices;

            public List<PrimitiveTrailCopy.VertexPosition2DColor> Vertices;

            public void Draw()
            {
                PrimitiveDrawer.DrawPrimsFromVertexData(Vertices, Indices, false);
                Indices.Clear();
                Vertices.Clear();
            }
        }

        internal static Dictionary<Type, PrimitiveBatch> Batches = [];

        public override void OnModLoad()
        {
            Batches = [];
            On_Main.DrawProjectiles += DrawBatches;
        }

        private void DrawBatches(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type > ProjectileID.None && p.ModProjectile != null)
                    Draw(p.ModProjectile.GetType());
            }

            Main.spriteBatch.End();
        }

        public static bool BatchIsRegistered<T>() where T : ModType =>
            Batches.ContainsKey(typeof(T));

        public static void PrepareBatch<T>(PrimitiveTrailCopy trail) where T : ModType
        {
            Type type = typeof(T);

            // Don't create the batch if one already exists.
            if (Batches.ContainsKey(type))
                return;

            Batches[type] = new()
            {
                PrimitiveDrawer = trail,
                Indices = [],
                Vertices = []
            };
        }

        public static void PrepareVertices<T>(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, float? directionOverride = null) where T : ModType
        {
            if (originalPositions.Count() <= 2)
                return;

            // Don't attempt to prepare anything if the batch has not been created yet.
            Type type = typeof(T);
            if (!Batches.TryGetValue(type, out PrimitiveBatch batch))
                return;

            originalPositions = originalPositions.Where(p => p != Vector2.Zero);
            List<Vector2> trailPoints = batch.PrimitiveDrawer.GetTrailPoints(originalPositions, generalOffset, totalTrailPoints);

            // A trail with only one point or less has nothing to connect to, and therefore, can't make a trail.
            if (trailPoints.Count <= 2)
                return;

            // If the trail point has any NaN positions, don't draw anything.
            if (trailPoints.Any(point => point.HasNaNs()))
                return;

            // If the trail points are all equal, don't draw anything.
            if (trailPoints.All(point => point == trailPoints[0]))
                return;

            List<PrimitiveTrailCopy.VertexPosition2DColor> vertices = batch.PrimitiveDrawer.GetVerticesFromTrailPoints(trailPoints, directionOverride);
            List<short> triangleIndices = PrimitiveTrailCopy.GetIndicesFromTrailPoints(trailPoints.Count);
            PrepareVertices<T>(triangleIndices, vertices);
        }

        public static void PrepareVertices<T>(List<short> triangleIndices, List<PrimitiveTrailCopy.VertexPosition2DColor> vertices) where T : ModType
        {
            // Don't attempt to prepare anything if the batch has not been created yet.
            Type type = typeof(T);
            if (!Batches.TryGetValue(type, out PrimitiveBatch batch))
                return;

            // Offset all of the indices so that they remain separated.
            if (batch.Indices.Any())
            {
                short startingIndex = (short)(batch.Indices.Max() + 1);
                for (int i = 0; i < triangleIndices.Count; i++)
                    triangleIndices[i] += startingIndex;
            }

            batch.Indices.AddRange(triangleIndices);
            batch.Vertices.AddRange(vertices);
        }

        public static void Draw<T>() => Draw(typeof(T));

        public static void Draw(Type type)
        {
            // Don't attempt to draw anything if the batch has not been created yet.
            if (!Batches.TryGetValue(type, out PrimitiveBatch batch))
                return;

            batch.Draw();
        }
    }
}