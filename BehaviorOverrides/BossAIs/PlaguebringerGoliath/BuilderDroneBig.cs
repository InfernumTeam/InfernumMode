using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class BuilderDroneBig : ModNPC
    {
        public Vector2 GeneralHoverPosition;
        public Player Target => Main.player[NPC.target];
        public ref float GeneralTimer => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Big Builder Drone");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 100;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 42;
            NPC.defense = 15;
            NPC.lifeMax = 5200;
            if (BossRushEvent.BossRushActive)
                NPC.lifeMax = 50000;

            NPC.aiStyle = aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 0.03f, 0.2f, 0f);

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

            NPC.dontTakeDamage = GeneralTimer < 60f;

            Vector2 continousHoverPosition = Target.Center + new Vector2(-250f, -175f);
            if (Vector2.Distance(GeneralHoverPosition, continousHoverPosition) > 325f)
                GeneralHoverPosition = continousHoverPosition;

            // Move in the general area of the hover position if not noticeably close or movement is very low.
            if (!NPC.WithinRange(continousHoverPosition, 200f) || NPC.velocity.Length() < 1f)
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(GeneralHoverPosition) * 13f, 0.85f);

            // Explode into rockets if the small builders are gone.
            if (!NPC.AnyNPCs(ModContent.NPCType<BuilderDroneSmall>()) || GeneralTimer >= PlagueNuke.BuildTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 rocketVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(9f, 13f);
                        Vector2 rocketSpawnPosition = NPC.Center + rocketVelocity * 4f;
                        Utilities.NewProjectileBetter(rocketSpawnPosition, rocketVelocity, ModContent.ProjectileType<RedirectingPlagueMissile>(), 160, 0f);
                    }
                }

                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }
            GeneralTimer++;
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 84;
            NPC.Center = NPC.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 1.4f);
                if (Main.rand.NextBool(2))
                {
                    plague.scale = 0.5f;
                    plague.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
                plague.velocity *= 3f;
                plague.noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 1.85f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 5D)
            {
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }
    }
}
