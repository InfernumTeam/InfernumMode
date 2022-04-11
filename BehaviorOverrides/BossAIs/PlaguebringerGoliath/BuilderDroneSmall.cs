using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class BuilderDroneSmall : ModNPC
    {
        public Vector2 GeneralHoverPosition;
        public Player Target => Main.player[NPC.target];
        public ref float GeneralTimer => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Small Builder Drone");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 100;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 42;
            NPC.defense = 15;
            NPC.lifeMax = 1200;
            if (BossRushEvent.BossRushActive)
                NPC.lifeMax = 11256;

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

            Vector2 continousHoverPosition = Target.Center + new Vector2(-280f, -225f);
            continousHoverPosition += (NPC.whoAmI * 1.58436f).ToRotationVector2() * (float)Math.Cos(GeneralTimer / 17f) * 42f;
            if (Vector2.Distance(GeneralHoverPosition, continousHoverPosition) > 325f)
                GeneralHoverPosition = continousHoverPosition;

            // Move in the general area of the hover position if not noticeably close or movement is very low.
            if (!NPC.WithinRange(continousHoverPosition, 95f) || NPC.velocity.Length() < 2.25f)
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(GeneralHoverPosition) * 11f, 0.9f);

            // Explode into rockets if the big builder is gone or the nuke has been launched.
            if (!NPC.AnyNPCs(ModContent.NPCType<BuilderDroneBig>()) || GeneralTimer >= PlagueNuke.BuildTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 rocketVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(9f, 13f);
                    Vector2 rocketSpawnPosition = NPC.Center + rocketVelocity * 4f;
                    Utilities.NewProjectileBetter(rocketSpawnPosition, rocketVelocity, ModContent.ProjectileType<RedirectingPlagueMissile>(), 160, 0f);
                }

                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
                return;
            }

            // Randomly play sounds to indicate building.
            if (Main.rand.NextBool(45))
            {
                NPC nuke = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<PlagueNuke>())];
                Vector2 end = nuke.Center + Main.rand.NextVector2Circular(8f, 8f);
                Dust.QuickDust(NPC.Center, Color.Lime).scale = 1.4f;
                Dust.QuickDust(end, Color.Lime).scale = 1.4f;
                for (float num2 = 0f; num2 < 1f; num2 += 0.01f)
                    Dust.QuickDust(Vector2.Lerp(NPC.Center, end, num2), Color.Lime).scale = 0.95f;

                switch (Main.rand.Next(4))
                {
                    case 0:
                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                        break;
                    case 1:
                        SoundEngine.PlaySound(SoundID.Item15, NPC.Center);
                        break;
                    case 2:
                        SoundEngine.PlaySound(SoundID.Item22, NPC.Center);
                        break;
                    case 3:
                        SoundEngine.PlaySound(SoundID.Item23, NPC.Center);
                        break;
                }
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
