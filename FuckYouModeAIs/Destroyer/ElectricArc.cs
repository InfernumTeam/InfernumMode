using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace InfernumMode.FuckYouModeAIs.Destroyer
{
	public class ElectricArc : BasePrimitiveLightningProjectile
	{
		public override int Lifetime => 90;
		public override int TrailPointCount => 150;

		public override float PrimitiveWidthFunction(float completionRatio)
		{
			float baseWidth = MathHelper.Lerp(4f, 9f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * projectile.scale;
			return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio);
		}

		public override Color PrimitiveColorFunction(float completionRatio)
		{
			Color baseColor = Color.Lerp(Color.Cyan, Color.LightBlue, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
			return Color.Lerp(baseColor, Color.LightCyan, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f);
		}
	}
}
