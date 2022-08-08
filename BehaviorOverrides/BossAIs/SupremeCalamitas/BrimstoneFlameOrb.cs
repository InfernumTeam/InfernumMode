using CalamityMod;
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

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneFlameOrb : ModProjectile
    {
        public PrimitiveTrailCopy FireDrawer;

        public NPC Owner => Main.npc.IndexInRange((int)Projectile.ai[1]) && Main.npc[(int)Projectile.ai[1]].active ? Main.npc[(int)Projectile.ai[1]] : null;

        public static int LaserCount => 5;

        public float TelegraphInterpolant => Utils.GetLerpValue(20f, LaserReleaseDelay, Time, true);

        public float Radius => Owner.Infernum().ExtraAI[0] * (1f - Owner.Infernum().ExtraAI[1]);

        public ref float Time => ref Projectile.ai[0];

        public const int OverloadBeamLifetime = 300;

        public const int LaserReleaseDelay = 125;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Flame Orb");
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
            if (Owner is null)
            {
                Projectile.Kill();
                return;
            }

            // Die after sufficiently shrunk.
            if (Owner.Infernum().ExtraAI[1] >= 1f)
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
                    int laser = Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<FlameOverloadBeam>(), 900, 0f);
                    if (Main.projectile.IndexInRange(laser))
                        Main.projectile[laser].ai[0] = Owner.whoAmI;
                }
            }
            
            Time++;
        }

        public float OrbWidthFunction(float completionRatio) => MathHelper.SmoothStep(0f, Radius, (float)Math.Sin(MathHelper.Pi * completionRatio));

        public Color OrbColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.Yellow, Color.Red, MathHelper.Lerp(0.2f, 0.8f, Projectile.localAI[0] % 1f));
            c = Color.Lerp(c, Color.White, completionRatio * 0.5f);
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
                    Color telegraphColor = Color.Orange * (float)Math.Pow(TelegraphInterpolant, 0.67);
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
