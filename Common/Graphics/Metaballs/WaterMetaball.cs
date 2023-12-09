using CalamityMod.Graphics.Metaballs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers;
using InfernumMode.Content.Projectiles.Summoner;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class WaterMetaball : Metaball
    {
        public List<InfernumMetaballParticle> Particles
        {
            get;
            private set;
        } = new();

        public override bool AnythingToDraw => Particles.Any() || CalamityMod.CalamityUtils.AnyProjectiles(ModContent.ProjectileType<PerditusProjectile>());

        public override IEnumerable<Texture2D> Layers
        {
            get
            {
                yield return ModContent.GetInstance<WaterScene>().MainTarget;
            }
        }

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.AfterProjectiles;

        public override Color EdgeColor => Color.AliceBlue;//new(28, 175, 189, 0);

        public void SpawnParticle(Vector2 center, Vector2 velocity, Vector2 size, float decayRate = 0.985f)
        {
            if (Main.netMode != NetmodeID.Server)
                Particles.Add(new(center, velocity, size, decayRate));
        }

        public void SpawnParticles(IEnumerable<InfernumMetaballParticle> particles)
        {
            if (Main.netMode != NetmodeID.Server)
                Particles.AddRange(particles);
        }

        public override void Update()
        {
            foreach (var particle in Particles)
            {
                particle.Velocity.Y += 0.15f;

                if (Collision.SolidCollision(particle.Center, (int)particle.Size.X, (int)particle.Size.Y / 2, true))
                    particle.Velocity = Vector2.Zero;        

                if (particle.Timer > 4f)
                {
                    particle.Size *= particle.DecayRate;
                    particle.Velocity *= 0.99f;
                    particle.Velocity.X *= 0.98f;
                    particle.Velocity.Y *= 1.01f;

                    particle.Center += particle.Velocity;
                }
                particle.Timer++;
            }

            Particles.RemoveAll(particle => particle.Size.Length() < 5f || particle.Size.X < 0 || particle.Size.Y < 0);
        }

        public override void ClearInstances() => Particles.Clear();

        public override void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            // Draw with additive blending.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        }

        public override void PrepareShaderForTarget(int layerIndex)
        {
            // Store the shader in an easy to use local variable.
            var metaballShader = InfernumEffectsRegistry.BaseMetaballEdgeShader.GetShader().Shader;

            // Supply shader parameter values.
            metaballShader.Parameters["rtSize"]?.SetValue(Main.ScreenSize.ToVector2());
            metaballShader.Parameters["layerOffset"]?.SetValue(Vector2.Zero);
            metaballShader.Parameters["mainColor"]?.SetValue(EdgeColor.ToVector4());
            metaballShader.Parameters["edgeColor"]?.SetValue(EdgeColor.ToVector4());
            metaballShader.Parameters["useOverlayImage"]?.SetValue(true);
            metaballShader.Parameters["threshold"]?.SetValue(0.1f);
            metaballShader.Parameters["singleFrameScreenOffset"]?.SetValue((Main.screenLastPosition - Main.screenPosition) / Main.ScreenSize.ToVector2());
            metaballShader.Parameters["layerOffset"]?.SetValue(Main.screenPosition / Main.ScreenSize.ToVector2() + CalculateManualOffsetForLayer(layerIndex));
            Main.instance.GraphicsDevice.Textures[1] = Layers.ElementAt(0);
            // Apply the metaball shader.
            metaballShader.CurrentTechnique.Passes[0].Apply();
        }

        public override void DrawInstances()
        {
            Texture2D tex = InfernumTextureRegistry.BigGreyscaleCircle.Value;

            // Draw all particles.
            foreach (var particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 origin = tex.Size() * 0.5f;

                float scaleInterpolant = Utils.GetLerpValue(0f, 5f, particle.Timer, true);
                Vector2 scale = Vector2.One * particle.Size / tex.Size() * scaleInterpolant;

                // Store the brightness of the metaball in the R value.
                Main.spriteBatch.Draw(tex, drawPosition, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            // Draw perditus' whip line as a metaball.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active)
                    continue;

                if (Main.projectile[i].ModProjectile is PerditusProjectile perditus)
                {
                    List<Vector2> points = new();
                    Projectile.FillWhipControlPoints(perditus.Projectile, points);
                    PerditusProjectile.DrawWaterLine(points);
                    break;
                }
            }
        }
    }
}
