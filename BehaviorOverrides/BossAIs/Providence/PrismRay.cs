using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
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
            projectile.penetrate = -1;
            projectile.alpha = 255;
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
            bool playerIsFarFromLaser = offsetAngleFromTarget > MathHelper.PiOver2 * 1.4f;
            if (Time <= 60f)
                RotationalSpeed = Time / 60f * InitialRotationalSpeed;
            else if (playerIsFarFromLaser)
                RotationalSpeed *= 1.01f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.EnterShaderRegion(spriteBatch);

            // Apply a super special shader to the laser.
            MiscShaderData gradientShader = GameShaders.Misc["Infernum:GradientWingShader"];
            gradientShader.UseImage("Images/Misc/Noise");
            gradientShader.UseOpacity(0.7f);

            // Use reflection to set the image (fuck you Terraria).
            typeof(MiscShaderData).GetField("_uImage", BindingFlags.NonPublic | BindingFlags.Instance).
                SetValue(gradientShader, new Ref<Texture2D>(ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTextureTransparent")));

            // And draw the wings.
            gradientShader.Apply(null);

            base.PreDraw(spriteBatch, lightColor);

            Utilities.ExitShaderRegion(spriteBatch);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
