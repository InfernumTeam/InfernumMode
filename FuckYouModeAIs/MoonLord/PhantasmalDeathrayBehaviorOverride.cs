using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class PhantasmalDeathrayBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalDeathray;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            Vector2? vector78 = null;

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            if (Main.npc[(int)projectile.ai[1]].active && Main.npc[(int)projectile.ai[1]].type == NPCID.MoonLordHead)
            {
                Vector2 value21 = new Vector2(27f, 59f);
                Vector2 value22 = Utils.Vector2FromElipse(Main.npc[(int)projectile.ai[1]].localAI[0].ToRotationVector2(), value21 * Main.npc[(int)projectile.ai[1]].localAI[1]);
                projectile.position = Main.npc[(int)projectile.ai[1]].Center + value22 - new Vector2(projectile.width, projectile.height) / 2f;
            }
            else projectile.Kill();

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Zombie, (int)projectile.position.X, (int)projectile.position.Y, 104, 1f, 0f);
            }

            float num801 = 1f;
            projectile.localAI[0] += 1f;
            if (projectile.localAI[0] >= 180f)
            {
                projectile.Kill();
                return false;
            }

            projectile.scale = (float)Math.Sin(projectile.localAI[0] * MathHelper.Pi / 180f) * 10f * num801;
            if (projectile.scale > num801)
            {
                projectile.scale = num801;
            }

            float rotationalAcceleration = projectile.velocity.ToRotation();
            rotationalAcceleration += projectile.ai[0];
            projectile.rotation = rotationalAcceleration - MathHelper.PiOver2;
            projectile.velocity = rotationalAcceleration.ToRotationVector2();

            float num805 = 3f;
            float num806 = projectile.width;

            Vector2 samplingPoint = projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }

            float[] array3 = new float[(int)num805];
            Collision.LaserScan(samplingPoint, projectile.velocity, num806 * projectile.scale, 2900f, array3);
            float laserLength = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                laserLength += array3[num808];
                num3 = num808;
            }
            laserLength /= num805;

            float amount = 0.5f;
            projectile.localAI[1] = MathHelper.Lerp(projectile.localAI[1], 2900f, amount);
            Vector2 laserEndPoint = projectile.Center + projectile.velocity * (projectile.localAI[1] - 20f);

            if (projectile.localAI[0] % 35 == 34)
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector2 velocity = (MathHelper.TwoPi / 8f * k).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                    velocity = velocity.RotatedByRandom(MathHelper.ToRadians(7f));
                    Dust.NewDust(laserEndPoint, projectile.width, projectile.height, 229, velocity.X, velocity.Y, 0, default, 1f);
                }
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.ToRadians(Main.rand.NextFloat(21f, 32f)) * (i - 2f) / 2f;
                    Projectile.NewProjectile(laserEndPoint, new Vector2(0f, -6f).RotatedBy(angle), ModContent.ProjectileType<PhantasmalSpark>(), 39, 1f);
                }
            }

            for (int num809 = 0; num809 < 2; num809 = num3 + 1)
            {
                float num810 = projectile.velocity.ToRotation() + ((Main.rand.Next(2) == 1) ? -1f : 1f) * 1.57079637f;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new Vector2((float)Math.Cos(num810) * num811, (float)Math.Sin(num810) * num811);
                int num812 = Dust.NewDust(laserEndPoint, 0, 0, 229, vector80.X, vector80.Y, 0, default, 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
                num3 = num809;
            }
            if (Main.rand.Next(5) == 0)
            {
                Vector2 value29 = projectile.velocity.RotatedBy(1.5707963705062866, default) * ((float)Main.rand.NextDouble() - 0.5f) * projectile.width;
                int num813 = Dust.NewDust(laserEndPoint + value29 - Vector2.One * 4f, 8, 8, 31, 0f, 0f, 100, default, 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }
            DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * projectile.localAI[1], projectile.width * projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));

            return false;
        }
    }
}
