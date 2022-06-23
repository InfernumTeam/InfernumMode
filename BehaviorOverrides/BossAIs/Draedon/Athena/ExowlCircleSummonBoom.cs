using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class ExowlCircleSummonBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 45;
        public override float MaxRadius => 1300f;
        public override float RadiusExpandRateInterpolant => 0.15f;
        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(1f, 4f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Fuchsia, Color.Lime, MathHelper.Clamp(lifetimeCompletionRatio * 1.35f, 0f, 1f));
        }
    }
}
