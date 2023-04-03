using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Shaders;
using CalamityMod;
using System.Collections.Generic;
using InfernumMode.Assets.Effects;
using Microsoft.Xna.Framework.Graphics;

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
            Projectile.height = 180;
            Projectile.height = 180;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 56f, Projectile.timeLeft, true);
            Projectile.velocity *= 1.06f;

            if (Projectile.timeLeft >= 30)
                Projectile.scale *= 1.033f;
        }

        public float SlashWidthFunction(float completionRatio) => Projectile.scale * Utils.GetLerpValue(0f, 0.35f, completionRatio, true) * Utils.GetLerpValue(1f, 0.65f, completionRatio, true) * 30f;

        public Color SlashColorFunction(float completionRatio) => Color.Red * Utils.GetLerpValue(0.04f, 0.27f, completionRatio, true) * Projectile.Opacity * Projectile.localAI[1];

        public override bool PreDraw(ref Color lightColor)
        {
            // Initialize the drawer.
            SlashDrawer ??= new(SlashWidthFunction, SlashColorFunction, null, InfernumEffectsRegistry.AresEnergySlashShader);

            // Draw the slash effect.
            Main.spriteBatch.EnterShaderRegion();

            List<Vector2> points = new();
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 perpendicularDirection = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 left = Projectile.Center - perpendicularDirection * Projectile.height * Projectile.scale * 0.5f;
            Vector2 right = Projectile.Center + perpendicularDirection * Projectile.height * Projectile.scale * 0.5f;
            Vector2 farLeft = left - direction * Projectile.height * Projectile.scale * 4f;
            Vector2 farRight = right - direction * Projectile.height * Projectile.scale * 4f;

            for (int i = 0; i < 10; i++)
                points.Add(Vector2.CatmullRom(farLeft, left, right, farRight, i / 9f));

            InfernumEffectsRegistry.AresEnergySlashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            InfernumEffectsRegistry.AresEnergySlashShader.SetShaderTexture2(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SwordSlashTexture"));
            for (int i = 0; i < 7; i++)
            {
                Projectile.localAI[1] = 1f - i / 7f;
                SlashDrawer.Draw(points, -Main.screenPosition - direction * i * MathHelper.Clamp(Projectile.velocity.Length(), 0f, Projectile.scale * 10f) * 2f, 56);
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
    }
}
