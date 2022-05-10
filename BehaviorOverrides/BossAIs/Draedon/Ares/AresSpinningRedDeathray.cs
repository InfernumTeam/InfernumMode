using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresSpinningRedDeathray : ModProjectile
    {
        public float InitialDirection = -100f;
        public PrimitiveTrailCopy BeamDrawer;
        public ref float Time => ref projectile.ai[0];
        public ref float OwnerIndex => ref projectile.ai[1];
        public NPC Owner => Main.npc[(int)OwnerIndex];
        public const float LaserLength = 7000f;
        public const int Lifetime = 480;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exothermal Disintegration Deathray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
            projectile.alpha = 255;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Owner.active || Owner.Opacity <= 0f)
            {
                projectile.Kill();
                return;
            }

            if (InitialDirection == -100f)
            {
                InitialDirection = projectile.velocity.ToRotation();
                projectile.netUpdate = true;
            }

            projectile.velocity = (InitialDirection + Owner.Infernum().ExtraAI[2]).ToRotationVector2();
            projectile.Center = Owner.Center + new Vector2(-14f, 10f) + projectile.velocity * -4f;

            // Fade in.
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);

            projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime) * 4f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = projectile.width * 0.8f;
            Vector2 start = projectile.Center;
            Vector2 end = start + projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public float WidthFunction(float completionRatio)
        {
            return MathHelper.Clamp(projectile.width * projectile.scale, 0f, projectile.width);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Red;
            color = Color.Lerp(color, Color.White, ((float)Math.Sin(MathHelper.TwoPi * completionRatio - Main.GlobalTime * 1.37f) * 0.5f + 0.5f) * 0.15f + 0.15f);
            color.A = 50;
            return color * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:Bordernado"]);

            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(1.4f);
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/VoronoiShapes"));

            List<float> originalRotations = new List<float>();
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center + projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
            {
                for (float offset = 0f; offset < 5f; offset += 1.2f)
                {
                    BeamDrawer.Draw(points, projectile.Size * 0.5f - Main.screenPosition, 35);
                    BeamDrawer.Draw(points, projectile.Size * 0.5f + (Main.GlobalTime * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 35);
                    BeamDrawer.Draw(points, projectile.Size * 0.5f - (Main.GlobalTime * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 35);
                }
            }
            return false;
        }
    }
}
