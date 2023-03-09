using CalamityMod;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsP2Wave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 180;

        public override float MaxRadius => 2000f;

        public override float RadiusExpandRateInterpolant => 0.12f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(2f, 9f, CalamityUtils.Convert01To010(lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Black, new(86, 36, 181), MathHelper.Clamp(lifetimeCompletionRatio * 1.5f, 0f, 1f));
        }
    }
}
