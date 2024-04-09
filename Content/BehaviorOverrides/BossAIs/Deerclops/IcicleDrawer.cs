using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class IcicleDrawer
    {
        public class Branch
        {
            public BezierCurve Curve;

            public Vector2 EndOfCurve;

            public float Direction;

            public float CurveLength;

            public float StartingWidth;

            public float EndingWidth;

            public Branch PreviousBranch;

            public int Generation
            {
                get
                {
                    int generation = 0;
                    var parent = PreviousBranch;
                    while (parent != null)
                    {
                        parent = parent.PreviousBranch;
                        generation++;
                    }

                    return generation;
                }
            }

            public Branch(BezierCurve curve, Vector2 end, float length, float direction, float startWidth, float endWidth, Branch previousBranch = null)
            {
                Curve = curve;
                EndOfCurve = end;
                CurveLength = length;
                Direction = direction;
                StartingWidth = startWidth;
                EndingWidth = endWidth;
                PreviousBranch = previousBranch;
            }
        }

        internal Point deferredDrawPosition;

        public Point DeferredDrawPosition
        {
            get => deferredDrawPosition;
            set
            {
                if (value == Point.Zero)
                    Debugger.Break();
                deferredDrawPosition = value;
            }
        }

        internal Point PreviousPoint;

        internal VertexPositionColorTexture[] vertexCache = Array.Empty<VertexPositionColorTexture>();

        internal short[] indexCache = Array.Empty<short>();

        protected UnifiedRandom RNG = new(0);

        internal static BasicEffect basicShader;

        public static BasicEffect BasicShader
        {
            get
            {
                if (Main.netMode != NetmodeID.Server && basicShader is null)
                {
                    basicShader = new BasicEffect(Main.instance.GraphicsDevice)
                    {
                        VertexColorEnabled = true,
                        TextureEnabled = true
                    };
                }
                return basicShader;
            }
        }

        private static Texture2D icicleTexture;

        public static Texture2D IcicleTexture
        {
            get
            {
                icicleTexture ??= ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Deerclops/IcicleTexture", AssetRequestMode.ImmediateLoad).Value;
                return icicleTexture;
            }
        }

        public int Seed;

        // The max amount that branches can travel. Once the distance of the branches reaches or exceeds this threshold, the tree is done growing.
        public float MaxDistanceBeforeCutoff;

        public float DistanceUsedForBase;

        public float BranchMaxBendFactor;

        public float BranchTurnAngleVariance;

        // The shortest possible length a branch can have.
        public float MinBranchLength;

        // The width of the base. Successive branches have a dynamic, random width, but the base should be static.
        public float BaseWidth;

        // Chance to create new branches instead of extending existing ones.
        public float ChanceToCreateNewBranches;

        public float VerticalStretchFactor;

        public float DownwardBiasFactor;

        public float BranchGrowthWidthDecay;

        public float BaseDirection;

        public int MaxCutoffBranchesPerBranch;

        public Color IcicleColor = Color.White;

        public const int ControlPointCountPerBranch = 8;

        public Dictionary<Branch, List<Branch>> GenerateBranches()
        {
            Dictionary<Branch, List<Branch>> existingBranches = [];

            RNG = new(Seed);

            int baseCount = RNG.Next(2, 4);
            for (int i = 0; i < baseCount; i++)
            {
                float cutoffDistance = MaxDistanceBeforeCutoff;
                float baseDirection = RNG.NextFloatDirection() * BranchTurnAngleVariance * 0.1f + BaseDirection - PiOver2;
                float baseSize = RNG.NextFloat(-8f, 8f) + DistanceUsedForBase;
                float distanceTraversed = baseSize;
                Vector2 startOfBase = Vector2.UnitY * 10f;
                Vector2 endOfBase = startOfBase + baseDirection.ToRotationVector2() * baseSize;
                Branch baseBranch = GenerateBranchCurve(startOfBase, endOfBase, BaseWidth, BaseWidth);
                existingBranches[baseBranch] = [];

                void extendLengthOfBranch(Branch branch, float lengthToAdd)
                {
                    branch.CurveLength += lengthToAdd;
                    branch.EndOfCurve += branch.Direction.ToRotationVector2() * lengthToAdd;

                    // Add the new length to all branches that exist on the current one, ones extented to each of them, and so on.
                    foreach (Branch attachedBranch in existingBranches[branch])
                        extendLengthOfBranch(attachedBranch, lengthToAdd);
                }

                int tries = 0;
                while (distanceTraversed < cutoffDistance)
                {
                    // Prevent infinite loops if conditions do not permit the distance traveled to reach its maximum.
                    tries++;
                    if (tries >= 500)
                        break;

                    // Sometimes simply extend an existing branch instead of creating new ones.
                    if (RNG.NextFloat() > ChanceToCreateNewBranches)
                    {
                        List<Branch> potentialBranchesToExtend = existingBranches.Where(b => b.Key.CurveLength < baseSize * 0.4f && b.Key.CurveLength > MinBranchLength).Select(b => b.Key).ToList();
                        if (potentialBranchesToExtend.Count <= 0)
                            continue;

                        Branch branchToExtend = RNG.Next(potentialBranchesToExtend);

                        float lengthToAdd = branchToExtend.CurveLength * 0.12f;
                        extendLengthOfBranch(branchToExtend, lengthToAdd);
                        distanceTraversed += lengthToAdd;
                        continue;
                    }

                    // Pick a random branch to attach to and determine the properties of the potential next one.
                    List<Branch> validBranches = existingBranches.Where(b => b.Value.Count < MaxCutoffBranchesPerBranch && b.Key.EndingWidth >= 6f).Select(b => b.Key).ToList();
                    if (validBranches.Count <= 0)
                        continue;

                    Branch branchToAttachTo = RNG.Next(validBranches);
                    float directionOfNextBranch = branchToAttachTo.Direction + RNG.NextFloatDirection() * BranchTurnAngleVariance;
                    float downwardBiasFactor = DownwardBiasFactor;
                    float downwardBiasFromGeneration = Utils.Remap(branchToAttachTo.Generation, 0f, 5f, 0f, 0.8f);
                    downwardBiasFactor = Clamp(downwardBiasFactor + downwardBiasFromGeneration, 0f, 0.95f);

                    if (downwardBiasFactor > 0f && branchToAttachTo != baseBranch)
                    {
                        float randomBias = RNG.NextFloat(0.67f, 1f) * downwardBiasFactor;
                        directionOfNextBranch = Vector2.Lerp(directionOfNextBranch.ToRotationVector2(), Vector2.UnitY, randomBias).ToRotation();
                    }

                    float lengthOfNextBranch = MathF.Max(MinBranchLength, branchToAttachTo.CurveLength * RNG.NextFloat(0.5f, 0.925f));

                    Vector2 start = branchToAttachTo.EndOfCurve;
                    Vector2 end = start + directionOfNextBranch.ToRotationVector2() * lengthOfNextBranch;

                    Branch newBranch = GenerateBranchCurve(start, end, branchToAttachTo.EndingWidth, branchToAttachTo.EndingWidth * BranchGrowthWidthDecay, branchToAttachTo);

                    // Create the new branch in the dictionary and make the old branch count count as having one extra branch attached.
                    existingBranches[branchToAttachTo].Add(newBranch);
                    existingBranches[newBranch] = [];

                    // Add to traversed distance.
                    distanceTraversed += lengthOfNextBranch;
                }

                // Go back and make all end branches have a small end width.
                foreach (Branch branch in existingBranches.Where(b => b.Value.Count <= 0).Select(b => b.Key))
                    branch.EndingWidth = MathF.Min(3f, branch.EndingWidth);
            }

            return existingBranches;
        }

        public void GetVertexData(Point p, out List<VertexPositionColorTexture> vertices, out List<short> indices, out IEnumerable<Branch> outwardmostBranches)
        {
            // Initialize vertex and index data.
            vertices = [];
            indices = [];

            // Determine branch data.
            var branchData = GenerateBranches();
            var branches = branchData.Select(b => b.Key);
            outwardmostBranches = branchData.Where(b => b.Value.Count <= 0f).Select(b => b.Key);

            // Generate vertex data.
            int batchIndex = 0;
            Texture2D icicleTexture = IcicleTexture;
            foreach (Branch branch in branches.OrderBy(b => b.EndOfCurve.Y))
            {
                int pointCount = 12;
                List<Vector2> smoothenedPoints = branch.Curve.GetPoints(pointCount + 1);
                Vector2? prevBottomLeft = null;
                Vector2? prevBottomRight = null;
                if (branch.PreviousBranch != null)
                {
                    Vector2 previousOrthogonalDirection = (branch.PreviousBranch.Direction + PiOver2).ToRotationVector2();
                    prevBottomLeft = branch.PreviousBranch.EndOfCurve + previousOrthogonalDirection * branch.PreviousBranch.EndingWidth * 0.5f;
                    prevBottomRight = branch.PreviousBranch.EndOfCurve - previousOrthogonalDirection * branch.PreviousBranch.EndingWidth * 0.5f;
                }

                for (int i = 0; i < pointCount; i++)
                {
                    Vector2 top = smoothenedPoints[i];
                    Vector2 bottom = smoothenedPoints[i + 1];
                    float topCompletionRatio = i / (float)pointCount;
                    float bottomCompletionRatio = (i + 1) / (float)pointCount;
                    if (i == pointCount - 1f)
                    {
                        topCompletionRatio = 1f;
                        bottomCompletionRatio = 1f;
                        bottom = branch.EndOfCurve;
                    }

                    // Calculate frame coordinates.
                    // This sucked to make.
                    float topWidth = Lerp(branch.StartingWidth, branch.EndingWidth, topCompletionRatio);
                    float bottomWidth = Lerp(branch.StartingWidth, branch.EndingWidth, bottomCompletionRatio);
                    float topTexCoord = branch.CurveLength * topCompletionRatio / VerticalStretchFactor / icicleTexture.Height;
                    float bottomTexCoord = branch.CurveLength * bottomCompletionRatio / VerticalStretchFactor / icicleTexture.Height;
                    if (VerticalStretchFactor <= 0f)
                    {
                        topTexCoord = topWidth;
                        bottomTexCoord = bottomWidth;
                    }
                    float stretchedHorizontalCoordTop = topWidth / icicleTexture.Width;
                    float stretchedHorizontalCoordBottom = bottomWidth / icicleTexture.Width;
                    if (topWidth > icicleTexture.Width * 0.5f)
                        stretchedHorizontalCoordTop = 1f;
                    if (bottomWidth > icicleTexture.Width * 0.5f)
                        stretchedHorizontalCoordBottom = 1f;

                    // Calculate texture coordinates.
                    Vector2 topLeftTexCoord = new(stretchedHorizontalCoordTop, topTexCoord);
                    Vector2 topRightTexCoord = new(0f, topTexCoord);
                    Vector2 bottomLeftTexCoord = new(stretchedHorizontalCoordBottom, bottomTexCoord);
                    Vector2 bottomRightTexCoord = new(0f, bottomTexCoord);

                    // Calculate draw coordinates.
                    Vector2 orthogonalDirection = (bottom - top).SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2);
                    Vector2 topLeft = prevBottomLeft ?? top + orthogonalDirection * topWidth * 0.5f;
                    Vector2 topRight = prevBottomRight ?? top - orthogonalDirection * topWidth * 0.5f;
                    Vector2 bottomLeft = bottom + orthogonalDirection * bottomWidth * 0.5f;
                    Vector2 bottomRight = bottom - orthogonalDirection * bottomWidth * 0.5f;

                    // Calculate lighting colors.
                    vertices.Add(new VertexPositionColorTexture(new Vector3(topLeft.Floor() + p.ToVector2(), 0f), IcicleColor, topLeftTexCoord));
                    vertices.Add(new VertexPositionColorTexture(new Vector3(topRight.Floor() + p.ToVector2(), 0f), IcicleColor, topRightTexCoord));
                    vertices.Add(new VertexPositionColorTexture(new Vector3(bottomRight.Floor() + p.ToVector2(), 0f), IcicleColor, bottomRightTexCoord));
                    vertices.Add(new VertexPositionColorTexture(new Vector3(bottomLeft.Floor() + p.ToVector2(), 0f), IcicleColor, bottomLeftTexCoord));

                    indices.Add((short)(batchIndex * 4));
                    indices.Add((short)(batchIndex * 4 + 1));
                    indices.Add((short)(batchIndex * 4 + 2));
                    indices.Add((short)(batchIndex * 4));
                    indices.Add((short)(batchIndex * 4 + 2));
                    indices.Add((short)(batchIndex * 4 + 3));

                    prevBottomLeft = bottomLeft;
                    prevBottomRight = bottomRight;

                    batchIndex++;
                }
            }
        }

        public void PrepareDeferredDraw(Point p)
        {
            DeferredDrawPosition = p;
        }

        public void Draw(Point p, bool applyShaderManually)
        {
            // Declare the vertex cache.
            if (vertexCache.Length <= 0 || Main.GameUpdateCount % 240 == 239)
            {
                GetVertexData(p, out var vertices, out var indices, out _);
                vertexCache = [.. vertices];
                indexCache = [.. indices];
                PreviousPoint = p;
            }

            if (applyShaderManually)
                ApplyShader();

            // Draw the tree itself.
            Main.instance.GraphicsDevice.Textures[0] = IcicleTexture;
            Main.instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexCache, 0, vertexCache.Length, indexCache, 0, indexCache.Length / 3);
        }

        public bool IsCollidingWith(Vector2 positionOffset, Rectangle otherRectangle)
        {
            var branches = GenerateBranches();
            foreach (var branch in branches.Keys)
            {
                float _ = 0f;
                Vector2 end = branch.EndOfCurve + positionOffset;
                Vector2 start = end - branch.Direction.ToRotationVector2() * branch.CurveLength;
                float width = (branch.StartingWidth + branch.EndingWidth) * 0.5f;
                if (Collision.CheckAABBvLineCollision(otherRectangle.TopLeft(), otherRectangle.Size(), start, end, width, ref _))
                    return true;
            }
            return false;
        }

        public void DoShatterEffect(Vector2 positionOffset)
        {
            var branches = GenerateBranches();
            foreach (var branch in branches.Keys)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 end = branch.EndOfCurve + positionOffset;
                    Vector2 start = end - branch.Direction.ToRotationVector2() * branch.CurveLength;
                    Vector2 crystalShardSpawnPosition = Vector2.Lerp(end, start, Main.rand.NextFloat());
                    Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.6f, 13.6f);

                    Dust shard = Dust.NewDustPerfect(crystalShardSpawnPosition, 68, shardVelocity);
                    shard.noGravity = Main.rand.NextBool();
                    shard.scale = Main.rand.NextFloat(1.3f, 1.925f);
                    shard.velocity.Y -= 5f;
                }
            }
        }

        public Branch GenerateBranchCurve(Vector2 start, Vector2 end, float startWidth, float endWidth, Branch previousBranch = null)
        {
            float distanceBetweenPoints = Vector2.Distance(start, end);
            Vector2[] initialPoints = new Vector2[ControlPointCountPerBranch];
            Vector2 orthogonalDirection = (end - start).SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2);
            for (int i = 0; i < ControlPointCountPerBranch; i++)
                initialPoints[i] = Vector2.Lerp(start, end, i / (float)(ControlPointCountPerBranch - 1f));

            // Create a bend midway.
            float bendFactor = Pow(RNG.NextFloat(), 0.66f) * RNG.NextBool().ToDirectionInt() * BranchMaxBendFactor;
            bendFactor = Lerp(bendFactor, Math.Sign(bendFactor) * BranchMaxBendFactor, Utils.GetLerpValue(DistanceUsedForBase * 0.4f, DistanceUsedForBase * 0.75f, distanceBetweenPoints, true));

            initialPoints[ControlPointCountPerBranch / 2] += orthogonalDirection * RNG.NextFloatDirection() * distanceBetweenPoints * bendFactor;

            return new(new(initialPoints), end, distanceBetweenPoints, (end - start).ToRotation(), startWidth, endWidth, previousBranch);
        }

        public static void ApplyShader()
        {
            // Redefine the perspective matrices of the shader.
            LumUtils.CalculatePrimitiveMatrices(Main.screenWidth, Main.screenHeight, out Matrix effectView, out Matrix effectProjection);

            BasicShader.Texture = IcicleTexture;
            BasicShader.View = effectView;
            BasicShader.Projection = effectProjection;
            BasicShader.World = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);
            BasicShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
