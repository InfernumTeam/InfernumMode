using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalDeathray : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;
        public int OwnerIndex;

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public ref float InitialRotationalOffset => ref Projectile.localAI[0];

        public const float LaserLength = 4000f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Phantasmal Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (OwnerIndex <= 0 || Main.npc[OwnerIndex - 1].ai[0] == -2f)
            {
                Projectile.Kill();
                return;
            }

            NPC head = Main.npc[OwnerIndex - 1];
            Projectile.Center = head.Center + new Vector2(-6f, -10f);

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / Lifetime) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            if (Time >= Lifetime)
                Projectile.Kill();

            // And create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.6f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.92f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.04f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Turquoise, Color.Cyan, (float)Math.Pow(completionRatio, 2D));
            return color * Projectile.Opacity * 1.1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            GameShaders.Misc["Infernum:Fire"].UseSaturation(1.4f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/CultistRayMap"));

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f) - Projectile.velocity * 500f);
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 26);
            Main.instance.GraphicsDevice.BlendState = oldBlendState;
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<Nightwither>(), 300);

        public override bool ShouldUpdatePosition() => false;
    }
}
