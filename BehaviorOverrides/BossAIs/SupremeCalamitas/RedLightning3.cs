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
	public class RedLightning3 : BasePrimitiveLightningProjectile
    {
        internal PrimitiveTrailCopy LightningDrawer2;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Lightning");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailPointCount;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override int Lifetime => 60;
        public override int TrailPointCount => 60;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            List<Vector2> checkPoints = Projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToList();
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
            Projectile.hostile = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
            float baseWidth = MathHelper.Lerp(0.25f, 3f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio) + 4.5f;
        }

        public float PrimitiveWidthFunction2(float completionRatio)
        {
            return PrimitiveWidthFunction(completionRatio) * 0.35f;
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            Color color = Color.Lerp(baseColor, Color.Red, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f);
            color.A = 64;
            return color * 0.7f;
        }

        public static Color PrimitiveColorFunction2(float completionRatio) => new(1f, 1f, 1f, 0.1f);

        public override bool PreDraw(ref Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, false);
            if (LightningDrawer2 is null)
                LightningDrawer2 = new PrimitiveTrailCopy(PrimitiveWidthFunction2, PrimitiveColorFunction2, null, false);

            LightningDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 100);
            LightningDrawer2.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 100);
            return false;
        }
    }
}
