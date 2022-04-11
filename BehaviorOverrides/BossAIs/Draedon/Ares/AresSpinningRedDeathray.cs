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
        public ref float Time => ref Projectile.ai[0];
        public ref float OwnerIndex => ref Projectile.ai[1];
        public NPC Owner => Main.npc[(int)OwnerIndex];
        public const float LaserLength = 7000f;
        public const int Lifetime = 480;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exothermal Disintegration Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Owner.active || Owner.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (InitialDirection == -100f)
            {
                InitialDirection = Projectile.velocity.ToRotation();
                Projectile.netUpdate = true;
            }

            Projectile.velocity = (InitialDirection + Owner.Infernum().ExtraAI[2]).ToRotationVector2();
            Projectile.Center = Owner.Center + new Vector2(-14f, 10f) + Projectile.velocity * -4f;

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public float WidthFunction(float completionRatio)
        {
            return MathHelper.Clamp(Projectile.width * Projectile.scale, 0f, Projectile.width);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Red;
            color = Color.Lerp(color, Color.White, ((float)Math.Sin(MathHelper.TwoPi * completionRatio - Main.GlobalTimeWrappedHourly * 1.37f) * 0.5f + 0.5f) * 0.15f + 0.15f);
            color.A = 50;
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["CalamityMod:Bordernado"]);

            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(1.4f);
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/VoronoiShapes").Value);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
            {
                for (float offset = 0f; offset < 5f; offset += 1.2f)
                {
                    BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 35);
                    BeamDrawer.Draw(points, Projectile.Size * 0.5f + (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 35);
                    BeamDrawer.Draw(points, Projectile.Size * 0.5f - (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 35);
                }
            }
            return false;
        }
    }
}
