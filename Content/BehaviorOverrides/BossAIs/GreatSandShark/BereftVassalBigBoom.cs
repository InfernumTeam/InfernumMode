using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class BereftVassalBigBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 60;

        public override float MaxRadius => 1200f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => 4f;

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.DarkOrange, Color.DarkViolet, lifetimeCompletionRatio * 0.7f);
        }
    }
}
