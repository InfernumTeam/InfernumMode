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

        public ref float Time => ref projectile.ai[0];

        public const int Lifetime = 360;

        public const float LaserLength = 4800f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fire Beam");

        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
            projectile.Opacity = 0f;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.holyBoss == -1 ||
                !Main.npc[CalamityGlobalNPC.holyBoss].active ||
                Main.npc[CalamityGlobalNPC.holyBoss].ai[0] != (int)ProvidenceBehaviorOverride.ProvidenceAttackType.CrystalBladesWithLaser)
            {
                projectile.Kill();
                return;
            }

            // Fade in.
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);
            projectile.scale = (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * 4f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;
            projectile.velocity = (MathHelper.TwoPi * projectile.ai[1] + Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[0]).ToRotationVector2();

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
            float squeezeInterpolant = Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(2f, projectile.width, squeezeInterpolant) * MathHelper.Clamp(projectile.scale, 0.01f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Orange, Color.DarkRed, (float)Math.Pow(completionRatio, 2D));
            color *= projectile.Opacity;
            return color;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, specialShader: GameShaders.Misc["CalamityMod:Flame"]);
            GameShaders.Misc["CalamityMod:Flame"].UseImage("Images/Misc/Perlin");

            float oldGlobalTime = Main.GlobalTime;
            Main.GlobalTime %= 1f;

            List<float> originalRotations = new List<float>();
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center + projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            BeamDrawer.Draw(points, projectile.Size * 0.5f - Main.screenPosition, 32);
            BeamDrawer.Draw(points, projectile.Size * 0.5f - Main.screenPosition, 32);
            Main.instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.GlobalTime = oldGlobalTime;
            return false;
        }
    }
}
