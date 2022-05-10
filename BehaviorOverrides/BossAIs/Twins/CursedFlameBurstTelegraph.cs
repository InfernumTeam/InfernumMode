using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class CursedFlameBurstTelegraph : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 42;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 30;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 30f * MathHelper.Pi) * 1.6f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(0f, 0.27f, completionRatio, true), 0.4f) * Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, projectile.width, squeezeInterpolant);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Green, Color.LimeGreen, (float)Math.Pow(completionRatio, 2D));
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 3D);
            return color * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utils.DrawLine(spriteBatch, projectile.Center - Vector2.UnitX * 1600f, projectile.Center + Vector2.UnitX * 1600f, Color.LimeGreen, Color.LimeGreen, projectile.Opacity * 1.6f + 0.1f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(projectile.Center, projectile.velocity, ModContent.ProjectileType<CursedFlameBurst>(), 115, 0f);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool CanDamage() => false;
    }
}
