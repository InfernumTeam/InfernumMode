using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System.IO;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class DeathExplosion : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 150;
        public override float MaxRadius => 2100f;
        public override float RadiusExpandRateInterpolant => 0.15f;
        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => 0f;

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            return (int)Projectile.localAI[1] switch
            {
                // Vortex.
                0 => Color.Teal,
                // Stardust.
                1 => Color.DeepSkyBlue,
                // Nebula.
                2 => Color.Violet,
                // Solar.
                3 => Color.Orange,
                _ => Color.White,
            };
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)Projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadInt32();
    }
}
