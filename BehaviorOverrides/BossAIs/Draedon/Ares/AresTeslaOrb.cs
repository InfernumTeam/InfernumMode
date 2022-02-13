using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresTeslaOrb : ModProjectile
    {
        public ref float Identity => ref projectile.ai[0];
        public PrimitiveTrail LightningDrawer;
        public PrimitiveTrail LightningBackgroundDrawer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tesla Sphere");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 125;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.velocity.Length() < 26f)
                projectile.velocity *= 1.01f;

            projectile.Opacity = Utils.InverseLerp(125f, 120f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 15f, projectile.timeLeft, true);

            // Emit light.
            Lighting.AddLight(projectile.Center, 0.1f * projectile.Opacity, 0.25f * projectile.Opacity, 0.25f * projectile.Opacity);

            // Handle frames.
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

            // Create a burst of dust on the first frame.
            if (projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 60; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(projectile.Center, Main.rand.NextBool() ? 206 : 229);
                    electricity.position += Main.rand.NextVector2Circular(20f, 20f);
                    electricity.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 16f);
                    electricity.fadeIn = 1f;
                    electricity.color = Color.Cyan * 0.6f;
                    electricity.scale *= Main.rand.NextFloat(1.5f, 2f);
                    electricity.noGravity = true;
                }
                projectile.localAI[0] = 1f;
            }
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.Electrified, 240);
        }

        public Projectile GetOrbToAttachTo()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime < 0 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)
                return null;

            float detachDistance = 1420f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != projectile.type || Main.projectile[i].ai[0] != Identity + 1f || !Main.projectile[i].active)
                    continue;

                if (Vector2.Distance(projectile.Center, Main.projectile[i].Center) > detachDistance)
                    continue;

                return Main.projectile[i];
            }

            return null;
        }

        public static List<Vector2> DetermineElectricArcPoints(Vector2 start, Vector2 end, int seed)
        {
            List<Vector2> points = new List<Vector2>();

            // Determine the base points based on a linear path from the start the end end point.
            for (int i = 0; i <= 75; i++)
                points.Add(Vector2.Lerp(start, end, i / 73.5f));

            // Then, add continuous randomness to the positions of various points.
            for (int i = 0; i < points.Count; i++)
            {
                float completionRatio = i / (float)points.Count;

                // Noise offsets should taper off at the ends of the line.
                float offsetMuffleFactor = Utils.InverseLerp(0.12f, 0.25f, completionRatio, true) * Utils.InverseLerp(0.88f, 0.75f, completionRatio, true);

                // Give a sense of time for the noise on the vertical axis. This is achieved via a 0-1 constricted sinusoid.
                float noiseY = (float)Math.Cos(completionRatio * 17.2f + Main.GlobalTime * 10.7f) * 0.5f + 0.5f;

                float noise = CalamityUtils.PerlinNoise2D(completionRatio, noiseY, 2, seed);

                // Now that the noise value has been computed, convert it to a direction by treating the noise as an angle
                // and then converting it into a unit vector.
                Vector2 offsetDirection = (noise * MathHelper.Pi * 0.7f).ToRotationVector2();

                // Then, determine the factor of the offset. This is based on the initial direction (but squashed) and the muffle factor from above.
                Vector2 offset = offsetDirection * (float)Math.Pow(offsetDirection.Y, 2D) * offsetMuffleFactor * 15f;

                points[i] += offset;
            }

            return points;
        }

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(0.75f, 1.85f, (float)Math.Sin(MathHelper.Pi * completionRatio)) * projectile.scale;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeToWhite = MathHelper.Lerp(0f, 0.65f, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f);
            Color baseColor = Color.Lerp(Color.Cyan, Color.White, fadeToWhite);
            return Color.Lerp(baseColor, Color.LightBlue, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTime * 4f) * 0.5f + 0.5f) * 0.8f) * projectile.Opacity;
        }

        internal float BackgroundWidthFunction(float completionRatio) => WidthFunction(completionRatio) * 4f;

        internal Color BackgroundColorFunction(float completionRatio)
        {
            Color color = Color.CornflowerBlue * projectile.Opacity * 0.4f;
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            if (LightningBackgroundDrawer is null)
                LightningBackgroundDrawer = new PrimitiveTrail(BackgroundWidthFunction, BackgroundColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);

            Projectile orbToAttachTo = GetOrbToAttachTo();
            if (orbToAttachTo != null)
            {
                List<Vector2> arcPoints = DetermineElectricArcPoints(projectile.Center, orbToAttachTo.Center, 117);
                LightningBackgroundDrawer.Draw(arcPoints, -Main.screenPosition, 90);
                LightningDrawer.Draw(arcPoints, -Main.screenPosition, 90);
            }

            lightColor.R = (byte)(255 * projectile.Opacity);
            lightColor.G = (byte)(255 * projectile.Opacity);
            lightColor.B = (byte)(255 * projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float _ = 0f;
            Projectile orbToAttachTo = GetOrbToAttachTo();
            if (orbToAttachTo != null && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, orbToAttachTo.Center, 8f, ref _))
                return true;

            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item93, projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into electric sparks on death.
            for (int i = 0; i < 4; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 5.6f;
                Utilities.NewProjectileBetter(projectile.Center, sparkVelocity, ModContent.ProjectileType<TeslaSpark>(), 500, 0f);
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
