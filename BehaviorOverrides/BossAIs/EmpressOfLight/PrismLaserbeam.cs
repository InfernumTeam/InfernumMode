using CalamityMod;
using CalamityMod.Buffs;
using CalamityMod.Particles;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class PrismLaserbeam : ModProjectile
    {
        public PrimitiveTrail RayDrawer = null;
        public Projectile Prism
        {
            get
            {
                int prismID = ModContent.ProjectileType<EmpressPrism>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].identity != projectile.ai[0] || !Main.projectile[i].active || Main.projectile[i].type != prismID)
                        continue;

                    return Main.projectile[i];
                }
                return null;
            }
        }
        public ref float LaserLength => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float MaxLaserLength = 3330f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Prismatic Ray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.hide = true;
            projectile.netImportant = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // If the prism is no longer able to cast the beam, kill it.
            if (Prism is null)
            {
                projectile.Kill();
                return;
            }

            // Grow bigger up to a point.
            float maxScale = MathHelper.Lerp(0.051f, 1.5f, Utils.GetLerpValue(0f, 30f, Prism.timeLeft, true));
            projectile.scale = MathHelper.Clamp(projectile.scale + 0.02f, 0.05f, maxScale);

            // Decide where to position the laserbeam.
            projectile.velocity = Prism.localAI[0].ToRotationVector2();
            projectile.Center = Prism.Center;

            // Update the laser length.
            LaserLength = MaxLaserLength;

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * projectile.scale * 0.6f;
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * LaserLength, projectile.width * projectile.scale, DelegateMethods.CastLight);
        }

        internal float PrimitiveWidthFunction(float completionRatio) => projectile.scale * 20f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) * 
                Utils.GetLerpValue(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3D);
            Color c = Main.hslToRgb((completionRatio * 3f + Main.GlobalTime * 0.5f + projectile.identity * 0.3156f) % 1f, 1f, 0.7f) * opacity;
            c.A /= 16;

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
