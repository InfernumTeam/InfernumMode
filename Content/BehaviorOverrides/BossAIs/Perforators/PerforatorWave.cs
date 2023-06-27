using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorWave : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 80;

        public override float MaxRadius => 1500f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = Lerp(1f, 5f, Sin(Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return Color.Lerp(Color.Crimson, Color.Gray, Clamp(lifetimeCompletionRatio * 1.5f, 0f, 1f));
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)Projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadInt32();
    }
}
