using CalamityMod;
using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
	public class RedLightning2 : BasePrimitiveLightningProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Red Lightning");
			ProjectileID.Sets.TrailingMode[projectile.type] = 1;
			ProjectileID.Sets.TrailCacheLength[projectile.type] = TrailPointCount;
		}

		public override int Lifetime => 60;
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
			cooldownSlot = 1;
			float baseWidth = MathHelper.Lerp(0.25f, 3.5f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * projectile.scale;
			return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio) + 1f;
		}

		public override Color PrimitiveColorFunction(float completionRatio)
		{
			Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
			return Color.Lerp(baseColor, Color.Red, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f);
		}
	}
}
