using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class BereftVassalTeleportBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 18;

        public override float MaxRadius => 100f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => 0f;

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            Projectile.Opacity = 1.8f;
            return Color.Lerp(Color.DarkOrange, Color.DarkViolet, MathHelper.Clamp(lifetimeCompletionRatio * 1.2f, 0f, 1f));
        }
    }
}
