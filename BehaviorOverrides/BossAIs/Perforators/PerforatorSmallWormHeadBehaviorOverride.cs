using CalamityMod;
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
            ref float hasSummonedSegments = ref npc.localAI[0];

            npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
            {
                PerforatorHiveBehaviorOverride.SpawnSegments(npc, 10, ModContent.NPCType<PerforatorBodySmall>(), ModContent.NPCType<PerforatorTailSmall>());
                hasSummonedSegments = 1f;
            }

            if (!NPC.AnyNPCs(ModContent.NPCType<PerforatorHive>()))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[npc.target];

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            int bodyType = ModContent.NPCType<PerforatorBodySmall>();
            float moveSpeed = MathHelper.Lerp(0.13f, 0.3f, 1f - npc.life / (float)npc.lifeMax);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                if (Main.npc[i].type == bodyType && Main.npc[i].active && inAir)
                    totalSegmentsInAir++;
            }

            if (fallCountdown > 0f)
            {
                if (npc.Center.Y < target.Center.Y + 470f)
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + moveSpeed * 1.5f, -17f, 17f);
                else
                    npc.velocity.Y *= 0.93f;
                fallCountdown--;
            }
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - moveSpeed, -17f, 17f);
                npc.velocity.X = (npc.velocity.X * 3f + npc.SafeDirectionTo(target.Center).X * 8.5f) / 4f;

                if (totalSegmentsInAir >= 7)
                {
                    fallCountdown = 45f;
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 ichorVelocity = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 8f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }
    }
}
