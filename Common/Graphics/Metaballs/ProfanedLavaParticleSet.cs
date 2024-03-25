using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Metaballs
{
    public class ProfanedLavaMetaball : MetaballType
    {
        public override bool ShouldRender => ActiveParticleCount > 0;

        public override Func<Texture2D>[] LayerTextures => 
        [
            () => ProvidenceBehaviorOverride.IsEnraged && CalamityGlobalNPC.holyBoss != -1 ? InfernumTextureRegistry.HolyFirePixelLayerNight.Value : InfernumTextureRegistry.HolyFirePixelLayer.Value
        ];

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

        public override string MetaballAtlasTextureToUse => "InfernumMode.BaseMetaball";

        public override void UpdateParticle(MetaballInstance particle)
        {
            if (particle.ExtraInfo[0] > 0)
                particle.Size *= particle.ExtraInfo[0];
            else
                particle.Size *= 0.93f;
        }

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 2f;

        public override void PrepareShaderForTarget(int layerIndex)
        {
            base.PrepareShaderForTarget(layerIndex);

            var metaballShader = InfernumEffectsRegistry.BaseMetaballEdgeShader;
            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);

            metaballShader.TrySetParameter("rtSize", screenSize);
            metaballShader.TrySetParameter("layerOffset", Vector2.Zero);
            metaballShader.TrySetParameter("edgeColor", EdgeColor.ToVector4());
            metaballShader.TrySetParameter("singleFrameScreenOffset", Vector2.Zero);
            metaballShader.Apply();
        }
    }
}
