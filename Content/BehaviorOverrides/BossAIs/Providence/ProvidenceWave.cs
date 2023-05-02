using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceWave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 90;

        public override float MaxRadius => 3000f;

        public override float RadiusExpandRateInterpolant => 0.07f;

        public override float Opacity => 0.5f;

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(1f, 5f, MathF.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return ProvidenceBehaviorOverride.IsEnraged
                ? Color.Lerp(Color.CadetBlue, Color.Gray, MathHelper.Clamp(lifetimeCompletionRatio * 1.5f, 0f, 1f))
                : Color.Lerp(Color.Goldenrod, Color.Gray, MathHelper.Clamp(lifetimeCompletionRatio * 1.5f, 0f, 1f));
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)Projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadInt32();
    }
}
