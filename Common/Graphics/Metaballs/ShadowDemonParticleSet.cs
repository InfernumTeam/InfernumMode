using CalamityMod.Graphics.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class ShadowMetaball : Metaball
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
                yield return InfernumTextureRegistry.Shadow.Value;
                //yield return InfernumTextureRegistry.Shadow2.Value;
            }
        }

        public override MetaballDrawLayer DrawContext => MetaballDrawLayer.BeforeProjectiles;

        public override Color EdgeColor => Color.Lerp(Color.Fuchsia, Color.Black, 0.7f) * 0.85f;

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

        public override Vector2 CalculateManualOffsetForLayer(int layerIndex)
        {
            return layerIndex switch
            {
                // Background 1.
                0 => Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.03f,
                // Background 2.
                _ => -Vector2.UnitY * Main.GlobalTimeWrappedHourly * 0.027f,
            };
        }

        public override void DrawInstances()
        {
            // Draw the shadow hydra.
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].ModNPC is not null and ShadowDemon demon && Main.npc[i].active)
                {
                    demon.DrawMetaballStuff();
                    break;
                }
            }

            Texture2D fusableParticleBase = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BasicCircle").Value;//InfernumTextureRegistry.BigGreyscaleCircle.Value;
            foreach (var particle in Particles)
            {
                Vector2 drawPosition = particle.Center - Main.screenPosition;
                Vector2 origin = fusableParticleBase.Size() * 0.5f;
                Vector2 scale = Vector2.One * particle.Size / fusableParticleBase.Size();
                Main.spriteBatch.Draw(fusableParticleBase, drawPosition, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
