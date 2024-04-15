using System;
using System.Collections.Generic;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Drawers.SceneDrawers;
using InfernumMode.Content.Projectiles.Summoner;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class WaterMetaball : MetaballType
    {
        public override bool ShouldRender => ActiveParticleCount > 0 || LumUtils.AnyProjectiles(ModContent.ProjectileType<PerditusProjectile>());

        public override Func<Texture2D>[] LayerTextures =>
        [
            () => ModContent.GetInstance<WaterScene>().MainTarget
        ];

        public override Color EdgeColor => Color.Transparent;

        public override string MetaballAtlasTextureToUse => "InfernumMode.BaseMetaball";

        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity.Y += 0.15f;

            if (Collision.SolidCollision(particle.Center, (int)particle.Size, (int)particle.Size / 2, true))
                particle.Velocity = Vector2.Zero;

            if (particle.ExtraInfo[0] > 4f)
            {
                particle.Size *= 0.95f;
                particle.Velocity *= 0.99f;
                particle.Velocity.X *= 0.98f;
                particle.Velocity.Y *= 1.01f;

                particle.Center += particle.Velocity;
            }
            particle.ExtraInfo[0]++;
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 2;

        public override bool PerformCustomSpritebatchBegin(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            return true;
        }

        public override void PrepareShaderForTarget(int layerIndex)
        {
            base.PrepareShaderForTarget(layerIndex);
            //var metaballShader = InfernumEffectsRegistry.BaseMetaballEdgeShader;

            //// Supply shader parameter values.
            //metaballShader.TrySetParameter("rtSize", Main.ScreenSize.ToVector2());
            //metaballShader.TrySetParameter("layerOffset", Vector2.Zero);
            //metaballShader.TrySetParameter("mainColor", EdgeColor.ToVector4());
            //metaballShader.TrySetParameter("edgeColor", EdgeColor.ToVector4());
            //metaballShader.TrySetParameter("useOverlayImage", true);
            //metaballShader.TrySetParameter("threshold", 0.1f);
            //metaballShader.TrySetParameter("singleFrameScreenOffset", (Main.screenLastPosition - Main.screenPosition) / Main.ScreenSize.ToVector2());
            //metaballShader.TrySetParameter("layerOffset", Main.screenPosition / Main.ScreenSize.ToVector2() + CalculateManualOffsetForLayer(layerIndex));
            //metaballShader.SetTexture(LayerTargets[0], 1, SamplerState.PointWrap);
            //metaballShader.Apply();
        }

        public override void ExtraDrawing()
        {
            // Draw perditus' whip line as a metaball.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active)
                    continue;

                if (Main.projectile[i].ModProjectile is PerditusProjectile perditus)
                {
                    List<Vector2> points = [];
                    Projectile.FillWhipControlPoints(perditus.Projectile, points);
                    PerditusProjectile.DrawWaterLine(points);
                    break;
                }
            }
        }
    }
}
