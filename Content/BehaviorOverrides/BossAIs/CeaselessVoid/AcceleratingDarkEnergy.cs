using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class AcceleratingDarkEnergy : ModProjectile
    {
        public enum DarkEnergyAttackState
        {
            HoverInPlace,
            SpinInPlace,
            AccelerateTowardsTarget
        }

        public List<Particle> LocalParticles
        {
            get;
            set;
        }

        public int Index
        {
            get;
            set;
        }

        public float SpinOffsetAngle
        {
            get;
            set;
        }

        public Vector2 RestingPosition
        {
            get;
            set;
        }

        public Vector2 CenterPoint
        {
            get;
            set;
        }

        public DarkEnergyAttackState AttackState
        {
            get => (DarkEnergyAttackState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public CurveSegment SpinRise => new(EasingType.PolyIn, 0f, CenterPoint.Distance(RestingPosition), 60f, 2);

        public CurveSegment SpinFall => new(EasingType.PolyOut, 0.35f, SpinRise.EndingHeight, -SpinRise.EndingHeight, 4);

        public Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        public ref float Time => ref Projectile.localAI[0];

        public ref float Acceleration => ref Projectile.ai[1];

        public ref float ZapFrameTimer => ref Projectile.localAI[1];

        public static float IdealSpeed => 42f;

        public static int SpinTime => 30;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergy";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy");
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
            LocalParticles = new();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(SpinOffsetAngle);
            writer.WriteVector2(RestingPosition);
            writer.WriteVector2(CenterPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            SpinOffsetAngle = reader.ReadSingle();
            RestingPosition = reader.ReadVector2();
            CenterPoint = reader.ReadVector2();
        }

        public override void AI()
        {
            switch (AttackState)
            {
                case DarkEnergyAttackState.HoverInPlace:
                    DoBehavior_HoverInPlace();
                    break;
                case DarkEnergyAttackState.SpinInPlace:
                    DoBehavior_SpinInPlace();
                    break;
                case DarkEnergyAttackState.AccelerateTowardsTarget:
                    DoBehavior_AccelerateTowardsTarget();
                    break;
            }

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Update all particles.
            LocalParticles.RemoveAll(p => p.Time >= p.Lifetime || p is null);
            foreach (Particle particle in LocalParticles)
            {
                particle.Position += particle.Velocity;
                particle.Position = Vector2.Lerp(particle.Position, Projectile.Center, 0.2f);
                particle.Time++;
                particle.Update();
            }

            if (Projectile.frame == 0 && Projectile.frameCounter % 5 == 0 && Main.rand.NextBool(3) && ZapFrameTimer <= 0f)
                ZapFrameTimer = 1f;
            if (ZapFrameTimer >= 1f)
            {
                ZapFrameTimer++;
                if (ZapFrameTimer >= Main.projFrames[Type] * 5)
                    ZapFrameTimer = 0f;
            }

            // Rotate based on velocity.
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Time++;
        }

        public void DoBehavior_HoverInPlace()
        {
            float hoverInstabilityInterpolant = Utils.GetLerpValue(40f, 30f, Time, true);
            float hoverOffsetSine = MathF.Sin(MathHelper.TwoPi * Time / 60f + MathHelper.PiOver4 * Projectile.identity);
            Vector2 bobHoverOffset = 60f * hoverInstabilityInterpolant * hoverOffsetSine * Vector2.UnitY;
            Vector2 fallFromAboveOffset = MathHelper.SmoothStep(0f, -950f, Utils.GetLerpValue(32f, 0f, Time, true)) * Vector2.UnitY;

            // Hover into position.
            Vector2 hoverDestination = RestingPosition + bobHoverOffset + fallFromAboveOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, 0.06f).MoveTowards(hoverDestination, 6f);
            Projectile.velocity = Vector2.Zero;

            // Fade in.
            Projectile.Opacity = MathF.Pow(Utils.GetLerpValue(8f, 36f, Time, true), 2.6f);
        }

        public void DoBehavior_SpinInPlace()
        {
            float spinCompletion = Utils.GetLerpValue(0f, SpinTime, Time, true);
            float radius = PiecewiseAnimation(spinCompletion, SpinRise, SpinFall);

            if (Time <= 0f)
                SpinOffsetAngle = CenterPoint.AngleTo(Projectile.Center);

            // Accelerate once done spinning.
            if (Time >= SpinTime)
            {
                Projectile.velocity = 12f * SpinOffsetAngle.ToRotationVector2();
                AttackState = DarkEnergyAttackState.AccelerateTowardsTarget;
                Time = 28f;
                Projectile.netUpdate = true;
                return;
            }

            // SPEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEN!!
            SpinOffsetAngle += MathHelper.TwoPi / SpinTime;
            Projectile.Center = CenterPoint + SpinOffsetAngle.ToRotationVector2() * radius;

            // Spawn particles.
            for (int i = 0; i < 2; i++)
            {
                Color voidColor = Color.Lerp(Color.Purple, Color.Black, Main.rand.NextFloat(0.5f, 0.9f));
                voidColor = Color.Lerp(voidColor, Color.DarkBlue, Main.rand.NextFloat(0.25f));
                HeavySmokeParticle voidGas = new(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2f, 2f), voidColor, 9, Projectile.scale * 1.7f, Projectile.Opacity, Main.rand.NextFloat(0.02f), true);
                LocalParticles.Add(voidGas);
            }
        }

        public void DoBehavior_AccelerateTowardsTarget()
        {
            // Accelerate.
            if (Projectile.velocity.Length() < IdealSpeed)
                Projectile.velocity *= Acceleration;

            // Arc towards the target.
            if (Time <= 28f)
            {
                if (!Projectile.WithinRange(Target.Center, 250f))
                {
                    Vector2 idealVelocity = Projectile.SafeDirectionTo(Target.Center, -Vector2.UnitY) * Projectile.velocity.Length();
                    Projectile.velocity = Projectile.velocity.RotateTowards(idealVelocity.ToRotation(), MathHelper.Pi / 5f);
                }
            }

            Projectile.Opacity = 1f;
            Projectile.tileCollide = Time >= 54f;

            // Spawn particles.
            for (int i = 0; i < 3; i++)
            {
                Color voidColor = Color.Lerp(Color.Purple, Color.Black, Main.rand.NextFloat(0.6f, 0.9f));
                voidColor = Color.Lerp(voidColor, Color.DarkBlue, Main.rand.NextFloat(0.3f));
                HeavySmokeParticle voidGas = new(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), Main.rand.NextVector2Circular(2f, 2f), voidColor, 9, Projectile.scale * 1.7f, Projectile.Opacity, Main.rand.NextFloat(0.02f), true);
                LocalParticles.Add(voidGas);
            }
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f && AttackState == DarkEnergyAttackState.AccelerateTowardsTarget;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion with { Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Color streakColor = Color.Lerp(Color.HotPink, Color.LightCyan, Main.rand.NextFloat());
                Vector2 streakVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(4f, 13f) + Main.rand.NextVector2Circular(2.5f, 2.5f);
                SparkParticle streak = new(Projectile.Center + streakVelocity * 5f, streakVelocity, false, Main.rand.Next(8, 12), 1.25f, streakColor);
                GeneralParticleHandler.SpawnParticle(streak);

                streak = new(Projectile.Center, streakVelocity.RotatedByRandom(0.25f) * 0.425f, false, Main.rand.Next(11, 16), 0.8f, streakColor);
                GeneralParticleHandler.SpawnParticle(streak);
            }

            Color bloomColor = Color.Lerp(Color.MediumPurple, Color.HotPink, Main.rand.NextFloat(0.6f));
            FlareShine strike = new(Projectile.Center, Vector2.Zero, Color.MediumPurple, bloomColor, 0f, Vector2.One * 9f, Vector2.Zero, 40, 0f, 8f);
            GeneralParticleHandler.SpawnParticle(strike);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.Opacity = 1f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            foreach (Particle particle in LocalParticles)
                particle.CustomDraw(Main.spriteBatch);
            Main.spriteBatch.ExitShaderRegion();

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D electricityTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergyElectricity").Value;
            if (ZapFrameTimer >= 1f && Projectile.frame <= 3f)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergyBright").Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);

            if (ZapFrameTimer >= 1f)
                Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, electricityTexture);
            return false;
        }
    }
}
