using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;

namespace InfernumMode.FuckYouModeAIs.Cultist
{
    public class DeathExplosion : BaseWaveExplosionProjectile
	{
		public override int Lifetime => 80;
		public override float MaxRadius => 2100f;
		public override float RadiusExpandRateInterpolant => 0.15f;
		public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer)
		{
			float baseShakePower = MathHelper.Lerp(3f, 16f, (float)Math.Sin(MathHelper.Pi * lifetimeCompletionRatio));
			return baseShakePower * Utils.InverseLerp(2200f, 1050f, distanceFromPlayer, true);
		}

		public override Color DetermineExplosionColor(float lifetimeCompletionRatio)
		{
			switch ((int)projectile.localAI[1])
			{
				// Vortex.
				case 0:
					return Color.Teal;
				// Stardust.
				case 1:
					return Color.DeepSkyBlue;
				// Nebula.
				case 2:
					return Color.Violet;
				// Solar.
				case 3:
					return Color.Orange;
			}

			return Color.White;
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write((int)projectile.localAI[1]);

		public override void ReceiveExtraAI(BinaryReader reader) => projectile.localAI[1] = reader.ReadInt32();
	}
}
