using System.IO;
using CalamityMod;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class OverloadBoom : BaseWaveExplosionProjectile
    {
        public override int Lifetime => 150;

        public override float MaxRadius => 2250f;

        public override float RadiusExpandRateInterpolant => 0.15f;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
        {
            float baseShakePower = Lerp(0.45f, 3f, Sin(Pi * lifetimeCompletionRatio));
            return baseShakePower * Utils.GetLerpValue(2200f, 1050f, distanceFromPlayer, true);
        }

        public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
        {
            Color exoColor = LumUtils.MulticolorLerp(Sin(lifetimeCompletionRatio * Pi * 3f) * 0.5f + 0.5f, CalamityUtils.ExoPalette);
            return Color.Lerp(exoColor, new Color(255, 55, 0, 84), Clamp(lifetimeCompletionRatio * 1.75f, 0f, 1f));
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)Projectile.localAI[1]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadInt32();
    }
}
