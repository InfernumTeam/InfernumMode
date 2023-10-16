using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvBoomDeath : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 120;

        public override float MaxRadius => 1200f;

        public override float RadiusExpandRateInterpolant => 0.2f;

        public override Texture2D ExplosionNoiseTexture => InfernumTextureRegistry.HarshNoise.Value;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => 5f;

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            float colorInterpolant = Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f);
            if (ProvidenceBehaviorOverride.IsEnraged)
                return Color.Lerp(Color.SkyBlue, Color.Cyan, colorInterpolant);

            return Color.Lerp(Color.DarkOrange, Color.Orange, colorInterpolant) * (1f - lifetimeCompletionRatio);
        }
    }
}
