using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class SmallDrone : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Timer => ref npc.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Small Drone");
            Main.npcFrameCount[npc.type] = 5;
        }

        public override void SetDefaults()
        {
            npc.damage = 140;
            npc.npcSlots = 0f;
            npc.width = npc.height = 42;
            npc.defense = 15;
            npc.lifeMax = 2400;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 0.03f, 0.2f, 0f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                npc.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;
                    return;
                }
            }
            else if (npc.timeLeft > 600)
                npc.timeLeft = 600;

            if (Timer > 45f)
            {
                // Home more quickly if close to the target.
                // However, if really close to the target, stop homing and simply go in the
                // current direction.
                float hoverSpeed = 12.25f;
                if (!npc.WithinRange(Target.Center, 210f))
                    npc.velocity = (npc.velocity * 84f + npc.SafeDirectionTo(Target.Center) * hoverSpeed) / 85f;
                else if (!npc.WithinRange(Target.Center, 120f))
                    npc.velocity = (npc.velocity * 60f + npc.SafeDirectionTo(Target.Center) * hoverSpeed * 0.8f) / 61f;
			}

            // Fly downward.
			else
                npc.velocity = Vector2.UnitY * Timer / 45f * 12f;

            Timer++;

            // Explode if close to the target or enough time has passed.
            if (npc.WithinRange(Target.Center, 40f) || Timer >= Main.rand.NextFloat(400f, 620f))
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }

            // Release lasers periodically.
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer % 75f == 74f)
			{
                Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center) * 7.4f;
                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ProjectileID.DeathLaser, 150, 0f);
            }
        }

        public override bool PreNPCLoot() => false;

		public override bool CheckDead()
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.position);

            npc.position = npc.Center;
            npc.width = npc.height = 84;
            npc.Center = npc.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 1.4f);
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
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 1.85f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;

            if (npc.frameCounter >= 5D)
            {
                npc.frame.Y += frameHeight;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                    npc.frame.Y = 0;

                npc.frameCounter = 0D;
            }
        }
    }
}
