using System;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class ShadowMetaball : MetaballType
    {
        public override bool ShouldRender => ActiveParticleCount > 0;

        public override Func<Texture2D>[] LayerTextures =>
        [
            () => InfernumTextureRegistry.Shadow.Value
        ];

        public override Color EdgeColor => Color.Lerp(Color.Fuchsia, Color.Black, 0.7f) * 0.85f;

        public override string MetaballAtlasTextureToUse => "InfernumMode.BaseMetaball";

        public override void UpdateParticle(MetaballInstance particle) => particle.Size *= 0.905f;

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 2f;

        public override bool PerformCustomSpritebatchBegin(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            return true;
        }

        public override Vector2 CalculateManualOffsetForLayer(int layerIndex)
        {
            return layerIndex switch
            {
                // Background 1.
                0 => Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.03f,
                // Background 2.
                _ => -Vector2.UnitY * Main.GlobalTimeWrappedHourly * 0.027f
            };
        }

        public override void ExtraDrawing()
        {
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].ModNPC is not null and ShadowDemon demon && Main.npc[i].active)
                {
                    demon.DrawMetaballStuff();
                    break;
                }
            }
        }
    }
}
