using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class DoomBeam : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;
        public ref float Time => ref projectile.ai[0];
        public const float LaserLength = 4000f;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Death Beam");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 48;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 125;
            projectile.alpha = 255;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY);

            // Fade in.
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);

            projectile.scale = (float)Math.Sin(MathHelper.Pi * Time / 125f) * 3f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;

            // And create bright light.
            Lighting.AddLight(projectile.Center, Color.Purple.ToVector3() * 1.4f);

            CreateDustAtBeginning();
            projectile.Center -= projectile.velocity;

            Time++;
        }

        public void CreateDustAtBeginning()
		{
            for (int i = 0; i < 4; i++)
            {
                Dust fire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 223);
                fire.velocity = -Vector2.UnitY * Main.rand.NextFloat(2.5f, 5.25f);
                fire.scale = 1f + fire.velocity.Length() * 0.1f;
                fire.color = Color.Lerp(Color.White, Color.OrangeRed, Main.rand.NextFloat());
                fire.noGravity = true;
            }
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
            float squeezeInterpolant = Utils.InverseLerp(0f, 0.03f, completionRatio, true) * Utils.InverseLerp(1f, 0.97f, completionRatio, true);
            return MathHelper.SmoothStep(2f, projectile.width, squeezeInterpolant) * MathHelper.Clamp(projectile.scale, 0.04f, 1f);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Purple, Color.DarkBlue, (float)Math.Pow(completionRatio, 2D));
            color = Color.Lerp(color, Color.DarkRed, ((float)Math.Sin(MathHelper.TwoPi * completionRatio - Main.GlobalTime * 1.37f) * 0.5f + 0.5f) * 0.3f);
            return color * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (BeamDrawer is null)
                BeamDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(1.4f);
            GameShaders.Misc["Infernum:Fire"].SetShaderTexture(ModContent.GetTexture("InfernumMode/ExtraTextures/CultistRayMap"));

            List<float> originalRotations = new List<float>();
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                points.Add(Vector2.Lerp(projectile.Center, projectile.Center + projectile.velocity * LaserLength, i / 8f));
                originalRotations.Add(MathHelper.PiOver2);
            }

            if (Time >= 2f)
                BeamDrawer.Draw(points, projectile.Size * 0.5f - Main.screenPosition, 67);
            return false;
        }
    }
}
