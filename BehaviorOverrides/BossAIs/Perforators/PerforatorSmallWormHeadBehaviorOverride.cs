using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Perforator;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorSmallWormHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadSmall>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;
        public override bool PreAI(NPC npc)
        {
            ref float fallCountdown = ref npc.ai[0];
            ref float enrageTimer = ref npc.ai[1];
            ref float hasSummonedSegments = ref npc.localAI[0];

            npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
            {
                PerforatorHiveBehaviorOverride.SpawnSegments(npc, 10, ModContent.NPCType<PerforatorBodySmall>(), ModContent.NPCType<PerforatorTailSmall>());
                hasSummonedSegments = 1f;
            }

            npc.timeLeft = 3600;
            if (!NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>()))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[npc.target];

            bool outOfBiome = !target.ZoneCorrupt && !target.ZoneCrimson;
            if (outOfBiome && !BossRushEvent.BossRushActive)
                enrageTimer++;
            else
                enrageTimer = 0f;

            bool enraged = enrageTimer > 300f;
            npc.dontTakeDamage = enraged;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            int bodyType = ModContent.NPCType<PerforatorBodySmall>();
            float moveSpeed = MathHelper.Lerp(0.13f, 0.3f, 1f - npc.life / (float)npc.lifeMax);
            float maxVerticalSpeed = 17f;
            int circularBurstCount = 10;
            float circularBurstSpeed = 8f;
            if (BossRushEvent.BossRushActive || enraged)
            {
                moveSpeed *= 4f;
                maxVerticalSpeed = 20f;
                circularBurstCount = 20;
                circularBurstSpeed = 20f;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool inAir = true;
                if (Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height))
                    inAir = false;
                if (Main.npc[i].type == bodyType && Main.npc[i].active && inAir)
                    totalSegmentsInAir++;
            }

            if (fallCountdown > 0f)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + moveSpeed * 1.2f, -maxVerticalSpeed, maxVerticalSpeed);
                fallCountdown--;
            }
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - moveSpeed * 3f, -maxVerticalSpeed, maxVerticalSpeed);
                npc.velocity.X = (npc.velocity.X * 3f + npc.SafeDirectionTo(target.Center).X * 8.5f) / 4f;

                if (totalSegmentsInAir >= 7 && target.Center.Y - npc.Center.Y > -920f)
                {
                    fallCountdown = 45f;
                    for (int i = 0; i < circularBurstCount; i++)
                    {
                        Vector2 ichorVelocity = (MathHelper.TwoPi * i / circularBurstCount).ToRotationVector2() * circularBurstSpeed;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 95, 0f);
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }
    }
}
