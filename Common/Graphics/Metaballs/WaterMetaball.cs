using System.Collections.Generic;
using System.Linq;
using CalamityMod.Graphics.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class WaterMetaball : Metaball
    {
        public List<InfernumMetaballParticle> Particles
        {
            get;
            private set;
        } = new();

        public override bool AnythingToDraw => Particles.Any();

        public override IEnumerable<Texture2D> Layers
        {
            get
            {
                yield return ModContent.GetInstance<WaterScene>().MainTarget;
            }
        }

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeNPCs;

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

                particle.Velocity *= 0.99f;
                particle.Update();
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
            Texture2D tex = InfernumTextureRegistry.BigGreyscaleCircle.Value;/*ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BasicCircle").Value;*/

            // Draw all particles.
            foreach (var particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 origin = tex.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();

                // Angle the metaball towards its direction.
                float rotation = 0f;//particle.Velocity.ToRotation() + PiOver2;

                // Store the brightness of the metaball in the R value.
                float lightingStrength = Lighting.GetColor(particle.Center.ToTileCoordinates()).ToGreyscale();
                Color color = new(lightingStrength, 0f, 0f, 1f);
                Main.spriteBatch.Draw(tex, drawPosition, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
