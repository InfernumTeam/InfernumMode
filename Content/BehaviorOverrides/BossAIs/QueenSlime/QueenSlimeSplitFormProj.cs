using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeSplitFormProj : ModProjectile
    {
        public Vector2 ConvergencePoint
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.QueenSlimeMinionPurple}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Heavenly Slime");
            Main.projFrames[Type] = 4;
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
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Decide the direction.
            if (Math.Abs(Projectile.velocity.X) >= 0.4f)
                Projectile.spriteDirection = -Math.Sign(Projectile.velocity.X);

            // Move towards the convergence point.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(ConvergencePoint) * 19f, 0.07f);
            if (Projectile.WithinRange(ConvergencePoint, 36f))
                Projectile.Kill();

            Time++;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(ConvergencePoint);

        public override void ReceiveExtraAI(BinaryReader reader) => ConvergencePoint = reader.ReadVector2();

        public override bool? CanDamage() => Time >= 24f;

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.Pink with { A = 0 } * 0.75f, lightColor, Projectile.Opacity * 5f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.Purple with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 16f, Time, true)) * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            int queenSlimeIndex = NPC.FindFirstNPC(NPCID.QueenSlimeBoss);
            if (queenSlimeIndex == -1)
                return;

            if (Main.netMode != NetmodeID.Server && Projectile.WithinRange(Main.LocalPlayer.Center, 1500f))
            {
                // Create a slime hit sound.
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

            NPC queenSlime = Main.npc[queenSlimeIndex];
            queenSlime.Infernum().ExtraAI[2] = 1f;
            queenSlime.netUpdate = true;
        }
    }
}
