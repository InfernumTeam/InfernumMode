using CalamityMod;
using CalamityMod.NPCs.Perforator;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorLargeWormHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadLarge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;
        public override bool PreAI(NPC npc)
        {
            ref float fallCountdown = ref npc.ai[0];
            ref float hasSummonedSegments = ref npc.localAI[0];

            npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);
            npc.Calamity().DR = 0.05f;

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedSegments == 0f)
            {
                PerforatorHiveBehaviorOverride.SpawnSegments(npc, 22, ModContent.NPCType<PerforatorBodyLarge>(), ModContent.NPCType<PerforatorTailLarge>());
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

            // Count segments in the air.
            int totalSegmentsInAir = 0;
            int bodyType = ModContent.NPCType<PerforatorBodyLarge>();
            float moveSpeed = MathHelper.Lerp(0.15f, 0.36f, 1f - npc.life / (float)npc.lifeMax);
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
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + moveSpeed * 1.775f, -17f, 17f);
                fallCountdown--;
            }
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - moveSpeed * 2f, -17f, 17f);

                if (MathHelper.Distance(target.Center.X, npc.Center.X) > 125f)
                    npc.velocity.X = (npc.velocity.X * 5f + npc.SafeDirectionTo(target.Center).X * 8.5f) / 6f;

                if (totalSegmentsInAir >= 14 && target.Center.Y - npc.Center.Y > -870f)
                {
                    fallCountdown = 50f;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 ichorVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                    }

                    if (!Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 ichorVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Lerp(-0.46f, 0.46f, i / 3f)) * 10f;
                            Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<SittingBlood>(), 75, 0f);
                        }
                    }
                    npc.netUpdate = true;
                }
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }
    }
}
