using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightOrb : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)Projectile.ai[1]) && Main.npc[(int)Projectile.ai[1]].active ? Main.npc[(int)Projectile.ai[1]] : null;

        public static int LaserCount => EmpressOfLightBehaviorOverride.ShouldBeEnraged ? 12 : 9;

        public float TelegraphInterpolant => Utils.GetLerpValue(20f, LaserReleaseDelay, Time, true);

        public float Radius => Owner.Infernum().ExtraAI[0] * (1f - Owner.Infernum().ExtraAI[2]);

        public ref float Time => ref Projectile.ai[0];

        public const int OverloadBeamLifetime = 300;

        public const int LaserReleaseDelay = 125;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Orb");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 164;
            Projectile.height = 164;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9000;
            Projectile.scale = 0.2f;
        }

        public override void AI()
        {
            int spiralReleaseRate = 13;
            if (Owner is null)
            {
                Projectile.Kill();
                return;
            }

            // Die after sufficiently shrunk.
            if (Owner.Infernum().ExtraAI[2] >= 1f)
            {
                Projectile.Kill();
                return;
            }

            // Release beams outward once ready.
            if (Time == LaserReleaseDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item163, Projectile.Center);

                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                    int laser = Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<LightOverloadBeam>(), EmpressOfLightBehaviorOverride.LaserbeamDamage, 0f);
                    if (Main.projectile.IndexInRange(laser))
                        Main.projectile[laser].ai[0] = Owner.whoAmI;
                }
            }

            Player target = Main.player[Owner.target];
            float distanceFromTarget = Projectile.Distance(target.Center) - Radius;
            if (distanceFromTarget < 0f)
                distanceFromTarget = 0f;

            float speedAdditive = distanceFromTarget * 0.017f;
            float speedFactor = speedAdditive / 90f + 0.5f;
            float aimAtTargetInterpolant = Utils.GetLerpValue(1260f, 1800f, distanceFromTarget, true);

            if (BossRushEvent.BossRushActive)
                speedFactor *= 1.35f;

            if (Time >= LaserReleaseDelay && (Time - LaserReleaseDelay) % 140f == 6f)
                SoundEngine.PlaySound(SoundID.Item164, Projectile.Center);

            // Release prismatic bolts in a spiral.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= LaserReleaseDelay && Time % spiralReleaseRate == spiralReleaseRate - 1f)
            {
                Vector2 spiralVelocity = (MathHelper.TwoPi * (Time - LaserReleaseDelay) / 200f).ToRotationVector2() * (speedAdditive + 12f);
                spiralVelocity = Vector2.Lerp(spiralVelocity, Projectile.SafeDirectionTo(target.Center) * spiralVelocity.Length(), aimAtTargetInterpolant * 0.95f);

                int bolt = Utilities.NewProjectileBetter(Projectile.Center + spiralVelocity * 2f, spiralVelocity, ModContent.ProjectileType<PrismaticBolt>(), EmpressOfLightBehaviorOverride.PrismaticBoltDamage, 0f);
                if (Main.projectile.IndexInRange(bolt))
                {
                    Main.projectile[bolt].ai[0] = target.whoAmI;
                    Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                    Main.projectile[bolt].localAI[0] = speedFactor;
                }
            }

            Time++;
        }

        public float OrbWidthFunction(float completionRatio) => MathHelper.SmoothStep(0f, Radius, (float)Math.Sin(MathHelper.Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Main.hslToRgb(Projectile.localAI[0] % 1f, 1f, 0.56f);
            c.A = 0;
            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null || !Owner.active)
                return false;

            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseOpacity(0.25f);
            GameShaders.Misc["Infernum:PrismaticRay"].UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak").Value;

            List<float> rotationPoints = new();
            List<Vector2> drawPoints = new();

            // Draw telegraphs.
            if (TelegraphInterpolant is >= 0 and < 1)
            {
                float telegraphWidth = MathHelper.Lerp(1f, 6f, TelegraphInterpolant);
                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                    Vector2 start = Projectile.Center;
                    Vector2 end = Projectile.Center + laserDirection * 4200f;
                    Color telegraphColor = Main.hslToRgb((i / (float)LaserCount + Main.GlobalTimeWrappedHourly * 0.3f) % 1f, 1f, 0.7f) * (float)Math.Pow(TelegraphInterpolant, 0.67);
                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                }
            }

            Main.spriteBatch.EnterShaderRegion();
            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 30f)
            {
                Projectile.localAI[0] = MathHelper.Clamp((offsetAngle + MathHelper.PiOver2) / MathHelper.Pi, 0f, 1f);

                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + CalamityUtils.PerlinNoise2D(offsetAngle, Main.GlobalTimeWrappedHourly * 0.02f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 4; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * Radius / 2f, Projectile.Center + offsetDirection * Radius / 2f, i / 3f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(Projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
