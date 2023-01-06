using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Signus
{
    public class UnworldlyEntity : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Timer => ref NPC.ai[0];
        public ref float DeathCountdown => ref NPC.ai[1];
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Unworldly Entity");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 180;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 62;
            NPC.defense = 15;
            NPC.lifeMax = 5666;
            if (BossRushEvent.BossRushActive)
                NPC.lifeMax = 26666;

            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit41;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Fade away on death.
            if (DeathCountdown > 0f)
            {
                DeathCountdown--;

                NPC.velocity *= 0.925f;
                NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.032f, 0.15f);
                NPC.Opacity = Utils.GetLerpValue(1f, 60f, DeathCountdown, true);

                if (DeathCountdown <= 1f)
                    NPC.active = false;

                return;
            }

            if (Timer >= Main.rand.NextFloat(400f, 500f))
            {
                DeathCountdown = 60f;
                NPC.netUpdate = true;
            }

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                NPC.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }
            else if (NPC.timeLeft > 600)
                NPC.timeLeft = 600;

            // Fade in.
            NPC.Opacity = Utils.GetLerpValue(0f, 30f, Timer, true);

            // Fly upwards before charging at the target.
            if (Timer < 40f)
            {
                if (NPC.velocity == Vector2.Zero)
                {
                    NPC.velocity = -Vector2.UnitY.RotatedByRandom(0.26f) * Main.rand.NextFloat(1f, 3f);
                    NPC.netUpdate = true;
                }
                NPC.velocity *= 0.97f;
                NPC.rotation = -NPC.velocity.X * 0.02f;
            }

            // Charge after this.
            else
            {
                Vector2 idealVelocity = NPC.SafeDirectionTo(Target.Center) * 12.75f;
                NPC.velocity = (NPC.velocity * 29f + idealVelocity) / 30f;
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 0.15f);
                NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.02f, 0.15f);
            }

            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            Timer++;

            // Explode into a bomb if close to the target.
            if (NPC.WithinRange(Target.Center, 40f))
                NPC.active = false;
        }

        public override bool PreKill() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int bomb = Utilities.NewProjectileBetter(NPC.Center, NPC.SafeDirectionTo(Target.Center) * 15f, ModContent.ProjectileType<DarkCosmicBomb>(), 0, 0f);
                if (Main.projectile.IndexInRange(bomb))
                    Main.projectile[bomb].ModProjectile<DarkCosmicBomb>().ExplosionRadius = 500f;
            }

            DeathCountdown = 60f;

            NPC.life = NPC.lifeMax;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 6D)
            {
                NPC.frame.Y += 92;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }
    }
}
