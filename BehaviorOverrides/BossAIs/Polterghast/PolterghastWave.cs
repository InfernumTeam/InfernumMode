using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastWave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 80;

        public override float MaxRadius => 1700f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(1f, 5f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Cyan, Color.Pink * 0.8f, MathHelper.Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f));
        }
    }
}
