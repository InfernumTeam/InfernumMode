using System;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class FallingSpikeSlimeProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.QueenSlimeMinionBlue}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Slime");
            Main.projFrames[Type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 540;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            
        }

        public override void AI()
        {
            // Slam downward.
            if (Projectile.velocity.Y >= 0.1f)
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y * 1.1f + 0.4f, 0.1f, 18f);

            // Decide frames.
            Projectile.frame = 1;

            Time++;

            // Interact with tiles after a short amount of time has passed.
            Projectile.tileCollide = Time >= 36f;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server && Projectile.WithinRange(Main.LocalPlayer.Center, 1500f))
            {
                // Create a slime hit sound.
                if (Main.rand.NextBool(6))
                    SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);

                // Create a bunch of crystal shards.
                for (int i = 0; i < 15; i++)
                {
                    Dust crystalShard = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.Next(DustID.BlueCrystalShard, DustID.PurpleCrystalShard + 1));
                    crystalShard.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2.5f;
                    crystalShard.noGravity = Main.rand.NextBool();
                    crystalShard.scale = Main.rand.NextFloat(0.9f, 1.3f);
                }

                for (int i = 0; i < 6; i++)
                {
                    Color gelColor = Color.Lerp(Color.Pink, Color.HotPink, Main.rand.NextFloat());
                    Particle gelParticle = new EoCBloodParticle(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), -Vector2.UnitY.RotatedByRandom(0.98f) * Main.rand.NextFloat(4f, 20f), 60, Main.rand.NextFloat(1.5f, 2f), gelColor * 0.75f, 5f);
                    GeneralParticleHandler.SpawnParticle(gelParticle);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 spikeVelocity = (TwoPi * i / 3f - PiOver2).ToRotationVector2() * 5f;
                    spikeVelocity = Vector2.Lerp(spikeVelocity, -Vector2.UnitY * spikeVelocity.Length(), 0.82f);
                    Utilities.NewProjectileBetter(Projectile.Center + spikeVelocity, spikeVelocity, ModContent.ProjectileType<QueenSlimeCrystalSpike>(), QueenSlimeBehaviorOverride.SmallCrystalSpikeDamage, 0f);
                }
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.UnitX * -4f, ModContent.ProjectileType<QueenSlimeCrystalSpike>(), QueenSlimeBehaviorOverride.SmallCrystalSpikeDamage, 0f);
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.UnitX * 4f, ModContent.ProjectileType<QueenSlimeCrystalSpike>(), QueenSlimeBehaviorOverride.SmallCrystalSpikeDamage, 0f);
            }
        }

        public override bool? CanDamage() => Math.Abs(Projectile.velocity.Y) >= 0.1f && Time >= 30f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type]);
            Projectile.DrawProjectileWithBackglowTemp(Color.Cyan with { A = 0 } * 0.75f, lightColor, Projectile.Opacity * 5f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.HotPink with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 16f, Time, true)) * Projectile.Opacity;
    }
}
