using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class SpinningPrismLaserbeam : ModProjectile
    {
        public PrimitiveTrail RayDrawer = null;

        public ref float AngularVelocity => ref projectile.ai[0];

        public ref float LaserLength => ref projectile.ai[1];

        public ref float Time => ref projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float MaxLaserLength = 4800f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Prismatic Ray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = LightCloud.LaserLifetime;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Grow bigger up to a point.
            float maxScale = MathHelper.Lerp(0.051f, 1.5f, Utils.InverseLerp(0f, 30f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 16f, Time, true));
            projectile.scale = MathHelper.Clamp(projectile.scale + 0.02f, 0.05f, maxScale);

            // Spin the laserbeam.
            projectile.velocity = projectile.velocity.RotatedBy(AngularVelocity * Utils.InverseLerp(0f, 32f, Time, true));

            // Update the laser length.
            LaserLength = MaxLaserLength;

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * projectile.scale * 0.6f;
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * LaserLength, projectile.width * projectile.scale, DelegateMethods.CastLight);
            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio) => projectile.scale * 27f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = projectile.Opacity * Utils.InverseLerp(0.97f, 0.9f, completionRatio, true) * 
                Utils.InverseLerp(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.InverseLerp(60f, 270f, LaserLength, true), 3D);
            Color c = Main.hslToRgb((completionRatio * 5f + Main.GlobalTime * 0.5f + projectile.identity * 0.3156f) % 1f, 1f, 0.7f) * opacity;
            c.A = 0;

            return c;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrail(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseImage("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = ModContent.GetTexture("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak");

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = projectile.Center + projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.Draw(basePoints, overallOffset, 92);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, projectile.Center + projectile.velocity * LaserLength, projectile.scale * 25f, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsOverWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
