using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class PsionicOrb : ModProjectile
    {
        public PrimitiveTrailCopy OrbDrawer;

        public bool UseUndergroundAI
        {
            get => projectile.localAI[0] == 1f;
            set => projectile.localAI[0] = value.ToInt();
        }

        public int Lifetime => UseUndergroundAI ? 225 : 300;

        public int AttackCycleTime => UseUndergroundAI ? 75 : 100;

        public float TelegraphInterpolant
        {
            get
            {
                float wrappedTimeCompletion = Time % AttackCycleTime / AttackCycleTime;
                float telegraphInterpolant = Utils.InverseLerp(0f, 0.6f, wrappedTimeCompletion, true);
                if (wrappedTimeCompletion >= 0.667f)
                    telegraphInterpolant = 0f;
                return telegraphInterpolant;
            }
        }

        public ref float Time => ref projectile.ai[0];

        public ref float PredictiveAimRotation => ref projectile.ai[1];

        public const int Radius = 30;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Energy");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = Radius - 4;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(UseUndergroundAI);

        public override void ReceiveExtraAI(BinaryReader reader) => UseUndergroundAI = reader.ReadBoolean();

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.06f, 0f, 1f);
            projectile.velocity *= 0.97f;

            Player nearestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (Time % AttackCycleTime > AttackCycleTime * 0.667f)
            {
                if (Time % AttackCycleTime == (int)(AttackCycleTime * 0.667f) + 5)
                {
                    SoundEngine.PlaySound(SoundID.Item125, nearestTarget.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 9f;
                            Utilities.NewProjectileBetter(projectile.position, shootVelocity, ProjectileID.MartianTurretBolt, 90, 0f);
                        }
                    }
                }

                if (Time % 9f == 8f)
                    ShootRay();
            }

            if (Time % AttackCycleTime < AttackCycleTime * 0.5f)
            {
                Vector2 aimVector = (nearestTarget.Center + nearestTarget.velocity * 32f - projectile.Center).SafeNormalize(Vector2.UnitY);
                PredictiveAimRotation = Vector2.Normalize(Vector2.Lerp(aimVector, PredictiveAimRotation.ToRotationVector2(), 0.02f)).ToRotation();
            }

            Lighting.AddLight(projectile.Center, Color.Cyan.ToVector3() * 1.6f);
            Time++;
        }

        public void ShootRay()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 shootVelocity = PredictiveAimRotation.ToRotationVector2() * 16f;
                int ray = Utilities.NewProjectileBetter(projectile.Center - shootVelocity * 7.6f, shootVelocity, ModContent.ProjectileType<PsionicLightningBolt>(), 135, 0f, 255);

                if (Main.projectile.IndexInRange(ray))
                {
                    Main.projectile[ray].ai[0] = shootVelocity.ToRotation();
                    Main.projectile[ray].ai[1] = Main.rand.Next(100);
                    Main.projectile[ray].tileCollide = false;
                }
            }

            for (int i = 0; i < 36; i++)
            {
                Dust psychicMagic = Dust.NewDustPerfect(projectile.Center, 264);
                psychicMagic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 5f;
                psychicMagic.scale = 1.45f;
                psychicMagic.noLight = true;
                psychicMagic.noGravity = true;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(0f, 0.27f, completionRatio, true), 0.9f) * Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(projectile.width * 0.1f, projectile.width, squeezeInterpolant) * projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Cyan, Color.White, (float)Math.Sin(Math.Pow(completionRatio, 2D) * MathHelper.Pi));
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 3D);
            color *= projectile.Opacity * 3f;
            return color;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (OrbDrawer is null)
                OrbDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:BrainPsychic"]);

            List<Vector2> drawPoints = new();

            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                float telegraphScaleFactor = TelegraphInterpolant * 0.7f;

                Vector2 telegraphStart = projectile.Center - Main.screenPosition;
                Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                Vector2 telegraphScale = new(telegraphScaleFactor, 3f);
                Color telegraphColor = new Color(50, 255, 232) * (float)Math.Pow(TelegraphInterpolant, 0.79) * 1.4f;
                spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, PredictiveAimRotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                spriteBatch.ResetBlendState();
            }

            spriteBatch.EnterShaderRegion();

            // Create a charged circle out of several primitives.
            for (float offsetAngle = 0f; offsetAngle <= MathHelper.TwoPi; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTime * 2.6f;
                Vector2 center = projectile.Center - Vector2.One * projectile.Size * 0.5f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                    drawPoints.Add(Vector2.Lerp(center - offsetDirection * Radius * 0.925f, center + offsetDirection * Radius * 0.925f, i / 16f));

                OrbDrawer.Draw(drawPoints, projectile.Size * 0.5f - Main.screenPosition, 24);
            }
            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
