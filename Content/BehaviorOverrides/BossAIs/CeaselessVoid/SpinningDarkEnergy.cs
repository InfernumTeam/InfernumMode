using CalamityMod.NPCs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class SpinningDarkEnergy : ModProjectile
    {
        public List<Particle> LocalParticles
        {
            get;
            set;
        }
        public static NPC CeaselessVoid => Main.npc[CalamityGlobalNPC.voidBoss];

        public ref float Time => ref Projectile.ai[0];

        public ref float SpinOffsetAngle => ref Projectile.ai[1];

        public ref float ZapFrameTimer => ref Projectile.localAI[0];

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergy";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Energy");
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
            Projectile.timeLeft = 72000;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
            LocalParticles = [];
        }

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (CalamityGlobalNPC.voidBoss == -1)
            {
                Projectile.Kill();
                return;
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

            // Perform the zap effect.
            if (Projectile.frame == 0 && Projectile.frameCounter % 5 == 0 && Main.rand.NextBool(3) && ZapFrameTimer <= 0f)
                ZapFrameTimer = 1f;
            if (ZapFrameTimer >= 1f)
            {
                ZapFrameTimer++;
                if (ZapFrameTimer >= Main.projFrames[Type] * 5)
                    ZapFrameTimer = 0f;
            }

            // Spin around the Ceaseless Void.
            float spinRadius = Utils.GetLerpValue(0f, 60f, Time, true) * 400f;
            if (Projectile.Infernum().FadeAwayTimer >= 1)
                spinRadius *= Utils.GetLerpValue(1f, 30f, Projectile.Infernum().FadeAwayTimer, true);

            SpinOffsetAngle += ToRadians(3f);
            Projectile.Center = CeaselessVoid.Center + SpinOffsetAngle.ToRotationVector2() * spinRadius;

            // Rotate based on velocity.
            Projectile.rotation = SpinOffsetAngle;

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion with { Pitch = 0.4f, Volume = 0.4f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Color streakColor = Color.Lerp(Color.HotPink, Color.LightCyan, Main.rand.NextFloat());
                Vector2 streakVelocity = (TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(4f, 13f) + Main.rand.NextVector2Circular(2.5f, 2.5f);
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
            // Draw the particles behind the dark energy.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            foreach (Particle particle in LocalParticles)
                particle.CustomDraw(Main.spriteBatch);
            Main.spriteBatch.ExitShaderRegion();

            // Draw the dark energy and electricity if applicable.
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
