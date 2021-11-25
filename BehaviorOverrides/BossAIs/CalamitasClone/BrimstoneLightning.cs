using CalamityMod;
using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
	public class BrimstoneLightning : BasePrimitiveLightningProjectile
	{
		public override int Lifetime => 90;
		public override int TrailPointCount => 150;


		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			List<Vector2> checkPoints = projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToList();
			if (checkPoints.Count <= 2)
				return false;

			for (int i = 0; i < checkPoints.Count - 1; i++)
			{
				float _ = 0f;
				float width = PrimitiveWidthFunction(i / (float)checkPoints.Count);
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), checkPoints[i], checkPoints[i + 1], width * 0.8f, ref _))
					return true;
			}
			return false;
		}

		public override float PrimitiveWidthFunction(float completionRatio)
		{
			projectile.hostile = true;
			projectile.Calamity().canBreakPlayerDefense = true;

			float baseWidth = MathHelper.Lerp(2f, 6f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * projectile.scale;
			return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio);
		}

		public override Color PrimitiveColorFunction(float completionRatio)
		{
			Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
			return Color.Lerp(baseColor, Color.Red, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f);
		}
	}
}
