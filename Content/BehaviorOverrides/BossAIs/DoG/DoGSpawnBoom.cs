using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class DoGSpawnBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 180;

        public override float MaxRadius => 2300f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override Texture2D ExplosionNoiseTexture => InfernumTextureRegistry.CracksNoise.Value;

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = Lerp(3f, 16f, Sin(Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Cyan, Color.Fuchsia, Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f));
        }
    }
}
