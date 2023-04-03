using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Shaders;
using CalamityMod;
using System.Collections.Generic;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresEnergySlash : ModProjectile
    {
        public Vector2[] ControlPoints;

        public PrimitiveTrail SlashDrawer
        {
            get;
            set;
        }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exo Energy Slash");
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 144;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 26f, Projectile.timeLeft, true);
            Projectile.velocity *= 1.06f;
            Projectile.scale *= 1.01f;
        }

        public float SlashWidthFunction(float completionRatio) => Projectile.scale * 22f;

        public Color SlashColorFunction(float completionRatio) => Color.White * Utils.GetLerpValue(0.04f, 0.27f, completionRatio, true) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            // Initialize the drawer.
            SlashDrawer ??= new(SlashWidthFunction, SlashColorFunction, null, GameShaders.Misc["CalamityMod:ExobladeSlash"]);

            // Draw the slash effect.
            Main.spriteBatch.EnterShaderRegion();
            //AresEnergyKatana.PrepareSlashShader();

            List<Vector2> points = new();
            for (int i = 0; i < ControlPoints.Length; i++)
                points.Add(ControlPoints[i] + ControlPoints[i].SafeNormalize(Vector2.Zero) * (Projectile.scale - 1f) * 40f);

            for (int i = 0; i < 3; i++)
                SlashDrawer.Draw(points, Projectile.Center - Main.screenPosition, 36);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
    }
}
