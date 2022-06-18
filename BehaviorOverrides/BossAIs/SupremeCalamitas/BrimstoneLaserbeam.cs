using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneLaserbeam : ModProjectile
    {
        public PrimitiveTrail RayDrawer = null;

        public ref float LaserLength => ref projectile.ai[1];

        public const int Lifetime = 360;

        public const float MaxLaserLength = 3330f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.rotation = reader.ReadSingle();

        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1)
            {
                projectile.Kill();
                return;
            }

            // Grow bigger up to a point.
            projectile.scale = MathHelper.Clamp(projectile.scale + 0.15f, 0.05f, 2f);

            // Decide where to position the laserbeam.
            Projectile jewel = Main.projectile[(int)Main.npc[CalamityGlobalNPC.SCal].Infernum().ExtraAI[0]];
            Vector2 circlePointDirection = jewel.rotation.ToRotationVector2();
            projectile.velocity = circlePointDirection;
            projectile.Center = Main.npc[CalamityGlobalNPC.SCal].Center;

            // Update the laser length.
            float[] laserLengthSamplePoints = new float[24];
            Collision.LaserScan(projectile.Center, projectile.velocity, projectile.scale * 24f, MaxLaserLength, laserLengthSamplePoints);
            LaserLength = laserLengthSamplePoints.Average();

            // Create arms on surfaces.
            if (Main.myPlayer == projectile.owner)
                CreateArmsOnSurfaces();

            // Create hit effects at the end of the beam.
            if (Main.myPlayer == projectile.owner)
                CreateTileHitEffects();

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.DarkViolet.ToVector3() * projectile.scale * 0.4f;
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * LaserLength, projectile.width * projectile.scale, DelegateMethods.CastLight);
        }

        public void CreateArmsOnSurfaces()
        {
            Vector2 endOfLaser = projectile.Center + projectile.velocity * LaserLength + Main.rand.NextVector2Circular(80f, 8f);
            Vector2 idealCenter = endOfLaser;
            Utilities.NewProjectileBetter(idealCenter, Vector2.Zero, ModContent.ProjectileType<AcceleratingDarkMagicFlame>(), 525, 0f, projectile.owner);
        }

        public void CreateTileHitEffects()
        {
            Vector2 endOfLaser = projectile.Center + projectile.velocity * (LaserLength - Main.rand.NextFloat(12f, 72f));

            if (Main.rand.NextBool(6))
                Projectile.NewProjectile(endOfLaser, Main.rand.NextVector2Circular(4f, 8f), ModContent.ProjectileType<RancorFog>(), 0, 0f, projectile.owner);

            if (Main.rand.NextBool(2))
            {
                int type = ModContent.ProjectileType<RancorSmallCinder>();
                float cinderSpeed = Main.rand.NextFloat(2f, 6f);
                Vector2 cinderVelocity = Vector2.Lerp(-projectile.velocity, -Vector2.UnitY, 0.45f).RotatedByRandom(0.72f) * cinderSpeed;
                Projectile.NewProjectile(endOfLaser, cinderVelocity, type, 0, 0f, projectile.owner);
            }
        }

        private float PrimitiveWidthFunction(float completionRatio) => projectile.scale * 10f;

        private Color PrimitiveColorFunction(float completionRatio)
        {
            Color vibrantColor = Color.Lerp(Color.Blue, Color.Red, (float)Math.Cos(Main.GlobalTime * 0.67f - completionRatio / LaserLength * 29f) * 0.5f + 0.5f);
            float opacity = projectile.Opacity * Utils.InverseLerp(0.97f, 0.9f, completionRatio, true) *
                Utils.InverseLerp(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.InverseLerp(60f, 270f, LaserLength, true), 3D);
            return Color.Lerp(vibrantColor, Color.White, 0.5f) * opacity * 2f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrail(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: GameShaders.Misc["CalamityMod:Flame"]);

            GameShaders.Misc["CalamityMod:Flame"].UseImage("Images/Misc/Perlin");

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = projectile.Center + projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.Draw(basePoints, overallOffset, 62);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = PrimitiveWidthFunction(0.4f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, projectile.Center + projectile.velocity * LaserLength, width, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) { }

        public override bool ShouldUpdatePosition() => false;
    }
}
