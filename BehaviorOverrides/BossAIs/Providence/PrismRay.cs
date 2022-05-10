using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class PrismRay : BaseLaserbeamProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float HueOffset => ref projectile.ai[1];
        public override float MaxScale => 2.6f;
        public override float MaxLaserLength => 2800f;
        public override float Lifetime => LaserLifetime;
        public override Color LaserOverlayColor => Main.hslToRgb((float)Math.Sin(Main.GlobalTime * 1.04f + HueOffset) * 0.5f + 0.5f, 1f, 0.5f) * Utils.InverseLerp(Lifetime, Lifetime - 20f, Time, true);
        public override Color LightCastColor => LaserOverlayColor;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayStart");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd");

        public float InitialRotationalSpeed = 0f;
        // To allow easy, static access from different locations.
        public const int LaserLifetime = 240;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Ray");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 25;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AttachToSomething()
        {
            projectile.Center = Main.npc[CalamityGlobalNPC.holyBoss].Center + projectile.velocity.SafeNormalize(Vector2.Zero) * 10f;
            if (InitialRotationalSpeed == 0f)
            {
                InitialRotationalSpeed = RotationalSpeed;
                RotationalSpeed = 0f;
                projectile.netUpdate = true;
            }

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            float offsetAngleFromTarget = Math.Abs(MathHelper.WrapAngle(projectile.velocity.ToRotation() - projectile.AngleTo(target.Center)));
            if (Time <= 60f)
                RotationalSpeed = Time / 60f * InitialRotationalSpeed;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();

            // Apply a super special shader to the laser.
            Texture2D laserTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTextureTransparent");
            if (!Main.dayTime)
                laserTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTextureTransparentNight");

            MiscShaderData gradientShader = GameShaders.Misc["Infernum:GradientWingShader"];
            gradientShader.UseImage("Images/Misc/Noise");
            gradientShader.UseOpacity(0.7f);
            gradientShader.SetShaderTexture(laserTexture);

            // And draw the laser.
            gradientShader.Apply(null);
            base.PreDraw(spriteBatch, lightColor);

            Utilities.ExitShaderRegion(spriteBatch);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}