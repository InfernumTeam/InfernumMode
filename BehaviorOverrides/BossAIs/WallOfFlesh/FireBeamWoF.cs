using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class FireBeamWoF : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;
        public ref float Time => ref Projectile.ai[0];
        public NPC Owner => Main.npc[(int)Projectile.ai[1]];
        public const float LaserLength = 2400f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Flame Beam");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = (float)Math.Sin(Time / 120f * MathHelper.Pi) * 3f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            Projectile.Center = Owner.Center;

            // And create bright light.
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 1.4f);

            if (!Owner.active || Owner.Infernum().ExtraAI[2] == 1f)
                Projectile.Kill();

            Owner.rotation = Projectile.velocity.ToRotation();
            if (Owner.direction < 0)
                Owner.rotation += MathHelper.Pi;

            CreateDustAtBeginning();

            Time++;
        }

        public void CreateDustAtBeginning()
        {
            for (int i = 0; i < 6; i++)
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 50f), 222);
                fire.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.5f, 3.25f);
                fire.velocity *= Main.rand.NextBool(2).ToDirectionInt();
                fire.scale = 1f + fire.velocity.Length() * 0.1f;
                fire.color = Color.Lerp(Color.White, Color.OrangeRed, Main.rand.NextFloat());
                fire.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(0f, 0.05f, completionRatio, true) * Utils.GetLerpValue(1f, 0.95f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.01f, 1f);
        }

        public override bool ShouldUpdatePosition() => false;

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.OrangeRed, Color.DarkRed, (float)Math.Pow(completionRatio, 2D));
            color = Color.Lerp(color, Color.Orange, 0.65f);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.ProviLaserVertexShader);

            Color middleColor = Color.Lerp(Color.White, Color.Orange, 0.6f);
            Color middleColor2 = Color.Lerp(Color.Red, Color.DarkRed, 0.5f);
            Color finalColor = Color.Lerp(middleColor, middleColor2,  Time / 120);

            InfernumEffectsRegistry.ProviLaserVertexShader.UseColor(finalColor);
            InfernumEffectsRegistry.ProviLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThinGlow);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 60);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= 8f;
    }
}
