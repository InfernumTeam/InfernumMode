using InfernumMode.Common.BaseEntities;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using System;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class GuardiansRodFailPulse : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 45;

        public override float MaxRadius => 72f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => Sin(Pi * lifetimeCompletionRatio) * 3f;

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio) => Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[0], lifetimeCompletionRatio * 2f % 1f);
    }
}