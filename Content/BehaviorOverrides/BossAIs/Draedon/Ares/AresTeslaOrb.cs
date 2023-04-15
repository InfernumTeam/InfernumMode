using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresTeslaOrb : ModProjectile
    {
        public PrimitiveTrail LightningDrawer;

        public PrimitiveTrail LightningBackgroundDrawer;

        public ref float Identity => ref Projectile.ai[0];

        public static float DetatchmentDistance => 900f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tesla Sphere");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 125;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.velocity.Length() < 26f)
                Projectile.velocity *= 1.0065f;

            Projectile.Opacity = Utils.GetLerpValue(125f, 120f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true);

            // Emit light.
            Lighting.AddLight(Projectile.Center, 0.1f * Projectile.Opacity, 0.25f * Projectile.Opacity, 0.25f * Projectile.Opacity);

            // Handle frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

            // Create a burst of dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 60; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 206 : 229);
                    electricity.position += Main.rand.NextVector2Circular(20f, 20f);
                    electricity.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 16f);
                    electricity.fadeIn = 1f;
                    electricity.color = Color.Cyan * 0.6f;
                    electricity.scale *= Main.rand.NextFloat(1.5f, 2f);
                    electricity.noGravity = true;
                }
                Projectile.localAI[0] = 1f;
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public Projectile GetOrbToAttachTo()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime < 0 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)
                return null;

            float detachDistance = DetatchmentDistance;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != Projectile.type || Main.projectile[i].ai[0] != Identity + 1f || !Main.projectile[i].active)
                    continue;

                if (Vector2.Distance(Projectile.Center, Main.projectile[i].Center) > detachDistance)
                    continue;

                return Main.projectile[i];
            }

            return null;
        }

        public static List<Vector2> DetermineElectricArcPoints(Vector2 start, Vector2 end, int seed)
        {
            List<Vector2> points = new();

            // Determine the base points based on a linear path from the start the end end point.
            for (int i = 0; i <= 75; i++)
                points.Add(Vector2.Lerp(start, end, i / 73.5f));

            // Then, add continuous randomness to the positions of various points.
            for (int i = 0; i < points.Count; i++)
            {
                float completionRatio = i / (float)points.Count;

                // Noise offsets should taper off at the ends of the line.
                float offsetMuffleFactor = Utils.GetLerpValue(0.12f, 0.25f, completionRatio, true) * Utils.GetLerpValue(0.88f, 0.75f, completionRatio, true);

                // Give a sense of time for the noise on the vertical axis. This is achieved via a 0-1 constricted sinusoid.
                float noiseY = (float)Math.Cos(completionRatio * 17.2f + Main.GlobalTimeWrappedHourly * 10.7f) * 0.5f + 0.5f;

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
            return MathHelper.Lerp(0.75f, 1.85f, (float)Math.Sin(MathHelper.Pi * completionRatio)) * Projectile.scale;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeToWhite = MathHelper.Lerp(0f, 0.65f, (float)Math.Sin(MathHelper.TwoPi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            Color baseColor = Color.Lerp(Color.Cyan, Color.White, fadeToWhite);
            return Color.Lerp(baseColor, Color.LightBlue, ((float)Math.Sin(MathHelper.Pi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f) * Projectile.Opacity;
        }

        internal float BackgroundWidthFunction(float completionRatio) => WidthFunction(completionRatio) * 4f;

        internal Color BackgroundColorFunction(float completionRatio)
        {
            Color color = Color.CornflowerBlue * Projectile.Opacity * 0.4f;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrail(WidthFunction, ColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            if (LightningBackgroundDrawer is null)
                LightningBackgroundDrawer = new PrimitiveTrail(BackgroundWidthFunction, BackgroundColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);

            Projectile orbToAttachTo = GetOrbToAttachTo();
            if (orbToAttachTo != null)
            {
                List<Vector2> arcPoints = DetermineElectricArcPoints(Projectile.Center, orbToAttachTo.Center, 117);
                LightningBackgroundDrawer.Draw(arcPoints, -Main.screenPosition, 90);
                LightningDrawer.Draw(arcPoints, -Main.screenPosition, 90);
            }

            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float _ = 0f;
            Projectile orbToAttachTo = GetOrbToAttachTo();
            if (orbToAttachTo != null && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, orbToAttachTo.Center, 8f, ref _))
                return true;

            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into electric sparks on death.
            for (int i = 0; i < 4; i++)
            {
                Vector2 sparkVelocity = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 5.6f;
                Utilities.NewProjectileBetter(Projectile.Center, sparkVelocity, ModContent.ProjectileType<AresTeslaSpark>(), DraedonBehaviorOverride.NormalShotDamage, 0f);
            }
        }
    }
}
