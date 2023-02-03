using CalamityMod;
using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class PinkLightning : BasePrimitiveLightningProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nebulous Lightning");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailPointCount;
        }

        public override int Lifetime => 72;

        public override int TrailPointCount => 7;

        public override float LightningTurnRandomnessFactor => 0.6f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            List<Vector2> checkPoints = Projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToList();
            if (checkPoints.Count <= 2)
                return false;

            for (int i = 0; i < checkPoints.Count - 1; i++)
            {
                float _ = 0f;
                float width = PrimitiveWidthFunction(i / (float)checkPoints.Count) * 2f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), checkPoints[i], checkPoints[i + 1], width * 0.8f, ref _))
                    return true;
            }
            return false;
        }

        public override float PrimitiveWidthFunction(float completionRatio)
        {
            Projectile.hostile = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
            float baseWidth = MathHelper.Lerp(0.25f, 3.5f, (float)Math.Sin(MathHelper.Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return baseWidth * CalamityUtils.Convert01To010(completionRatio) + 1f;
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.HotPink, Color.Magenta, (float)Math.Sin(MathHelper.TwoPi * completionRatio * 10f + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(baseColor, Color.DarkMagenta, ((float)Math.Sin(MathHelper.Pi * completionRatio * 20f + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f);
        }
    }
}
