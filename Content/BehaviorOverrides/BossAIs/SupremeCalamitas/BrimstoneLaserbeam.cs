using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Magic;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneLaserbeam : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy RayDrawer;

        public ref float LaserLength => ref Projectile.ai[1];

        public const int Lifetime = 360;

        public const float MaxLaserLength = 3330f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 7200;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.rotation = reader.ReadSingle();

        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1 || !Main.npc[CalamityGlobalNPC.SCal].active)
            {
                Projectile.Kill();
                return;
            }

            // Grow bigger up to a point.
            Projectile.scale = Clamp(Projectile.scale + 0.15f, 0.05f, 2f);

            // Decide where to position the laserbeam.
            Vector2 circlePointDirection = Main.npc[CalamityGlobalNPC.SCal].Infernum().ExtraAI[2].ToRotationVector2();
            Projectile.velocity = circlePointDirection;
            Projectile.Center = Main.npc[CalamityGlobalNPC.SCal].Center;

            // Update the laser length.
            float[] laserLengthSamplePoints = new float[24];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.scale * 24f, MaxLaserLength, laserLengthSamplePoints);
            LaserLength = laserLengthSamplePoints.Average();

            // Create arms on surfaces.
            if (Main.myPlayer == Projectile.owner)
                CreateLavaOnSurfaces();

            // Create hit effects at the end of the beam.
            if (Main.myPlayer == Projectile.owner)
                CreateTileHitEffects();

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.DarkViolet.ToVector3() * Projectile.scale * 0.4f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
        }

        public void CreateLavaOnSurfaces()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 endOfLaser = Projectile.Center + Projectile.velocity * LaserLength;
            RancorLavaMetaball.SpawnParticle(endOfLaser + Main.rand.NextVector2Circular(10f, 10f) + Projectile.velocity * 40f, 320f);
        }

        public void CreateTileHitEffects()
        {
            Vector2 endOfLaser = Projectile.Center + Projectile.velocity * (LaserLength - Main.rand.NextFloat(12f, 72f));

            if (Main.rand.NextBool(6))
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), endOfLaser, Main.rand.NextVector2Circular(4f, 8f), ModContent.ProjectileType<RancorFog>(), 0, 0f, Projectile.owner);

            if (Main.rand.NextBool(2))
            {
                int type = ModContent.ProjectileType<RancorSmallCinder>();
                float cinderSpeed = Main.rand.NextFloat(2f, 6f);
                Vector2 cinderVelocity = Vector2.Lerp(-Projectile.velocity, -Vector2.UnitY, 0.45f).RotatedByRandom(0.72f) * cinderSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), endOfLaser, cinderVelocity, type, 0, 0f, Projectile.owner);
            }
        }

        private float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 10f;

        private Color PrimitiveColorFunction(float completionRatio)
        {
            Color vibrantColor = Color.Lerp(Color.Blue, Color.Red, Cos(Main.GlobalTimeWrappedHourly * 0.67f - completionRatio / LaserLength * 29f) * 0.5f + 0.5f);
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) *
                Utils.GetLerpValue(0f, Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3f);
            return Color.Lerp(vibrantColor, Color.White, 0.3f) * opacity * 2f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            RayDrawer ??= new(PrimitiveWidthFunction, PrimitiveColorFunction, null, true, specialShader: GameShaders.Misc["CalamityMod:Flame"]);

            GameShaders.Misc["CalamityMod:Flame"].UseImage1("Images/Misc/Perlin");

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.DrawPixelated(basePoints, overallOffset, 62);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = PrimitiveWidthFunction(0.4f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, width, ref _);
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft < 7198;
    }
}
