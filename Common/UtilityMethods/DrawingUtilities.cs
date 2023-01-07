using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        private static readonly FieldInfo shaderTextureField = typeof(MiscShaderData).GetField("_uImage1", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Uses reflection to set the _uImage1. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField.SetValue(shader, texture);

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
            spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
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
                int idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, 31, 0f, 0f, 100, default, 2f);
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
                int idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, 6, 0f, 0f, 100, default, 3f);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity *= 5f;
                Main.dust[idx].velocity += force.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.8f, 1.2f);

                idx = Dust.NewDust(topLeft, (int)area.X, (int)area.Y, 6, 0f, 0f, 100, default, 2f);
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
                            Color color = proj.GetAlpha(lightColor) * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
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

        public static void DisplayText(string text, Color? color = null)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(text, color ?? Color.White);
            else if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color ?? Color.White);
        }

        public static void GetCircleVertices(int sideCount, float radius, Vector2 center, out List<short> triangleIndices, out List<PrimitiveTrailCopy.VertexPosition2DColor> vertices)
        {
            vertices = new();
            triangleIndices = new();

            // Use the law of cosines to determine the side length of the triangles that compose the inscribed shape.
            float sideAngle = MathHelper.TwoPi / sideCount;
            float sideLength = (float)Math.Sqrt(2D - Math.Cos(sideAngle) * 2D) * radius;

            // Calculate vertices by approximating a circle with a bunch of triangles.
            for (int i = 0; i < sideCount; i++)
            {
                float completionRatio = i / (float)(sideCount - 1f);
                float nextCompletionRatio = (i + 1) / (float)(sideCount - 1f);
                Vector2 orthogonal = (MathHelper.TwoPi * completionRatio + MathHelper.PiOver2).ToRotationVector2();
                Vector2 radiusOffset = (MathHelper.TwoPi * completionRatio).ToRotationVector2() * radius;
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

        public static void CreateShockwave(Vector2 shockwavePosition, int rippleCount = 2, int rippleSize = 8, float rippleSpeed = 75f, bool playSound = true)
        {
            DeleteAllProjectiles(false, ModContent.ProjectileType<ScreenShakeProj>());

            if (playSound)
                SoundEngine.PlaySound(InfernumSoundRegistry.SonicBoomSound, Vector2.Lerp(shockwavePosition, Main.LocalPlayer.Center, 0.84f));

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int shockwaveID = NewProjectileBetter(shockwavePosition, Vector2.Zero, ModContent.ProjectileType<ScreenShakeProj>(), 0, 0f);
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

            int cinderSpawnRate = (int)MathHelper.Lerp(maxCinderSpawnRate, minCinderSpawnRate, lifeRatio);
            float cinderFlySpeed = MathHelper.Lerp(maxCinderFlySpeed, minCinderFlySpeed, lifeRatio);

            for (int i = 0; i < 3; i++)
            {
                if (!Main.rand.NextBool(cinderSpawnRate) || Main.gfxQuality < 0.35f)
                    continue;

                Vector2 cinderSpawnOffset = new(Main.rand.NextFloatDirection() * 1550f, 650f);
                Vector2 cinderVelocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.23f, 0.98f)) * Main.rand.NextFloat(0.6f, 1.2f) * cinderFlySpeed;
                if (Main.rand.NextBool())
                {
                    cinderSpawnOffset = cinderSpawnOffset.RotatedBy(-MathHelper.PiOver2) * new Vector2(0.9f, 1f);
                    cinderVelocity = cinderVelocity.RotatedBy(-MathHelper.PiOver2) * new Vector2(1.8f, -1f);
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

        public static string InfernalRelicText
        {
            get
            {
                float colorInterpolant = (float)(Math.Sin(MathHelper.Pi * Main.GlobalTimeWrappedHourly + 1.0) * 0.5) + 0.5f;
                Color c = CalamityUtils.MulticolorLerp(colorInterpolant, new Color(170, 0, 0, 255), Color.OrangeRed, new Color(255, 200, 0, 255));
                return CalamityUtils.ColorMessage("Imbued with the infernal flames of a defeated foe", c);
            }
        }
    }
}
