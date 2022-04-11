using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class UnworldlyEntity : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Timer => ref NPC.ai[0];
        public ref float DeathCountdown => ref NPC.ai[1];
        public override void SetStaticDefaults()
        {
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

            NPC.aiStyle = aiType = -1;
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
                Vector2 idealVelocity = NPC.SafeDirectionTo(Target.Center) * 17f;
                NPC.velocity = (NPC.velocity * 29f + idealVelocity) / 30f;
                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 0.15f);
                NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.02f, 0.15f);
            }

            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            Timer++;

            // Explode into strange tentacles if close to the target or enough time has passed.
            if (NPC.WithinRange(Target.Center, 40f))
            {
                SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        // Randomize tentacle behavior variables.
                        float ai0 = Main.rand.NextFloat(0.01f, 0.08f) * Main.rand.NextBool().ToDirectionInt();
                        float ai1 = Main.rand.NextFloat(0.01f, 0.08f) * Main.rand.NextBool().ToDirectionInt();

                        int tentacle = Projectile.NewProjectile(Target.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<VoidTentacle>(), 250, 0f);
                        if (Main.projectile.IndexInRange(tentacle))
                        {
                            Main.projectile[tentacle].ai[0] = ai0;
                            Main.projectile[tentacle].ai[1] = ai1;
                        }
                    }
                }

                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
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
