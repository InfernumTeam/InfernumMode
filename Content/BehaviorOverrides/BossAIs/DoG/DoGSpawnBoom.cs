using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class DoGSpawnBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 180;

        public override float MaxRadius => 3900f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(3f, 16f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Cyan, Color.Fuchsia, MathHelper.Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f));
        }
    }
}
