using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsEyeLaserbeam : ModProjectile
    {
        public PrimitiveTrail RayDrawer = null;
        public NPC Owner => Main.npc[(int)Projectile.ai[0]];
        public ref float LaserLength => ref Projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const int LaserLifetime = 105;

        public const float MaxLaserLength = 3330f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eye Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = LaserLifetime;
        }

        public override void AI()
        {
            // If the owner is no longer able to cast the beam, kill it.
            if (!Owner.active)
            {
                Projectile.Kill();
                return;
            }

            // Grow bigger up to a point.
            Projectile.scale = MathHelper.Clamp(Projectile.scale + (Projectile.timeLeft > 16f).ToDirectionInt() * 0.15f, 0.05f, 2f);

            // Decide where to position the laserbeam.
            Vector2 circlePointDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.Center = DeerclopsBehaviorOverride.GetEyePosition(Owner);

            // Update the laser length.
            float[] laserLengthSamplePoints = new float[24];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.scale * 8f, MaxLaserLength, laserLengthSamplePoints);
            LaserLength = laserLengthSamplePoints.Average() - 10f;

            // Update aim.
            UpdateAim();

            // Create hit effects at the end of the beam.
            if (Main.myPlayer == Projectile.owner)
                CreateTileHitEffects();

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.DarkViolet.ToVector3() * Projectile.scale * 0.4f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }

        public void UpdateAim()
        {
            Projectile.velocity = Owner.Infernum().ExtraAI[1].ToRotationVector2();
        }

        public void CreateTileHitEffects()
        {
            Vector2 endOfLaser = Projectile.Center + Projectile.velocity * LaserLength;
            if (Main.netMode != NetmodeID.Server)
            {
                Vector2 particleSpawnPosition = endOfLaser + Main.rand.NextVector2Circular(10f, 10f) + Projectile.velocity * 40f;
                Vector2 particleVelocity = -Vector2.UnitY.RotatedByRandom(0.73f) * Main.rand.NextFloat(0.4f, 4f);
                GeneralParticleHandler.SpawnParticle(new SeaFoamParticle(particleSpawnPosition, particleVelocity, Color.Orange, Color.Red, Main.rand.NextFloat(0.8f, 1.2f), 225f));
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), endOfLaser, Main.rand.NextVector2Circular(4f, 8f), ModContent.ProjectileType<RancorFog>(), 0, 0f, Projectile.owner);

            if (Main.rand.NextBool(2))
            {
                int type = ModContent.ProjectileType<RancorSmallCinder>();
                int damage = 0;
                float cinderSpeed = Main.rand.NextFloat(2f, 6f);
                Vector2 cinderVelocity = Vector2.Lerp(-Projectile.velocity, -Vector2.UnitY, 0.45f).RotatedByRandom(0.72f) * cinderSpeed;
                Projectile.NewProjectile(Projectile.GetProjectileSource_FromThis(), endOfLaser, cinderVelocity, type, damage, 0f, Projectile.owner);
            }

            if (Projectile.timeLeft % 6 == 5)
                Utilities.NewProjectileBetter(endOfLaser - Vector2.UnitY * 10f, -Vector2.UnitY * 0.072f, ModContent.ProjectileType<EyeGroundFire>(), 95, 0f);
        }

        private float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 12f;

        private Color PrimitiveColorFunction(float completionRatio)
        {
            Color vibrantColor = Color.Lerp(Color.Orange, Color.Red, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.67f - completionRatio / LaserLength * 29f) * 0.5f + 0.5f);
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) *
                Utils.GetLerpValue(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3D);
            return Color.Lerp(vibrantColor, Color.White, 0.3f) * opacity * 2f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrail(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: GameShaders.Misc["CalamityMod:Flame"]);

            GameShaders.Misc["CalamityMod:Flame"].UseImage1("Images/Misc/Perlin");

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * LaserLength * 1.1f;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.Draw(basePoints, overallOffset, 92);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
