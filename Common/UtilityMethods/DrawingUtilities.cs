using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Common.MapLayers;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        private static readonly FieldInfo shaderTextureField = typeof(MiscShaderData).GetField("_uImage1", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shaderTextureField2 = typeof(MiscShaderData).GetField("_uImage2", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shaderTextureField3 = typeof(MiscShaderData).GetField("_uImage3", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Uses reflection to set the _uImage1. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField.SetValue(shader, texture);

        /// <summary>
        /// Uses reflection to set the _uImage2. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture2(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField2.SetValue(shader, texture);

        /// <summary>
        /// Uses reflection to set the _uImage3. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture3(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField3.SetValue(shader, texture);

        /// <summary>
        /// Prepares a <see cref="SpriteBatch"/> for shader-based drawing.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        public static void EnterShaderRegion(this SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Ends changes to a <see cref="SpriteBatch"/> based on shader-based drawing in favor of typical draw begin states.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        public static void ExitShaderRegion(this SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Sets a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> arbitrarily.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void SetBlendState(this SpriteBatch spriteBatch, BlendState blendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Reset's a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> based to a typical <see cref="BlendState.AlphaBlend"/>.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void ResetBlendState(this SpriteBatch spriteBatch) => spriteBatch.SetBlendState(BlendState.AlphaBlend);

        /// <summary>
        /// Restarts a given <see cref="SpriteBatch"/> such that it enforces a rectangular area where pixels outside of said area are not drawn.<br></br>
        /// This is incredible convenient for UI sections where you need to ensure things only appear inside a box panel.<br></br>
        /// This method should be followed by a call to <see cref="ReleaseCutoffRegion(SpriteBatch, Matrix, SpriteSortMode)"/> once you're ready to flush the contents drawn under these conditions.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to enforce the cutoff region on.</param>
        /// <param name="cutoffRegion">The cutoff region. This should be in screen coordinates.</param>
        /// <param name="perspective">The perspective matrix that should be used across drawn contents.</param>
        /// <param name="sortMode">The sort mode that should be used across drawn contents. Use <see cref="SpriteSortMode.Immediate"/> if you additionally need to draw shaders.</param>
        /// <param name="newBlendState">The blend state that should be used across drawn contents. This defaults to <see cref="BlendState.AlphaBlend"/>.</param>
        public static void EnforceCutoffRegion(this SpriteBatch spriteBatch, Rectangle cutoffRegion, Matrix perspective, SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState newBlendState = null)
        {
            var rasterizer = Main.Rasterizer;
            rasterizer.ScissorTestEnable = true;

            spriteBatch.End();
            spriteBatch.Begin(sortMode, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, perspective);
            spriteBatch.GraphicsDevice.ScissorRectangle = cutoffRegion;
        }

        /// <summary>
        /// Flushes contents drawn under restrictions enforced by the <see cref="EnforceCutoffRegion(SpriteBatch, Rectangle, Matrix, SpriteSortMode, BlendState)"/> method and returns the <see cref="SpriteBatch"/> to a more typical state.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to flush the contents of.</param>
        /// <param name="perspective">The perspective matrix that was used before the cutoff region was enforced. Take care to ensure that this has the correct input.</param>
        /// <param name="sortMode">The sort mode that should be used across drawn contents. Use <see cref="SpriteSortMode.Immediate"/> if you additionally need to draw shaders.</param>
        public static void ReleaseCutoffRegion(this SpriteBatch spriteBatch, Matrix perspective, SpriteSortMode sortMode = SpriteSortMode.Deferred)
        {
            int width = spriteBatch.GraphicsDevice.Viewport.Width;
            int height = spriteBatch.GraphicsDevice.Viewport.Height;

            spriteBatch.End();
            spriteBatch.Begin(sortMode, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, perspective);
            spriteBatch.GraphicsDevice.ScissorRectangle = new(-1, -1, width + 2, height + 2);
        }

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

            Texture2D line = InfernumTextureRegistry.Line.Value;
            float rotation = (end - start).ToRotation();
            Vector2 scale = new(Vector2.Distance(start, end) / line.Width, width);

            spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public static void DrawBloomLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            // Draw nothing if the start and end are equal, to prevent division by 0 problems.
            if (start == end)
                return;

            start -= Main.screenPosition;
            end -= Main.screenPosition;

            Texture2D line = InfernumTextureRegistry.BloomLine.Value;
            float rotation = (end - start).ToRotation() + PiOver2;
            Vector2 scale = new Vector2(width, Vector2.Distance(start, end)) / line.Size();
            Vector2 origin = new(line.Width / 2f, line.Height);

            spriteBatch.Draw(line, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
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

        public static List<Vector2> CorrectBezierPointRetreivalFunction(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, IEnumerable<float> _ = null)
        {
            List<Vector2> controlPoints = new();
            for (int i = 0; i < originalPositions.Count(); i++)
            {
                // Don't incorporate points that are zeroed out.
                // They are almost certainly a result of incomplete oldPos arrays.
                if (originalPositions.ElementAt(i) == Vector2.Zero)
                    continue;
                controlPoints.Add(originalPositions.ElementAt(i) + generalOffset);
            }

            if (controlPoints.Count <= 1)
                return controlPoints;

            List<Vector2> points = new();
            BezierCurve bezierCurve = new(controlPoints.ToArray());

            // The GetPoints method uses imprecise floating-point looping, which can result in inaccuracies with point generation.
            // Instead, an integer-based loop is used to mitigate such problems.
            for (int i = 0; i < totalTrailPoints; i++)
                points.Add(bezierCurve.Evaluate(i / (float)(totalTrailPoints - 1f)));

            return points;
        }

        public static void CreateFireExplosion(Vector2 topLeft, Vector2 area, Vector2 force)
        {
            // Sparks and such
            for (int i = 0; i < 40; i++)
            {
                int idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Main.dust[idx].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[idx].scale = 0.5f;
                    Main.dust[idx].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                    Main.dust[idx].velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);
                }
            }
            for (int i = 0; i < 70; i++)
            {
                int idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, DustID.Torch, 0f, 0f, 100, default, 3f);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity *= 5f;
                Main.dust[idx].velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);

                idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, DustID.Torch, 0f, 0f, 100, default, 2f);
                Main.dust[idx].velocity *= 2f;
                Main.dust[idx].velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);
            }

            // Smoke, which counts as a Gore
            if (Main.netMode != NetmodeID.Server)
            {
                int goreAmt = 3;
                Vector2 center = topLeft + area * 0.5f;
                Vector2 source = new(center.X - 24f, center.Y - 24f);
                for (int goreIndex = 0; goreIndex < goreAmt; goreIndex++)
                {
                    float velocityMult = 0.33f;
                    if (goreIndex < (goreAmt / 3))
                        velocityMult = 0.66f;
                    if (goreIndex >= (2 * goreAmt / 3))
                        velocityMult = 1f;

                    int type = Main.rand.Next(61, 64);
                    int smoke = Gore.NewGore(new EntitySource_WorldEvent(), source, default, type, 1f);
                    Gore gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X += 1f;
                    gore.velocity.Y += 1f;
                    gore.velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);

                    type = Main.rand.Next(61, 64);
                    smoke = Gore.NewGore(new EntitySource_WorldEvent(), source, default, type, 1f);
                    gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X -= 1f;
                    gore.velocity.Y += 1f;
                    gore.velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);

                    type = Main.rand.Next(61, 64);
                    smoke = Gore.NewGore(new EntitySource_WorldEvent(), source, default, type, 1f);
                    gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X += 1f;
                    gore.velocity.Y -= 1f;
                    gore.velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);

                    type = Main.rand.Next(61, 64);
                    smoke = Gore.NewGore(new EntitySource_WorldEvent(), source, default, type, 1f);
                    gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X -= 1f;
                    gore.velocity.Y -= 1f;
                    gore.velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);
                }
            }
        }

        /// <summary>
        /// Draws a projectile as a series of afterimages. The first of these afterimages is centered on the center of the projectile's hitbox.<br />
        /// This function is guaranteed to draw the projectile itself, even if it has no afterimages and/or the Afterimages config option is turned off.
        /// </summary>
        /// <param name="proj">The projectile to be drawn.</param>
        /// <param name="mode">The type of afterimage drawing code to use. Vanilla Terraria has three options: 0, 1, and 2.</param>
        /// <param name="lightColor">The light color to use for the afterimages.</param>
        /// <param name="typeOneIncrement">If mode 1 is used, this controls the loop increment. Set it to more than 1 to skip afterimages.</param>
        /// <param name="texture">The texture to draw. Set to <b>null</b> to draw the projectile's own loaded texture.</param>
        /// <param name="drawCentered">If <b>false</b>, the afterimages will be centered on the projectile's position instead of its own center.</param>
        public static void DrawAfterimagesCentered(Projectile proj, Color lightColor, int mode, int typeOneIncrement = 1, Texture2D texture = null, bool drawCentered = true)
        {
            texture ??= TextureAssets.Projectile[proj.type].Value;

            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            float scale = proj.scale;
            float rotation = proj.rotation;

            Rectangle rectangle = new(0, frameY, texture.Width, frameHeight);
            Vector2 origin = rectangle.Size() / 2f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // If no afterimages are drawn due to an invalid mode being specified, ensure the projectile itself is drawn anyway.
            bool failedToDrawAfterimages = false;

            if (CalamityConfig.Instance.Afterimages)
            {
                Vector2 centerOffset = drawCentered ? proj.Size / 2f : Vector2.Zero;
                switch (mode)
                {
                    // Standard afterimages. No customizable features other than total afterimage count.
                    // Type 0 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 0:
                        for (int i = proj.oldPos.Length - 1; i >= 0; --i)
                        {
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // DO NOT REMOVE THESE "UNNECESSARY" FLOAT CASTS. THIS WILL BREAK THE AFTERIMAGES.
                            Color color = proj.GetAlpha(lightColor) * ((float)(proj.oldPos.Length - i) / proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, scale, spriteEffects, 0f);
                        }
                        break;

                    // Paladin's Hammer style afterimages. Can be optionally spaced out further by using the typeOneDistanceMultiplier variable.
                    // Type 1 afterimages linearly scale down from 66% to 0% opacity. They otherwise do not differ from type 0.
                    case 1:
                        // Safety check: the loop must increment
                        int increment = Math.Max(1, typeOneIncrement);
                        Color drawColor = proj.GetAlpha(lightColor);
                        int afterimageCount = ProjectileID.Sets.TrailCacheLength[proj.type];
                        int k = 0;
                        while (k < afterimageCount)
                        {
                            Vector2 drawPos = proj.oldPos[k] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // DO NOT REMOVE THESE "UNNECESSARY" FLOAT CASTS EITHER.
                            if (k > 0)
                            {
                                float colorMult = (float)(afterimageCount - k);
                                drawColor *= colorMult / ((float)afterimageCount * 1.5f);
                            }
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, scale, spriteEffects, 0f);
                            k += increment;
                        }
                        break;

                    // Standard afterimages with rotation. No customizable features other than total afterimage count.
                    // Type 2 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 2:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            float afterimageRot = proj.oldRot[i];
                            SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // DO NOT REMOVE THESE "UNNECESSARY" FLOAT CASTS. THIS WILL BREAK THE AFTERIMAGES.
                            Color color = proj.GetAlpha(lightColor) * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, scale, sfxForThisAfterimage, 0f);
                        }
                        break;

                    default:
                        failedToDrawAfterimages = true;
                        break;
                }
            }

            // Draw the projectile itself. Only do this if no afterimages are drawn because afterimage 0 is the projectile itself.
            if (!CalamityConfig.Instance.Afterimages || ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = drawCentered ? proj.Center : proj.position;
                Main.spriteBatch.Draw(texture, startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY), rectangle, proj.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);
            }
        }

        public static void GetCircleVertices(int sideCount, float radius, Vector2 center, out List<short> triangleIndices, out List<PrimitiveTrailCopy.VertexPosition2DColor> vertices)
        {
            vertices = new();
            triangleIndices = new();

            // Use the law of cosines to determine the side length of the triangles that compose the inscribed shape.
            float sideAngle = TwoPi / sideCount;
            float sideLength = Sqrt(2f - Cos(sideAngle) * 2f) * radius;

            // Calculate vertices by approximating a circle with a bunch of triangles.
            for (int i = 0; i < sideCount; i++)
            {
                float completionRatio = i / (float)(sideCount - 1f);
                float nextCompletionRatio = (i + 1) / (float)(sideCount - 1f);
                Vector2 orthogonal = (TwoPi * completionRatio + PiOver2).ToRotationVector2();
                Vector2 radiusOffset = (TwoPi * completionRatio).ToRotationVector2() * radius;
                Vector2 leftEdgeInner = center;
                Vector2 rightEdgeInner = center;
                Vector2 leftEdge = leftEdgeInner + radiusOffset + orthogonal * sideLength * -0.5f;
                Vector2 rightEdge = rightEdgeInner + radiusOffset + orthogonal * sideLength * 0.5f;

                vertices.Add(new(leftEdge - Main.screenPosition, Color.White, new(completionRatio, 1f)));
                vertices.Add(new(rightEdge - Main.screenPosition, Color.White, new(nextCompletionRatio, 1f)));
                vertices.Add(new(rightEdgeInner - Main.screenPosition, Color.White, new(nextCompletionRatio, 0f)));
                vertices.Add(new(leftEdgeInner - Main.screenPosition, Color.White, new(completionRatio, 0f)));

                triangleIndices.Add((short)(i * 4));
                triangleIndices.Add((short)(i * 4 + 1));
                triangleIndices.Add((short)(i * 4 + 2));
                triangleIndices.Add((short)(i * 4));
                triangleIndices.Add((short)(i * 4 + 2));
                triangleIndices.Add((short)(i * 4 + 3));
            }
        }

        public static void CreateShockwave(Vector2 shockwavePosition, int rippleCount = 2, int rippleSize = 8, float rippleSpeed = 75f, bool playSound = true, bool useSecondaryVariant = false)
        {
            DeleteAllProjectiles(false, ModContent.ProjectileType<ScreenShakeProj>());

            // Don't bother spawning on low graphics mode.
            if (InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            if (playSound)
                SoundEngine.PlaySound(InfernumSoundRegistry.SonicBoomSound, Vector2.Lerp(shockwavePosition, Main.LocalPlayer.Center, 0.84f));

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int shockwaveID = NewProjectileBetter(shockwavePosition, Vector2.Zero, ModContent.ProjectileType<ScreenShakeProj>(), 0, 0f, -1, useSecondaryVariant.ToInt());
                if (Main.projectile.IndexInRange(shockwaveID))
                {
                    var shockwave = Main.projectile[shockwaveID].ModProjectile<ScreenShakeProj>();
                    shockwave.RippleCount = rippleCount;
                    shockwave.RippleSize = rippleSize;
                    shockwave.RippleSpeed = rippleSpeed;
                }
            }
        }

        public static void CreateCinderParticles(this Player target, float lifeRatio, BaseCinderParticle cinderParticle, float maxCinderSpawnRate = 3.5f, float minCinderSpawnRate = 12f, float maxCinderFlySpeed = 12f, float minCinderFlySpeed = 6f)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            int cinderSpawnRate = (int)Lerp(maxCinderSpawnRate, minCinderSpawnRate, lifeRatio);
            float cinderFlySpeed = Lerp(maxCinderFlySpeed, minCinderFlySpeed, lifeRatio);

            for (int i = 0; i < 3; i++)
            {
                if (!Main.rand.NextBool(cinderSpawnRate) || Main.gfxQuality < 0.35f)
                    continue;

                Vector2 cinderSpawnOffset = new(Main.rand.NextFloatDirection() * 1550f, 650f);
                Vector2 cinderVelocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.23f, 0.98f)) * Main.rand.NextFloat(0.6f, 1.2f) * cinderFlySpeed;
                if (Main.rand.NextBool())
                {
                    cinderSpawnOffset = cinderSpawnOffset.RotatedBy(-PiOver2) * new Vector2(0.9f, 1f);
                    cinderVelocity = cinderVelocity.RotatedBy(-PiOver2) * new Vector2(1.8f, -1f);
                }

                if (Main.rand.NextBool(6))
                    cinderVelocity.X *= -1f;

                cinderParticle.Position = target.Center + cinderSpawnOffset;
                cinderParticle.Velocity = cinderVelocity;
                cinderParticle.Color = Color.White;
                GeneralParticleHandler.SpawnParticle(cinderParticle);
            }
        }

        public static void UpdateMapIconList()
        {
            // Get the private list of fields
            FieldInfo layers = typeof(MapIconOverlay).GetField("_layers", BindingFlags.Instance | BindingFlags.NonPublic);
            // Save it locally.
            List<IMapLayer> list = (List<IMapLayer>)layers.GetValue(Main.MapIcons);
            // Add our one to the end.
            list.Add(new WayfinderMapLayer());
            // Set the value.
            layers.SetValue(Main.MapIcons, list);
        }

        public static void EmptyDrawCache(this List<DrawData> drawCache)
        {
            // WHAT THE FUCK NO ABORT ABORT ABORT
            if (drawCache.Count >= 10000 || Main.mapFullscreen)
                drawCache.Clear();

            Vector2 topLeft = Vector2.One * -200f;
            Vector2 bottomRight = new Vector2(Main.screenWidth, Main.screenHeight) - topLeft;
            while (drawCache.Count > 0)
            {
                if (drawCache[0].position.Length() > 10000f && drawCache[0].position.Between(topLeft, bottomRight))
                    drawCache[0] = drawCache[0] with
                    {
                        position = drawCache[0].position - Main.screenPosition
                    };
                drawCache[0].Draw(Main.spriteBatch);
                drawCache.RemoveAt(0);
            }
        }

        public static string InfernalRelicText
        {
            get
            {
                float colorInterpolant = (float)(Math.Sin(Pi * Main.GlobalTimeWrappedHourly + 1f) * 0.5) + 0.5f;
                Color c = CalamityUtils.MulticolorLerp(colorInterpolant, new Color(170, 0, 0, 255), Color.OrangeRed, new Color(255, 200, 0, 255));
                return CalamityUtils.ColorMessage(GetLocalization("Items.InfernalRelicText").Value, c);
            }
        }

        public static void SwapToRenderTarget(this ManagedRenderTarget renderTarget, Color? flushColor = null) => SwapToRenderTarget(renderTarget.Target, flushColor);

        public static void SwapToRenderTarget(this RenderTarget2D renderTarget, Color? flushColor = null)
        {
            // Local variables for convinience.
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            // If we are in the menu, a server, or any of these are null, return.
            if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
                return;

            // Otherwise set the render target.
            graphicsDevice.SetRenderTarget(renderTarget);

            // "Flush" the screen, removing any previous things drawn to it.
            flushColor ??= Color.Transparent;
            graphicsDevice.Clear(flushColor.Value);
        }

        /// <summary>
        /// Creates a list of <see cref="InfernumMetaballParticle"/> from a given texture shape and color information.
        /// </summary>
        /// <param name="texture">The texture to use</param>
        /// <param name="texturePosition">The world postition of the texture</param>
        /// <param name="textureRotation">The rotation of the texture</param>
        /// <param name="textureScale">The scale of the texture</param>
        /// <param name="metaballSize">The base size of the metaballs</param>
        /// <param name="spawnChance">The chance that a pixel will create a metaball</param>
        /// <returns></returns>
        public static IEnumerable<InfernumMetaballParticle> CreateMetaballsFromTexture(this Texture2D texture, Vector2 texturePosition, float textureRotation, float textureScale, float metaballSize, int spawnChance = 35, float decayRate = 0.985f)
        {
            List<InfernumMetaballParticle> metaballs = new();
            // Leave if this is null, or this is called on the server.
            if (Main.netMode == NetmodeID.Server)
                return metaballs;

            // If on low detail mode, just give a bunch of random metaballs from the texture size to save on performance.
            if (InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                Vector2 actualSize = texture.Size() * textureScale;
                int metaballCount = (int)(actualSize.X * actualSize.Y) / 2;
                for (int i = 0; i < metaballCount; i++)
                {
                    if (Main.rand.NextBool(spawnChance))
                    {
                        InfernumMetaballParticle particle = new(Main.rand.NextVector2FromRectangle(new((int)texturePosition.X, (int)texturePosition.Y, (int)actualSize.X, (int)actualSize.Y)), Vector2.Zero, new(Main.rand.NextFloat(metaballSize * 0.8f, metaballSize * 1.2f)), decayRate);
                        metaballs.Add(particle);
                    }
                }
                return metaballs;
            }

            // Get the dimensions of the texture.
            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            // Get the data of every color in the texture.
            Color[] colorData = new Color[textureWidth * textureHeight];
            texture.GetData(colorData);

            // Loop across the texture lengthways, one row at a time.
            for (int h = 0; h < textureHeight; h++)
            {
                for (int w = 0; w < textureWidth; w++)
                {
                    Color color = colorData[w + h * textureWidth];

                    // If the current pixel has any alpha, and the chance is selected (this exists to add variation and prevent having way too many metaballs spawn)
                    if (color.A > 0 && (color.R > 0 && color.G > 0 && color.B > 0) && Main.rand.NextBool(spawnChance))
                    {
                        Vector2 positionOffset = textureScale * new Vector2(textureWidth * 0.5f, textureHeight * 0.5f).RotatedBy(textureRotation);
                        Vector2 metaballSpawnPosition = texturePosition - positionOffset + new Vector2(w, h).RotatedBy(textureRotation);
                        InfernumMetaballParticle particle = new(metaballSpawnPosition, Vector2.Zero, new Vector2(Main.rand.NextFloat(metaballSize * 0.8f, metaballSize * 1.2f)) * color.A / 255, decayRate);
                        metaballs.Add(particle);
                    }
                }
            }
            return metaballs;
        }

        public static void DrawBloomLineTelegraph(Vector2 drawPosition, BloomLineDrawInfo drawInfo, bool resetSpritebatch = true, Vector2? resolution = null)
        {
            // Claim texture and shader data in easy to use local variables.
            Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;

            // Prepare all parameters for the shader in anticipation that they will go the GPU for shader effects.
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.004f);
            laserScopeEffect.Parameters["mainOpacity"].SetValue(drawInfo.Opacity);
            laserScopeEffect.Parameters["Resolution"].SetValue(resolution ?? Vector2.One * 425f);
            laserScopeEffect.Parameters["laserAngle"].SetValue(drawInfo.LineRotation);
            laserScopeEffect.Parameters["laserWidth"].SetValue(drawInfo.WidthFactor);
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(drawInfo.LightStrength);
            laserScopeEffect.Parameters["color"].SetValue(drawInfo.MainColor.ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(drawInfo.DarkerColor.ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(drawInfo.BloomIntensity);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(drawInfo.BloomOpacity);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

            // Prepare the sprite batch for shader drawing.
            if (resetSpritebatch)
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            laserScopeEffect.CurrentTechnique.Passes[0].Apply();

            // Draw the texture with the shader and flush the results to the GPU, clearing the shader effect for any successive draw calls.
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, drawInfo.Scale, SpriteEffects.None, 0f);
            if (resetSpritebatch)
                Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>
        /// Return a matrix suitable for use when resetting spritebatches in CustomSkys for shader work.
        /// </summary>
        /// <returns></returns>
        public static Matrix GetCustomSkyBackgroundMatrix()
        {
            Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
            transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation *
                new Vector3(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? (-1f) : 1f, 1f);
            return transformationMatrix;
        }

        /// <summary>
        /// Returns the appropriate value for "resolution" in pixelshaders, to make the pixel size match Terraria's.
        /// </summary>
        /// <param name="areaSize">The size of the area the shader is being applied on.</param>
        /// <param name="scale">The scale at which the pixels are being drawn at, eg: NPC.scale.</param>
        /// <returns></returns>
        public static Vector2 CreatePixelationResolution(Vector2 areaSize, Vector2? scale = null) => areaSize / (2 * (scale ?? Vector2.One));

        public static void SetTexture1(this Texture2D texture) => Main.instance.GraphicsDevice.Textures[1] = texture;

        public static void SetTexture2(this Texture2D texture) => Main.instance.GraphicsDevice.Textures[2] = texture;

        public static void SetTexture3(this Texture2D texture) => Main.instance.GraphicsDevice.Textures[3] = texture;

        /// <summary>
        /// Converts a color to its greyscale brightness value.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static float ToGreyscale(this Color color) => Vector3.Dot(color.ToVector3(), new Vector3(0.299f, 0.587f, 0.114f));
    }
}
