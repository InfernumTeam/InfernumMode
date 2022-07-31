using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class HolyFireBeam : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 360;

        public const float LaserLength = 4800f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fire Beam");

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.holyBoss == -1 ||
                !Main.npc[CalamityGlobalNPC.holyBoss].active ||
                Main.npc[CalamityGlobalNPC.holyBoss].ai[0] != (int)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalBladesWithLaser)
            {
                Projectile.Kill();
                return;
            }

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);
            Projectile.scale = (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * 4f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
            Projectile.velocity = (MathHelper.TwoPi * Projectile.ai[1] + Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[0]).ToRotationVector2();

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
            float squeezeInterpolant = Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(2f, Projectile.width, squeezeInterpolant) * MathHelper.Clamp(Projectile.scale, 0.01f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Orange, Color.DarkRed, (float)Math.Pow(completionRatio, 2D));
            if (!Main.dayTime)
                color = Color.Lerp(Color.Cyan, Color.Lime, (float)Math.Pow(completionRatio, 2D) * 0.5f);

            color *= Projectile.Opacity;
            return color;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, specialShader: GameShaders.Misc["CalamityMod:Flame"]);
            GameShaders.Misc["CalamityMod:Flame"].UseImage1("Images/Misc/Perlin");

            float oldGlobalTime = Main.GlobalTimeWrappedHourly;
            Main.GlobalTimeWrappedHourly %= 1f;

            List<float> originalRotations = new();
            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;

            for (int i = 0; i < 2; i++)
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 32);
            Main.instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.GlobalTimeWrappedHourly = oldGlobalTime;
            return false;
        }
    }
}
