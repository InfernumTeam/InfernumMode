using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class RoDFailPulse : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 45;
        public override float MaxRadius => 72f;
        public override float RadiusExpandRateInterpolant => 0.15f;
        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            return (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio) * 3f;
        }
        public override Color DetermineExplosionColor(float lifetimeCompletionRatio) => Color.Lerp(Color.Cyan, Color.Fuchsia, lifetimeCompletionRatio * 2f % 1f);
    }
}
