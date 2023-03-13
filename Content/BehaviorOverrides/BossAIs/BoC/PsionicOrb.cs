using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BoC
{
    public class PsionicOrb : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy OrbDrawer;

        public bool UseUndergroundAI
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        public int Lifetime => UseUndergroundAI ? 225 : 300;

        public int AttackCycleTime => UseUndergroundAI ? 75 : 100;

        public float TelegraphInterpolant
        {
            get
            {
                float wrappedTimeCompletion = Time % AttackCycleTime / AttackCycleTime;
                float telegraphInterpolant = Utils.GetLerpValue(0f, 0.6f, wrappedTimeCompletion, true);
                if (wrappedTimeCompletion >= 0.667f)
                    telegraphInterpolant = 0f;
                return telegraphInterpolant;
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float PredictiveAimRotation => ref Projectile.ai[1];

        public const int Radius = 30;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Energy");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = Radius - 4;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(UseUndergroundAI);

        public override void ReceiveExtraAI(BinaryReader reader) => UseUndergroundAI = reader.ReadBoolean();

        public override void AI()
        {
            // Disappear if the brain is not present.
            if (!NPC.AnyNPCs(NPCID.BrainofCthulhu))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.06f, 0f, 1f);
            Projectile.velocity *= 0.97f;

            Player nearestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
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
                            Utilities.NewProjectileBetter(Projectile.position, shootVelocity, ProjectileID.MartianTurretBolt, 95, 0f);
                        }
                    }
                }

                if (Time % 9f == 8f)
                    ShootRay();
            }

            if (Time % AttackCycleTime < AttackCycleTime * 0.5f)
            {
                Vector2 aimVector = (nearestTarget.Center + nearestTarget.velocity * 32f - Projectile.Center).SafeNormalize(Vector2.UnitY);
                PredictiveAimRotation = PredictiveAimRotation.AngleLerp(aimVector.ToRotation(), 0.03f).AngleTowards(aimVector.ToRotation(), 0.02f);

                if (Time <= 2f)
                    PredictiveAimRotation = aimVector.ToRotation();
            }

            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 1.6f);
            Time++;
        }

        public void ShootRay()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 shootVelocity = PredictiveAimRotation.ToRotationVector2() * 16f;
                Utilities.NewProjectileBetter(Projectile.Center - shootVelocity * 7.6f, shootVelocity, ModContent.ProjectileType<PsionicLightningBolt>(), 145, 0f, -1, shootVelocity.ToRotation(), Main.rand.Next(100));
            }

            for (int i = 0; i < 36; i++)
            {
                Dust psychicMagic = Dust.NewDustPerfect(Projectile.Center, 264);
                psychicMagic.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 5f;
                psychicMagic.scale = 1.45f;
                psychicMagic.noLight = true;
                psychicMagic.noGravity = true;
            }
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 0.27f, completionRatio, true), 0.9f) * Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(Projectile.width * 0.1f, Projectile.width, squeezeInterpolant) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Cyan, Color.White, (float)Math.Sin(Math.Pow(completionRatio, 2D) * MathHelper.Pi));
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 3D);
            color *= Projectile.Opacity * 3f;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw a line telegraph as necessary
            if (TelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = InfernumTextureRegistry.BloomLine.Value;
                float telegraphScaleFactor = TelegraphInterpolant * 0.7f;

                Vector2 telegraphStart = Projectile.Center - Main.screenPosition;
                Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                Vector2 telegraphScale = new(telegraphScaleFactor, 3f);
                Color telegraphColor = new Color(50, 255, 232) * (float)Math.Pow(TelegraphInterpolant, 0.79) * 1.4f;
                Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, PredictiveAimRotation - MathHelper.PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            OrbDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.BrainPsychicVertexShader);

            spriteBatch.EnterShaderRegion();

            List<Vector2> drawPoints = new();

            // Create a charged circle out of several primitives.
            for (float offsetAngle = 0f; offsetAngle <= MathHelper.TwoPi; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTimeWrappedHourly * 2.6f;
                Vector2 center = Projectile.Center - Vector2.One * Projectile.Size * 0.5f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                for (int i = 0; i < 16; i++)
                    drawPoints.Add(Vector2.Lerp(center - offsetDirection * Radius * 0.925f, center + offsetDirection * Radius * 0.925f, i / 16f));

                OrbDrawer.DrawPixelated(drawPoints, Projectile.Size * 0.5f - Main.screenPosition, 24);
            }
            spriteBatch.ExitShaderRegion();
        }
    }
}
