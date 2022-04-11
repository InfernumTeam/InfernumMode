using CalamityMod;
using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class ExoLightning : BasePrimitiveLightningProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exocharge Lightning");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailPointCount;
        }

        public override int Lifetime => 60;
        public override int TrailPointCount => 40;

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
            return baseWidth * (float)Math.Sin(MathHelper.Pi * completionRatio) + 1f;
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color color = CalamityUtils.MulticolorLerp(Projectile.identity % 11f / 11f, CalamityUtils.ExoPalette);
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, false);

            LightningDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 40);
            return false;
        }
    }
}