using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresDeathray : ModProjectile
    {
        public PrimitiveTrailCopy BeamDrawer;
        public ref float Time => ref Projectile.ai[0];
        public ref float OwnerIndex => ref Projectile.ai[1];
        public NPC Owner => Main.npc[(int)OwnerIndex];
        public const float LaserLength = 4800f;
        public const int Lifetime = 90;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exothermal Disintegration Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 125;
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

            Projectile.Center = Owner.Center + Vector2.UnitY * 26f + Projectile.velocity * 18f;

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
            color = Color.Lerp(color, Color.White, ((float)Math.Sin(MathHelper.TwoPi * completionRatio - Main.GlobalTimeWrappedHourly * 1.37f) * 0.5f + 0.5f) * 0.67f);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(1.4f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/CultistRayMap").Value);

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
            {
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 47);
                BeamDrawer.Draw(points, Projectile.Size * 0.5f + (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * 2f - Main.screenPosition, 47);
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * 2f - Main.screenPosition, 47);
            }
            return false;
        }
    }
}
