using CalamityMod;
using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class OverloadBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 150;
        public override float MaxRadius => 2250f;
        public override float RadiusExpandRateInterpolant => 0.15f;
        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = MathHelper.Lerp(0.45f, 3f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.InverseLerp(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            Color exoColor = CalamityUtils.MulticolorLerp((float)Math.Sin(lifetimeCompletionRatio * MathHelper.Pi * 3f) * 0.5f + 0.5f, CalamityUtils.ExoPalette);
            return Color.Lerp(exoColor, new Color(255, 55, 0, 84), MathHelper.Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f));
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.localAI[1] = reader.ReadInt32();
    }
}
