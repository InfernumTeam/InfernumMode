using CalamityMod.Graphics.Metaballs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class ProfanedLavaMetaball : Metaball
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
                yield return ProvidenceBehaviorOverride.IsEnraged && CalamityGlobalNPC.holyBoss != -1 ? InfernumTextureRegistry.HolyFirePixelLayerNight.Value : InfernumTextureRegistry.HolyFirePixelLayer.Value;
            }
        }

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeProjectiles;

        public override Color EdgeColor
        {
            get
            {
                Color dayColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[2], 0.2f);
                if (NPC.AnyNPCs(ModContent.NPCType<Providence>()))
                    return ProvidenceBehaviorOverride.IsEnraged ? Color.DeepSkyBlue : dayColor;

                return dayColor;
            }
        }

        public void SpawnParticle(Vector2 center, Vector2 velocity, Vector2 size, float decayRate = 0.965f)
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
                particle.Update();

            Particles.RemoveAll(particle => particle.Size.Length() < 2f);
        }

        public override void ClearInstances() => Particles.Clear();

        public override void PrepareSpriteBatch(SpriteBatch spriteBatch)
        {
            // Draw with additive blending.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        }

        public override void PrepareShaderForTarget(int layerIndex)
        {
            base.PrepareShaderForTarget(layerIndex);
            //// Store the shader in an easy to use local variable.
            //var metaballShader = CalamityShaders.MetaballEdgeShader;

            //// Calculate the layer scroll offset. This is used to ensure that the texture contents of the given metaball have parallax, rather than being static over the screen
            //// regardless of world position.
            //Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);

            //// Supply shader parameter values.
            //metaballShader.Parameters["screenArea"]?.SetValue(screenSize);
            //metaballShader.Parameters["layerOffset"]?.SetValue(Vector2.Zero);
            //metaballShader.Parameters["edgeColor"]?.SetValue(EdgeColor.ToVector4());
            //metaballShader.Parameters["singleFrameScreenOffset"]?.SetValue(Vector2.Zero);

            //// Apply the metaball shader.
            //metaballShader.CurrentTechnique.Passes[0].Apply();
        }

        public override void DrawInstances()
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BasicCircle").Value;

            // Draw all particles.
            foreach (var particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 origin = tex.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / tex.Size();

                Main.spriteBatch.Draw(tex, drawPosition, null, EdgeColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
