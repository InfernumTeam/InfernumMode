using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresLaughBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 60;

        public override float MaxRadius => 1000f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = Lerp(3f, 12f, Sin(Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Fuchsia, Color.Cyan, Clamp(lifetimeCompletionRatio * 1.35f, 0f, 1f));
        }
    }
}
