using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightOrb : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)projectile.ai[1]) && Main.npc[(int)projectile.ai[1]].active ? Main.npc[(int)projectile.ai[1]] : null;

        public int LaserCount => EmpressOfLightNPC.ShouldBeEnraged ? 12 : 9;

        public float TelegraphInterpolant => Utils.InverseLerp(20f, LaserReleaseDelay, Time, true);

        public float Radius => Owner.Infernum().ExtraAI[0] * (1f - Owner.Infernum().ExtraAI[2]);

        public ref float Time => ref projectile.ai[0];

        public const int OverloadBeamLifetime = 300;

        public const int LaserReleaseDelay = 125;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Light Orb");

        public override void SetDefaults()
        {
            projectile.width = 164;
            projectile.height = 164;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 9000;
            projectile.scale = 0.2f;
        }

        public override void AI()
        {
            int spiralReleaseRate = 25;
            if (Owner is null)
            {
                projectile.Kill();
                return;
            }

            // Die after sufficiently shrunk.
            if (Owner.Infernum().ExtraAI[2] >= 1f)
            {
                projectile.Kill();
                return;
            }

            // Release beams outward once ready.
            if (Time == LaserReleaseDelay)
            {
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WyrmElectricCharge"), projectile.Center);
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightMagicCast"), projectile.Center);

                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                    int laser = Utilities.NewProjectileBetter(projectile.Center, laserDirection, ModContent.ProjectileType<LightOverloadBeam>(), EmpressOfLightNPC.LaserbeamDamage, 0f);
                    if (Main.projectile.IndexInRange(laser))
                        Main.projectile[laser].ai[0] = Owner.whoAmI;
                }
            }

            Player target = Main.player[Owner.target];
            float distanceFromTarget = projectile.Distance(target.Center) - Radius;
            if (distanceFromTarget < 0f)
                distanceFromTarget = 0f;

            float speedAdditive = distanceFromTarget * 0.017f;
            float aimAtTargetInterpolant = Utils.InverseLerp(1260f, 1800f, distanceFromTarget, true);

            if (Time >= LaserReleaseDelay && (Time - LaserReleaseDelay) % 140f == 5f)
                Main.PlayTrackedSound(Utilities.GetTrackableSound("Sounds/Custom/EmpressOfLightBoltCast"), projectile.Center);

            // Release prismatic bolts in a spiral.
            if (Main.netMode != NetmodeID.MultiplayerClient && Time >= LaserReleaseDelay && Time % spiralReleaseRate == spiralReleaseRate - 1f)
            {
                Vector2 spiralVelocity = (MathHelper.TwoPi * (Time - LaserReleaseDelay) / 200f).ToRotationVector2() * (speedAdditive + 12f);
                spiralVelocity = Vector2.Lerp(spiralVelocity, projectile.SafeDirectionTo(target.Center) * spiralVelocity.Length(), aimAtTargetInterpolant * 0.95f);

                int bolt = Utilities.NewProjectileBetter(projectile.Center + spiralVelocity * 2f, spiralVelocity, ModContent.ProjectileType<PrismaticBolt>(), EmpressOfLightNPC.PrismaticBoltDamage, 0f);
                if (Main.projectile.IndexInRange(bolt))
                {
                    Main.projectile[bolt].ai[0] = target.whoAmI;
                    Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                    Main.projectile[bolt].localAI[0] = 0.5f + speedAdditive / 90f;
                }
            }

            Time++;
        }

        public float OrbWidthFunction(float completionRatio) => MathHelper.SmoothStep(0f, Radius, (float)Math.Sin(MathHelper.Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Main.hslToRgb(projectile.localAI[0] % 1f, 1f, 0.56f);
            c.A = 0;
            return c;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(OrbWidthFunction, OrbColorFunction, null, true, GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseOpacity(0.25f);
            GameShaders.Misc["Infernum:PrismaticRay"].UseImage("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = ModContent.GetTexture("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak");

            List<float> rotationPoints = new List<float>();
            List<Vector2> drawPoints = new List<Vector2>();

            // Draw telegraphs.
            if (TelegraphInterpolant >= 0f && TelegraphInterpolant < 1f)
            {
                float telegraphWidth = MathHelper.Lerp(1f, 6f, TelegraphInterpolant);
                for (int i = 0; i < LaserCount; i++)
                {
                    Vector2 laserDirection = (MathHelper.TwoPi * i / LaserCount + 0.8f).ToRotationVector2();
                    Vector2 start = projectile.Center;
                    Vector2 end = projectile.Center + laserDirection * 4200f;
                    Color telegraphColor = Main.hslToRgb((i / (float)LaserCount + Main.GlobalTime * 0.3f) % 1f, 1f, 0.7f) * (float)Math.Pow(TelegraphInterpolant, 0.67);
                    spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                }
            }

            spriteBatch.EnterShaderRegion();
            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 30f)
            {
                projectile.localAI[0] = MathHelper.Clamp((offsetAngle + MathHelper.PiOver2) / MathHelper.Pi, 0f, 1f);

                rotationPoints.Clear();
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + CalamityUtils.PerlinNoise2D(offsetAngle, Main.GlobalTime * 0.02f, 3, 185) * 3f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 4; i++)
                {
                    rotationPoints.Add(adjustedAngle);
                    drawPoints.Add(Vector2.Lerp(projectile.Center - offsetDirection * Radius / 2f, projectile.Center + offsetDirection * Radius / 2f, i / 3f));
                }

                FireDrawer.Draw(drawPoints, -Main.screenPosition, 30);
            }
            spriteBatch.ExitShaderRegion();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utilities.CircularCollision(projectile.Center, targetHitbox, Radius * 0.85f);
    }
}
